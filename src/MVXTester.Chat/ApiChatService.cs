using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MVXTester.Chat;

/// <summary>
/// Chat service for cloud API providers: OpenAI, Claude (Anthropic), Gemini (Google).
/// </summary>
public sealed class ApiChatService : IChatService, IDisposable
{
    private const string AnthropicApiVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly string _provider; // "openai" | "claude" | "gemini"
    private readonly string _apiKey;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public string ModelName => _model;

    public ApiChatService(ChatConfig config)
    {
        _provider = config.ApiProvider.ToLowerInvariant();
        _apiKey = config.ApiKey;
        _model = !string.IsNullOrEmpty(config.ApiModel) ? config.ApiModel : DefaultModel(_provider);
        _temperature = config.Temperature;
        _maxTokens = config.MaxTokens;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    private static string DefaultModel(string provider) => provider switch
    {
        "claude" => "claude-sonnet-4-6",
        "gemini" => "gemini-2.5-flash",
        _ => "gpt-5-mini"
    };

    public async Task<string> ChatAsync(
        string prompt,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default)
    {
        var cleanSystem = StripOllamaDirectives(systemPrompt);
        return _provider switch
        {
            "claude" => await ClaudeChatAsync(prompt, cleanSystem, history, ct),
            "gemini" => await GeminiChatAsync(prompt, cleanSystem, history, ct),
            _ => await OpenAIChatAsync(prompt, cleanSystem, history, ct)
        };
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string prompt,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var cleanSystem = StripOllamaDirectives(systemPrompt);
        switch (_provider)
        {
            case "claude":
                await foreach (var t in ClaudeStreamAsync(prompt, cleanSystem, history, ct))
                    yield return t;
                break;
            case "gemini":
                // Gemini SSE 미구현 → 전체 응답 반환
                var geminiResult = await GeminiChatAsync(prompt, cleanSystem, history, ct);
                yield return geminiResult;
                break;
            default:
                await foreach (var t in OpenAIStreamAsync(prompt, cleanSystem, history, ct))
                    yield return t;
                break;
        }
    }

    public async Task<string> ChatWithImageAsync(
        string prompt,
        byte[] imageData,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(imageData);
        var cleanSystem = StripOllamaDirectives(systemPrompt);

        return _provider switch
        {
            "claude" => await ClaudeImageChatAsync(prompt, base64, cleanSystem, history, ct),
            "gemini" => await GeminiImageChatAsync(prompt, base64, cleanSystem, history, ct),
            _ => await OpenAIImageChatAsync(prompt, base64, cleanSystem, history, ct)
        };
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(_apiKey));
    }

    // ──────────────────── OpenAI ────────────────────

    private async Task<string> OpenAIChatAsync(
        string prompt, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history, CancellationToken ct)
    {
        var messages = BuildOpenAIMessages(prompt, systemPrompt, history);
        var body = new { model = _model, messages, temperature = _temperature, max_tokens = _maxTokens };
        var json = JsonSerializer.Serialize(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, responseJson, "OpenAI");

        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private async IAsyncEnumerable<string> OpenAIStreamAsync(
        string prompt, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var messages = BuildOpenAIMessages(prompt, systemPrompt, history);
        var body = new { model = _model, messages, temperature = _temperature, max_tokens = _maxTokens, stream = true };
        var json = JsonSerializer.Serialize(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") yield break;

            using var doc = JsonDocument.Parse(data);
            var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var content))
            {
                var token = content.GetString();
                if (!string.IsNullOrEmpty(token))
                    yield return token;
            }
        }
    }

    private async Task<string> OpenAIImageChatAsync(
        string prompt, string base64, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history, CancellationToken ct)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });

        if (history != null)
            foreach (var m in history)
                messages.Add(new { role = m.Role == ChatRole.User ? "user" : "assistant", content = m.Content });

        messages.Add(new
        {
            role = "user",
            content = new object[]
            {
                new { type = "text", text = prompt },
                new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64}" } }
            }
        });

        var body = new { model = _model, messages, temperature = _temperature, max_tokens = _maxTokens };
        var json = JsonSerializer.Serialize(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, responseJson, "OpenAI");

        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static List<object> BuildOpenAIMessages(
        string prompt, string? systemPrompt, IReadOnlyList<ChatMessage>? history)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });

        if (history != null)
            foreach (var m in history)
                messages.Add(new { role = m.Role == ChatRole.User ? "user" : "assistant", content = m.Content });

        messages.Add(new { role = "user", content = prompt });
        return messages;
    }

    // ──────────────────── Claude ────────────────────

    private async Task<string> ClaudeChatAsync(
        string prompt, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history, CancellationToken ct)
    {
        var messages = BuildClaudeMessages(prompt, history);
        var body = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["max_tokens"] = _maxTokens,
            ["temperature"] = _temperature,
            ["messages"] = messages
        };
        if (!string.IsNullOrEmpty(systemPrompt))
            body["system"] = systemPrompt;

        var json = JsonSerializer.Serialize(body);
        using var request = CreateClaudeRequest(json);

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, responseJson, "Claude");

        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("content")[0]
            .GetProperty("text").GetString() ?? "";
    }

    private async IAsyncEnumerable<string> ClaudeStreamAsync(
        string prompt, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var messages = BuildClaudeMessages(prompt, history);
        var body = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["max_tokens"] = _maxTokens,
            ["temperature"] = _temperature,
            ["messages"] = messages,
            ["stream"] = true
        };
        if (!string.IsNullOrEmpty(systemPrompt))
            body["system"] = systemPrompt;

        var json = JsonSerializer.Serialize(body);
        using var request = CreateClaudeRequest(json);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var type) &&
                type.GetString() == "content_block_delta" &&
                root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
            {
                var token = text.GetString();
                if (!string.IsNullOrEmpty(token))
                    yield return token;
            }

            if (root.TryGetProperty("type", out var stopType) &&
                stopType.GetString() == "message_stop")
                yield break;
        }
    }

    private async Task<string> ClaudeImageChatAsync(
        string prompt, string base64, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history, CancellationToken ct)
    {
        var msgList = new List<object>();
        if (history != null)
            foreach (var m in history)
                msgList.Add(new { role = m.Role == ChatRole.User ? "user" : "assistant", content = m.Content });

        msgList.Add(new
        {
            role = "user",
            content = new object[]
            {
                new { type = "image", source = new { type = "base64", media_type = "image/png", data = base64 } },
                new { type = "text", text = prompt }
            }
        });

        var body = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["max_tokens"] = _maxTokens,
            ["temperature"] = _temperature,
            ["messages"] = msgList
        };
        if (!string.IsNullOrEmpty(systemPrompt))
            body["system"] = systemPrompt;

        var json = JsonSerializer.Serialize(body);
        using var request = CreateClaudeRequest(json);

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, responseJson, "Claude");

        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("content")[0]
            .GetProperty("text").GetString() ?? "";
    }

    private HttpRequestMessage CreateClaudeRequest(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", AnthropicApiVersion);
        return request;
    }

    private static List<object> BuildClaudeMessages(string prompt, IReadOnlyList<ChatMessage>? history)
    {
        var messages = new List<object>();
        if (history != null)
            foreach (var m in history)
                messages.Add(new { role = m.Role == ChatRole.User ? "user" : "assistant", content = m.Content });

        messages.Add(new { role = "user", content = prompt });
        return messages;
    }

    // ──────────────────── Gemini ────────────────────

    private async Task<string> GeminiChatAsync(
        string prompt, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history, CancellationToken ct)
    {
        var contents = BuildGeminiContents(prompt, history);
        var body = BuildGeminiBody(contents, systemPrompt);
        var json = JsonSerializer.Serialize(body);

        using var request = CreateGeminiRequest(json);

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, responseJson, "Gemini");

        using var doc = JsonDocument.Parse(responseJson);
        return ExtractGeminiText(doc);
    }

    private async Task<string> GeminiImageChatAsync(
        string prompt, string base64, string? systemPrompt,
        IReadOnlyList<ChatMessage>? history, CancellationToken ct)
    {
        var contents = new List<object>();
        if (history != null)
        {
            foreach (var m in history)
            {
                var role = m.Role == ChatRole.User ? "user" : "model";
                contents.Add(new { role, parts = new[] { new { text = m.Content } } });
            }
        }

        var userParts = new List<object>
        {
            new { text = prompt },
            new { inline_data = new { mime_type = "image/png", data = base64 } }
        };
        contents.Add(new { role = "user", parts = userParts });

        var body = BuildGeminiBody(contents, systemPrompt);
        var json = JsonSerializer.Serialize(body);

        using var request = CreateGeminiRequest(json);

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, responseJson, "Gemini");

        using var doc = JsonDocument.Parse(responseJson);
        return ExtractGeminiText(doc);
    }

    /// <summary>
    /// Gemini 응답에서 thinking part를 제외하고 실제 텍스트만 추출.
    /// </summary>
    private static string ExtractGeminiText(JsonDocument doc)
    {
        var parts = doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts");

        foreach (var part in parts.EnumerateArray())
        {
            // thought: true인 part는 건너뛰기 (Gemini thinking 출력)
            if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                continue;

            if (part.TryGetProperty("text", out var text))
                return text.GetString() ?? "";
        }

        // 모든 part가 thought이면 첫 번째 텍스트라도 반환
        return parts[0].TryGetProperty("text", out var fallback)
            ? fallback.GetString() ?? ""
            : "";
    }

    private static List<object> BuildGeminiContents(
        string prompt, IReadOnlyList<ChatMessage>? history)
    {
        var contents = new List<object>();

        if (history != null)
        {
            foreach (var m in history)
            {
                var role = m.Role == ChatRole.User ? "user" : "model";
                contents.Add(new { role, parts = new[] { new { text = m.Content } } });
            }
        }

        contents.Add(new { role = "user", parts = new[] { new { text = prompt } } });
        return contents;
    }

    private static object BuildGeminiBody(List<object> contents, string? systemPrompt)
    {
        // thinking 비활성화 (Gemini 2.5 Flash 등)
        var generationConfig = new
        {
            thinkingConfig = new { thinkingBudget = 0 }
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            return new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents,
                generationConfig
            };
        }
        return new { contents, generationConfig };
    }

    /// <summary>
    /// Ollama 전용 지시어 (/no_think 등)를 제거. API 프로바이더에서는 불필요.
    /// </summary>
    private static string? StripOllamaDirectives(string? prompt)
    {
        if (string.IsNullOrEmpty(prompt)) return prompt;
        return prompt.Replace("/no_think\n", "").Replace("/no_think", "").Trim();
    }

    // ──────────────────── Helpers ────────────────────

    private static void EnsureSuccess(HttpResponseMessage response, string responseJson, string provider)
    {
        if (response.IsSuccessStatusCode) return;

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var errorMsg = doc.RootElement.TryGetProperty("error", out var err)
                ? (err.TryGetProperty("message", out var msg) ? msg.GetString() : err.ToString())
                : responseJson;
            throw new HttpRequestException($"{provider} API error ({response.StatusCode}): {SanitizeApiKey(errorMsg)}");
        }
        catch (JsonException)
        {
            throw new HttpRequestException($"{provider} API error ({response.StatusCode}): {SanitizeApiKey(responseJson)}");
        }
    }

    private HttpRequestMessage CreateGeminiRequest(string json)
    {
        var safeModel = Uri.EscapeDataString(_model);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{safeModel}:generateContent";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _apiKey);
        return request;
    }

    private static string? SanitizeApiKey(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return System.Text.RegularExpressions.Regex.Replace(text, @"key=[^&\s""]+", "key=***");
    }

    public void Dispose() => _http.Dispose();
}
