namespace MVXTester.Chat;

/// <summary>
/// Abstraction for text/image embedding generation.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Generate embedding vector for text.</summary>
    Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default);

    /// <summary>Embedding vector dimension count.</summary>
    int Dimensions { get; }
}
