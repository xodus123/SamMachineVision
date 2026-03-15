


using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MVXTester.Chat;

/// <summary>
/// A single chunk of help documentation with optional embedding vector.
/// </summary>
public sealed class DocumentChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
}

/// <summary>
/// In-memory vector store for RAG document chunks.
/// Supports cosine similarity search and JSON serialization for caching.
/// </summary>
public sealed class RagDocumentStore
{
    private readonly List<DocumentChunk> _chunks = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // BM25 parameters
    private const float K1 = 1.2f;  // term frequency saturation
    private const float B = 0.75f;  // document length normalization
    private float _avgDocLength;
    private float[]? _docLengths;
    private bool _bm25Ready;

    public IReadOnlyList<DocumentChunk> Chunks => _chunks;
    public int Count => _chunks.Count;
    public bool HasEmbeddings => _chunks.Count > 0 && _chunks[0].Embedding != null;

    /// <summary>Add chunks to the store.</summary>
    public void AddChunks(IEnumerable<DocumentChunk> chunks)
    {
        _chunks.AddRange(chunks);
    }

    /// <summary>
    /// Hybrid search: Reciprocal Rank Fusion (RRF) of vector and keyword results.
    /// RRF uses rank positions instead of raw scores, avoiding score scale mismatch.
    /// Falls back to keyword-only if no embeddings available.
    /// </summary>
    public List<DocumentChunk> HybridSearch(
        float[]? queryEmbedding, string query, int topK = 5)
    {
        if (_chunks.Count == 0) return new List<DocumentChunk>();

        bool hasVector = queryEmbedding != null
            && queryEmbedding.Length > 0
            && HasEmbeddings;

        // 1. Keyword scores (always available)
        var keywordScores = ComputeKeywordScores(query);

        // 2. Vector scores (if embeddings exist)
        var vectorScores = hasVector
            ? ComputeVectorScores(queryEmbedding!)
            : null;

        // 3. RRF (Reciprocal Rank Fusion): 1/(k + rank)
        // Weighted RRF: 벡터 0.7 + BM25 0.3 (의미 검색 우선)
        const float k = 60f;
        const float vectorWeight = 0.7f;
        const float keywordWeight = 0.3f;
        var rrfScores = new float[_chunks.Count];

        // BM25 순위 계산
        var kwRanked = Enumerable.Range(0, _chunks.Count)
            .Where(i => keywordScores[i] > 0)
            .OrderByDescending(i => keywordScores[i])
            .ToList();
        for (int rank = 0; rank < kwRanked.Count; rank++)
            rrfScores[kwRanked[rank]] += keywordWeight / (k + rank + 1);

        // 벡터 순위 계산 (있을 때만)
        if (hasVector && vectorScores != null)
        {
            var vecRanked = Enumerable.Range(0, _chunks.Count)
                .Where(i => vectorScores[i] > 0)
                .OrderByDescending(i => vectorScores[i])
                .ToList();
            for (int rank = 0; rank < vecRanked.Count; rank++)
                rrfScores[vecRanked[rank]] += vectorWeight / (k + rank + 1);
        }

        // 4. 결과 정렬
        var combined = new List<(DocumentChunk chunk, float score)>();
        for (int i = 0; i < _chunks.Count; i++)
        {
            if (rrfScores[i] > 0)
                combined.Add((_chunks[i], rrfScores[i]));
        }

        var results = combined
            .OrderByDescending(x => x.score)
            .Take(topK)
            .ToList();

        // 디버그 로그 (실시간 검색 확인용)
        try
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Chat");
            var logPath = Path.Combine(logDir, "rag_debug.log");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] Query: \"{query}\"");
            sb.AppendLine($"  Vector: {(hasVector ? "Yes" : "No (keyword-only)")}, Candidates: {combined.Count}/{_chunks.Count}");
            if (results.Count == 0)
            {
                sb.AppendLine("  Result: NONE (fallback 모드로 전환)");
            }
            else
            {
                for (int r = 0; r < results.Count; r++)
                {
                    var (chunk, score) = results[r];
                    var preview = chunk.Text.Length > 80 ? chunk.Text[..80] + "..." : chunk.Text;
                    preview = preview.Replace("\r", "").Replace("\n", " ");
                    sb.AppendLine($"  #{r + 1} [{score:F3}] ({chunk.Source}) {preview}");
                }
            }
            sb.AppendLine();
            File.AppendAllText(logPath, sb.ToString());
        }
        catch { /* 로그 실패 무시 */ }

        return results.Select(x => x.chunk).ToList();
    }

    /// <summary>
    /// Vector-only search (cosine similarity).
    /// </summary>
    public List<DocumentChunk> Search(float[] queryEmbedding, int topK = 5)
    {
        if (_chunks.Count == 0 || queryEmbedding.Length == 0)
            return new List<DocumentChunk>();

        var scored = _chunks
            .Where(c => c.Embedding != null && c.Embedding.Length == queryEmbedding.Length)
            .Select(c => (chunk: c, score: CosineSimilarity(queryEmbedding, c.Embedding!)))
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.chunk)
            .ToList();

        return scored;
    }

    /// <summary>
    /// Keyword-only fallback search.
    /// </summary>
    public List<DocumentChunk> SearchByKeyword(string query, int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || _chunks.Count == 0)
            return new List<DocumentChunk>();

        var scores = ComputeKeywordScores(query);

        return _chunks
            .Select((c, i) => (chunk: c, score: scores[i]))
            .Where(x => x.score > 0.01f)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.chunk)
            .ToList();
    }

    // ──────────── Scoring Internals ────────────

    /// <summary>
    /// Korean phonetic → English/technical term synonyms.
    /// Expands query keywords so "욜로" also searches for "yolo", "yolov8", etc.
    /// </summary>
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // AI/ML
        { "욜로", new[] { "yolo", "yolov8" } },
        { "yolo", new[] { "욜로", "yolov8" } },
        { "오씨알", new[] { "ocr", "tesseract" } },
        { "ocr", new[] { "오씨알", "tesseract" } },
        // MediaPipe
        { "미디어파이프", new[] { "mediapipe" } },
        { "mediapipe", new[] { "미디어파이프" } },
        { "얼굴인식", new[] { "face detection", "얼굴 검출", "facedetection" } },
        { "포즈", new[] { "pose", "포즈추정" } },
        { "손인식", new[] { "hand", "handlandmark" } },
        // Control flow
        { "루프", new[] { "loop", "forloop", "반복" } },
        { "반복", new[] { "loop", "forloop", "루프" } },
        { "loop", new[] { "루프", "forloop", "반복" } },
        { "조건", new[] { "if", "ifselect", "조건문" } },
        { "조건문", new[] { "if", "ifselect", "조건" } },
        // Processing
        { "캐니", new[] { "canny", "에지" } },
        { "canny", new[] { "캐니", "에지" } },
        { "에지", new[] { "edge", "canny", "캐니" } },
        { "edge", new[] { "에지", "canny" } },
        { "블러", new[] { "blur", "가우시안" } },
        { "blur", new[] { "블러", "가우시안" } },
        { "가우시안", new[] { "gaussian", "블러", "blur" } },
        { "이진화", new[] { "threshold", "임계값" } },
        { "임계값", new[] { "threshold", "이진화" } },
        { "threshold", new[] { "이진화", "임계값" } },
        { "모폴로지", new[] { "morphology", "침식", "팽창" } },
        { "morphology", new[] { "모폴로지" } },
        { "컨투어", new[] { "contour", "윤곽" } },
        { "contour", new[] { "컨투어", "윤곽" } },
        { "윤곽", new[] { "contour", "컨투어" } },
        { "템플릿", new[] { "template", "매칭" } },
        { "template", new[] { "템플릿" } },
        { "히스토그램", new[] { "histogram" } },
        { "histogram", new[] { "히스토그램" } },
        // Inspection
        { "불량", new[] { "defect", "결함", "검사" } },
        { "defect", new[] { "불량", "결함" } },
        { "측정", new[] { "measure", "measurement" } },
        { "measure", new[] { "측정" } },
        // Input
        { "카메라", new[] { "camera", "캡처" } },
        { "camera", new[] { "카메라" } },
        { "비디오", new[] { "video", "영상" } },
        { "video", new[] { "비디오", "영상" } },
        // Communication
        { "시리얼", new[] { "serial", "serialport", "rs232" } },
        { "serial", new[] { "시리얼" } },
        { "tcp", new[] { "tcpip", "소켓", "socket" } },
        { "소켓", new[] { "socket", "tcp" } },
        // Transform
        { "크롭", new[] { "crop", "자르기", "잘라내기" } },
        { "crop", new[] { "크롭", "자르기" } },
        { "리사이즈", new[] { "resize", "크기변환" } },
        { "resize", new[] { "리사이즈", "크기변환" } },
        { "회전", new[] { "rotate", "로테이트" } },
        { "rotate", new[] { "회전" } },
        { "플립", new[] { "flip", "뒤집기" } },
        { "flip", new[] { "플립", "뒤집기" } },
        // ROI
        { "roi", new[] { "관심영역", "크롭", "crop" } },
        { "관심영역", new[] { "roi", "crop" } },
        // Script
        { "파이썬", new[] { "python", "스크립트" } },
        { "python", new[] { "파이썬" } },
        { "스크립트", new[] { "script", "파이썬" } },
        { "script", new[] { "스크립트" } },
        // Color
        { "색변환", new[] { "cvtcolor", "색공간", "color" } },
        { "그레이", new[] { "grayscale", "gray", "흑백" } },
        { "grayscale", new[] { "그레이", "흑백" } },
        // Drawing
        { "바운딩박스", new[] { "boundingbox", "rectangle", "사각형" } },
        { "boundingbox", new[] { "바운딩박스" } },
    };

    /// <summary>
    /// Expand keywords with synonyms. Returns original + synonym keywords.
    /// </summary>
    private static string[] ExpandWithSynonyms(string[] keywords)
    {
        var expanded = new HashSet<string>(keywords, StringComparer.OrdinalIgnoreCase);
        foreach (var kw in keywords)
        {
            if (Synonyms.TryGetValue(kw, out var syns))
            {
                foreach (var s in syns)
                    expanded.Add(s);
            }
        }
        return expanded.ToArray();
    }

    /// <summary>
    /// Stop words: common query words that appear in most documents and don't help distinguish.
    /// These are filtered out before keyword matching.
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // 한국어 일반
        "해줘", "알려줘", "좀", "하는", "있는", "없는", "이거", "저거",
        "뭐야", "어떻게", "어디", "왜", "것", "거", "예제", "사용", "방법", "기능",
        "하나", "하나만", "이", "그", "저", "에서", "으로", "를", "을", "의",
        "나", "다", "한", "수", "중", "더", "좀더", "자세히", "목록",
        "어떤", "무슨", "언제", "얼마", "정도", "같은", "대해", "관련",
        // 영어 일반
        "the", "is", "are", "how", "what", "this", "that", "node", "please",
        "explain", "tell", "show", "about", "help", "use", "using", "list",
    };

    /// <summary>
    /// Initialize BM25 document length statistics (lazy, called once).
    /// </summary>
    private void InitBM25()
    {
        if (_bm25Ready || _chunks.Count == 0) return;

        _docLengths = new float[_chunks.Count];
        float totalLen = 0;
        for (int i = 0; i < _chunks.Count; i++)
        {
            _docLengths[i] = _chunks[i].Text.Length;
            totalLen += _docLengths[i];
        }
        _avgDocLength = totalLen / _chunks.Count;
        _bm25Ready = true;
    }

    /// <summary>
    /// Count non-overlapping occurrences of keyword in text.
    /// </summary>
    private static int CountOccurrences(string text, string keyword)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(keyword, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += keyword.Length;
        }
        return count;
    }

    /// <summary>
    /// Compute BM25 keyword relevance scores for all chunks.
    /// Uses: BM25 scoring + IDF + synonym expansion + bigram/exact match bonus.
    /// </summary>
    private float[] ComputeKeywordScores(string query)
    {
        var scores = new float[_chunks.Count];
        if (string.IsNullOrWhiteSpace(query)) return scores;

        InitBM25();
        int N = _chunks.Count;
        // 초성 분리된 한글을 정규화 ("크로ㅂ" → "크롭")
        var queryNormalized = KoreanTextNormalizer.Normalize(query);
        var queryLower = queryNormalized.ToLowerInvariant();

        // 영어/한글 경계에서 자동 분리 ("contour목록" → "contour", "목록")
        var separated = System.Text.RegularExpressions.Regex.Replace(
            queryLower, @"([a-z0-9])([가-힣])", "$1 $2");
        separated = System.Text.RegularExpressions.Regex.Replace(
            separated, @"([가-힣])([a-z0-9])", "$1 $2");

        var allTokens = separated
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(k => k.Length >= 2)
            .ToArray();
        var baseKeywords = allTokens.Where(k => !StopWords.Contains(k)).ToArray();

        // 불용어 제거 후 키워드가 없으면 원래 토큰 사용
        if (baseKeywords.Length == 0) baseKeywords = allTokens;
        if (baseKeywords.Length == 0) return scores;

        // Expand with synonyms
        var keywords = ExpandWithSynonyms(baseKeywords);

        // Pre-compute document frequency (DF) for each keyword
        var textLowers = new string[N];
        for (int i = 0; i < N; i++)
            textLowers[i] = _chunks[i].Text.ToLowerInvariant();

        var df = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kw in keywords)
        {
            int count = 0;
            for (int i = 0; i < N; i++)
            {
                if (textLowers[i].Contains(kw))
                    count++;
            }
            df[kw] = count;
        }

        // BM25 scoring
        float maxRaw = 0;
        for (int i = 0; i < N; i++)
        {
            float score = 0;
            var docLen = _docLengths![i];

            foreach (var kw in keywords)
            {
                int tf = CountOccurrences(textLowers[i], kw);
                if (tf == 0) continue;

                // IDF: log((N - df + 0.5) / (df + 0.5) + 1)
                float idf = MathF.Log((N - df[kw] + 0.5f) / (df[kw] + 0.5f) + 1f);

                // BM25 term score: idf * (tf * (k1+1)) / (tf + k1 * (1 - b + b * docLen/avgDocLen))
                float tfNorm = (tf * (K1 + 1f)) / (tf + K1 * (1f - B + B * docLen / _avgDocLength));
                score += idf * tfNorm;
            }

            // Bigram bonus: consecutive keyword proximity
            for (int bi = 0; bi < keywords.Length - 1; bi++)
            {
                var bigram = $"{keywords[bi]} {keywords[bi + 1]}";
                if (textLowers[i].Contains(bigram))
                    score += 2f;
            }

            // Exact query match bonus
            if (textLowers[i].Contains(queryLower))
                score += 3f;

            scores[i] = score;
            if (score > maxRaw) maxRaw = score;
        }

        // Normalize to 0~1
        if (maxRaw > 0)
        {
            for (int i = 0; i < scores.Length; i++)
                scores[i] /= maxRaw;
        }

        return scores;
    }

    /// <summary>
    /// Compute normalized vector similarity scores (0~1) for all chunks.
    /// </summary>
    private float[] ComputeVectorScores(float[] queryEmbedding)
    {
        var scores = new float[_chunks.Count];
        float maxScore = 0;

        for (int i = 0; i < _chunks.Count; i++)
        {
            var emb = _chunks[i].Embedding;
            if (emb != null && emb.Length == queryEmbedding.Length)
            {
                scores[i] = CosineSimilarity(queryEmbedding, emb);
                if (scores[i] > maxScore) maxScore = scores[i];
            }
        }

        // Normalize to 0~1
        if (maxScore > 0)
        {
            for (int i = 0; i < scores.Length; i++)
                scores[i] /= maxScore;
        }

        return scores;
    }

    /// <summary>Save the store (with embeddings) to a JSON file.</summary>
    public void SaveToFile(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_chunks, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>Load the store from a JSON cache file.</summary>
    public static RagDocumentStore? LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            var chunks = JsonSerializer.Deserialize<List<DocumentChunk>>(json, JsonOptions);
            if (chunks == null || chunks.Count == 0) return null;

            var store = new RagDocumentStore();
            store._chunks.AddRange(chunks);
            return store;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Find the rag_index.json path.</summary>
    public static string GetDefaultIndexPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(baseDir, "Models", "Chat", "rag_index.json");

        // Fallback: search parent directories
        if (!Directory.Exists(Path.GetDirectoryName(path)!))
        {
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5; i++)
            {
                dir = dir.Parent;
                if (dir == null) break;
                var candidate = Path.Combine(dir.FullName, "Models", "Chat");
                if (Directory.Exists(candidate))
                    return Path.Combine(candidate, "rag_index.json");
            }
        }

        return path;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom > 0 ? dot / denom : 0f;
    }
}
