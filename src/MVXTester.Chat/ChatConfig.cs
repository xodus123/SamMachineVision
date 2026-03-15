using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MVXTester.Chat;

/// <summary>
/// Chatbot configuration loaded from Models/Chat/chat_config.json.
/// </summary>
public sealed class ChatConfig
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "ollama";

    [JsonPropertyName("ollama_base_url")]
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

    [JsonPropertyName("ollama_chat_model")]
    public string OllamaChatModel { get; set; } = "qwen3.5:2b";

    [JsonPropertyName("ollama_vision_model")]
    public string OllamaVisionModel { get; set; } = "qwen3.5:2b";

    [JsonPropertyName("ollama_embed_model")]
    public string OllamaEmbedModel { get; set; } = "qwen3-embedding:0.6b";

    [JsonPropertyName("embedding_backend")]
    public string EmbeddingBackend { get; set; } = "ollama";

    [JsonPropertyName("api_provider")]
    public string ApiProvider { get; set; } = "openai";

    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("api_model")]
    public string ApiModel { get; set; } = "";

    [JsonPropertyName("max_context_chunks")]
    public int MaxContextChunks { get; set; } = 5;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.3;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2048;

    [JsonPropertyName("repeat_penalty")]
    public double RepeatPenalty { get; set; } = 1.5;

    [JsonPropertyName("max_history_messages")]
    public int MaxHistoryMessages { get; set; } = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Finds and loads config from Models/Chat/chat_config.json.
    /// Returns default config if file not found.
    /// </summary>
    public static ChatConfig Load()
    {
        var path = FindConfigPath();
        if (path == null || !File.Exists(path))
            return new ChatConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ChatConfig>(json, JsonOptions) ?? new ChatConfig();
        }
        catch
        {
            return new ChatConfig();
        }
    }

    /// <summary>
    /// Saves config to Models/Chat/chat_config.json.
    /// </summary>
    public void Save()
    {
        var path = FindConfigPath() ?? CreateDefaultPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static string? FindConfigPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(baseDir, "Models", "Chat", "chat_config.json");
        if (File.Exists(path)) return path;

        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            dir = dir.Parent;
            if (dir == null) break;
            path = Path.Combine(dir.FullName, "Models", "Chat", "chat_config.json");
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private static string CreateDefaultPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "Models", "Chat", "chat_config.json");
    }
}
