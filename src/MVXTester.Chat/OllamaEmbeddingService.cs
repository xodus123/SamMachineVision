using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MVXTester.Chat;

/// <summary>
/// Embedding service using Ollama embed API.
/// POST {base_url}/api/embed
/// </summary>
public sealed class OllamaEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;
    private int _dimensions;

    public int Dimensions => _dimensions;

    public OllamaEmbeddingService(ChatConfig config)
    {
        _baseUrl = config.OllamaBaseUrl.TrimEnd('/');
        _model = config.OllamaEmbedModel;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public OllamaEmbeddingService(string baseUrl, string model)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
    {
        var body = new { model = _model, input = text, keep_alive = "30m" };
        var json = JsonSerializer.Serialize(body);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/embed")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _http.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Ollama embed error: {response.StatusCode} - {responseJson}");

        using var doc = JsonDocument.Parse(responseJson);

        // Response: { "embeddings": [[...]] }
        var embeddings = doc.RootElement.GetProperty("embeddings");
        var firstEmbedding = embeddings[0];
        var result = new float[firstEmbedding.GetArrayLength()];

        int i = 0;
        foreach (var val in firstEmbedding.EnumerateArray())
        {
            result[i++] = val.GetSingle();
        }

        _dimensions = result.Length;
        return result;
    }

    public void Dispose() => _http.Dispose();
}
