using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MVXTester.Chat;

/// <summary>
/// Prompt templates loaded from Models/Chat/prompts.json.
/// Separating prompts from code allows tuning without recompilation.
/// </summary>
public sealed class PromptConfig
{
    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = "";

    [JsonPropertyName("image_system_prompt")]
    public string ImageSystemPrompt { get; set; } = "";

    [JsonPropertyName("fallback_system_prompt")]
    public string FallbackSystemPrompt { get; set; } = "";

    [JsonPropertyName("fallback_disclaimer")]
    public string FallbackDisclaimer { get; set; } = "";

    [JsonPropertyName("rag_context_instruction")]
    public string RagContextInstruction { get; set; } = "";

    [JsonPropertyName("image_captioning_prompt")]
    public string ImageCaptioningPrompt { get; set; } = "이 이미지에 보이는 버튼, 메뉴, 노드 이름을 한국어로만 나열하세요. 1줄.";

    [JsonPropertyName("off_topic_response")]
    public string OffTopicResponse { get; set; } = "MVXTester 관련 질문만 답변할 수 있습니다.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Load prompts from Models/Chat/prompts.json.
    /// All prompt content is managed in prompts.json only (no hardcoded defaults).
    /// </summary>
    public static PromptConfig Load()
    {
        var path = FindPath();
        if (path == null || !File.Exists(path))
            return new PromptConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PromptConfig>(json, JsonOptions) ?? new PromptConfig();
        }
        catch
        {
            return new PromptConfig();
        }
    }

    private static string? FindPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(baseDir, "Models", "Chat", "prompts.json");
        if (File.Exists(path)) return path;

        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            dir = dir.Parent;
            if (dir == null) break;
            path = Path.Combine(dir.FullName, "Models", "Chat", "prompts.json");
            if (File.Exists(path)) return path;
        }

        return null;
    }
}
