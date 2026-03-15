namespace MVXTester.Chat;

/// <summary>
/// Abstraction for LLM chat generation (Ollama, OpenAI, Claude, Gemini).
/// </summary>
public interface IChatService
{
    /// <summary>Send a single prompt and get the full response.</summary>
    Task<string> ChatAsync(
        string prompt,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default);

    /// <summary>Stream response tokens as they are generated.</summary>
    IAsyncEnumerable<string> ChatStreamAsync(
        string prompt,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default);

    /// <summary>Send a prompt with an image attachment.</summary>
    Task<string> ChatWithImageAsync(
        string prompt,
        byte[] imageData,
        string? systemPrompt = null,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default);

    /// <summary>Check if the service is reachable.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Display name for the current model.</summary>
    string ModelName { get; }
}

/// <summary>
/// A single message in the chat conversation.
/// </summary>
public sealed class ChatMessage
{
    public ChatRole Role { get; init; }
    public string Content { get; set; } = "";
    public byte[]? ImageData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public static ChatMessage User(string content, byte[]? imageData = null) =>
        new() { Role = ChatRole.User, Content = content, ImageData = imageData };

    public static ChatMessage Assistant(string content) =>
        new() { Role = ChatRole.Assistant, Content = content };
}

public enum ChatRole
{
    User,
    Assistant,
    System
}
