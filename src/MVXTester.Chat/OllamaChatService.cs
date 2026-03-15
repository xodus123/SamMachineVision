using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MVXTester.Chat;

/// <summary>
/// Chat service using Ollama local LLM via REST API.
/// POST {base_url}/api/chat with NDJSON streaming.
/// </summary>
public sealed class OllamaChatService : IChatService, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string _visionModel;
    private readonly double _temperature;
    private readonly int _maxTokens;
    private readonly double _repeatPenalty;

    public string ModelName => _model;

    public OllamaChatService(ChatConfig config)
    {
        _baseUrl = config.OllamaBaseUrl.TrimEnd('/');
        _model = config.OllamaChatModel;
        _visionModel = !string.IsNullOrEmpty(config.OllamaVisionModel)
            ? config.OllamaVisionModel
            : config.OllamaChatModel;
        _temperature = config.Temperature;
        _maxTokens = config.MaxTokens;
        _repeatPenalty = config.RepeatPenalty;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<string> ChatAsync(
        string prompt,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var token in ChatStreamAsync(prompt, systemPrompt, history, ct))
        {
            sb.Append(token);
        }
        return sb.ToString();
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string prompt,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(prompt, systemPrompt, history);
        var body = new
        {
            model = _model,
            messages,
            stream = true,
            think = false,
            keep_alive = "30m",
            options = new { temperature = _temperature, num_predict = _maxTokens, top_k = 20, repeat_penalty = _repeatPenalty }
        };

        var json = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Ollama 오류 ({response.StatusCode}): {TruncateError(errorBody)}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        // Filter out <think>...</think> blocks from qwen3 reasoning
        bool inThinkBlock = false;
        var thinkBuffer = new StringBuilder();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msg) &&
                msg.TryGetProperty("content", out var content))
            {
                var token = content.GetString();
                if (string.IsNullOrEmpty(token)) continue;

                if (inThinkBlock)
                {
                    thinkBuffer.Append(token);
                    if (thinkBuffer.ToString().Contains("</think>"))
                    {
                        // Extract any text after </think>
                        var full = thinkBuffer.ToString();
                        var afterThink = full[(full.IndexOf("</think>") + "</think>".Length)..].TrimStart();
                        inThinkBlock = false;
                        thinkBuffer.Clear();
                        if (!string.IsNullOrEmpty(afterThink))
                            yield return afterThink;
                    }
                }
                else if (token.Contains("<think>"))
                {
                    inThinkBlock = true;
                    thinkBuffer.Clear();
                    // Save text before <think> tag
                    var beforeThink = token[..token.IndexOf("<think>")];
                    if (!string.IsNullOrEmpty(beforeThink))
                        yield return beforeThink;
                    thinkBuffer.Append(token[token.IndexOf("<think>")..]);
                    // Check if think block closes in same token
                    if (thinkBuffer.ToString().Contains("</think>"))
                    {
                        var full = thinkBuffer.ToString();
                        var afterThink = full[(full.IndexOf("</think>") + "</think>".Length)..].TrimStart();
                        inThinkBlock = false;
                        thinkBuffer.Clear();
                        if (!string.IsNullOrEmpty(afterThink))
                            yield return afterThink;
                    }
                }
                else
                {
                    yield return token;
                }
            }

            if (root.TryGetProperty("done", out var done) && done.GetBoolean())
                yield break;
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
        var messages = new List<object>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });

        if (history != null)
        {
            foreach (var msg in history)
            {
                var role = msg.Role == ChatRole.User ? "user" : "assistant";
                messages.Add(new { role, content = msg.Content });
            }
        }

        messages.Add(new
        {
            role = "user",
            content = prompt,
            images = new[] { base64 }
        });

        var body = new
        {
            model = _visionModel,
            messages,
            stream = false,
            think = false,
            keep_alive = "30m",
            options = new { temperature = _temperature, num_predict = _maxTokens, top_k = 20, repeat_penalty = _repeatPenalty }
        };

        var json = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Ollama API error: {response.StatusCode} - {responseJson}");

        using var doc = JsonDocument.Parse(responseJson);
        var rawContent = doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
        return StripThinkBlock(rawContent);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync($"{_baseUrl}/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static List<object> BuildMessages(
        string prompt,
        string? systemPrompt,
        IReadOnlyList<ChatMessage>? history)
    {
        var messages = new List<object>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });

        if (history != null)
        {
            foreach (var msg in history)
            {
                var role = msg.Role == ChatRole.User ? "user" : "assistant";
                messages.Add(new { role, content = msg.Content });
            }
        }

        messages.Add(new { role = "user", content = prompt });
        return messages;
    }

    private static string StripThinkBlock(string text)
    {
        // Remove <think>...</think> blocks from qwen3 model output
        while (true)
        {
            var start = text.IndexOf("<think>");
            if (start < 0) break;
            var end = text.IndexOf("</think>", start);
            if (end < 0)
            {
                // Unclosed think block — remove from <think> to end
                text = text[..start];
                break;
            }
            text = text[..start] + text[(end + "</think>".Length)..];
        }
        return text.TrimStart();
    }

    private static string TruncateError(string error)
    {
        return error.Length > 300 ? error[..300] + "..." : error;
    }

    public void Dispose() => _http.Dispose();
}
