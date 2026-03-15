using System.IO;
using System.Text;
using MVXTester.Core.Registry;

namespace MVXTester.Chat;

/// <summary>
/// RAG (Retrieval-Augmented Generation) pipeline orchestrator.
/// Handles: content extraction → embedding → search → prompt construction → LLM response.
/// </summary>
public sealed class RagEngine : IDisposable
{
    private ChatConfig _config;
    private PromptConfig _prompts;
    private IChatService _chatService;
    private volatile IEmbeddingService? _embeddingService;
    private OllamaModelManager? _modelManager;
    private NodeDirectLookup? _directLookup;
    private RagDocumentStore _store;
    private bool _isInitialized;
    private bool _isIndexing;
    private volatile bool _embeddingReady;

    public bool IsInitialized => _isInitialized;
    public bool IsIndexing => _isIndexing;
    public int DocumentCount => _store.Count;
    public string CurrentModel => _chatService.ModelName;
    public OllamaModelManager? ModelManager => _modelManager;

    public event Action<string>? StatusChanged;

    public RagEngine(ChatConfig config)
    {
        _config = config;
        _prompts = PromptConfig.Load();
        _chatService = CreateChatService(config);
        _store = new RagDocumentStore();
    }

    /// <summary>
    /// Initialize the RAG engine: setup Ollama → load/build index.
    /// </summary>
    public async Task InitializeAsync(
        NodeRegistry? registry = null,
        Dictionary<string, (string Desc, string Apps)>? koreanDescriptions = null,
        Dictionary<string, string>? categoryNames = null,
        CancellationToken ct = default)
    {
        if (_isInitialized) return;

        // NodeRegistry 직접 조회 엔진 생성 (LLM 없이 노드 정보 응답)
        if (registry != null)
            _directLookup = new NodeDirectLookup(registry, koreanDescriptions, categoryNames);

        // Step 1: Ensure Ollama is running + models available
        // Ollama provider → 챗 모델 + 임베딩 모델 모두 필요
        // API provider → 임베딩(nomic-embed-text)용으로 Ollama 필요
        bool needsOllama = _config.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            || _config.EmbeddingBackend.Equals("ollama", StringComparison.OrdinalIgnoreCase);

        if (needsOllama)
        {
            await EnsureOllamaReadyAsync(ct);
        }

        // Step 2: Load pre-built RAG index (shipped with app)
        StatusChanged?.Invoke("RAG 인덱스 로드 중...");

        var indexPath = RagDocumentStore.GetDefaultIndexPath();
        var cached = RagDocumentStore.LoadFromFile(indexPath);

        if (cached != null && cached.Count > 0)
        {
            _store = cached;
            _isInitialized = true;

            // 쿼리 임베딩용 서비스 생성 + 워밍업 (벡터 검색에 필요)
            try
            {
                _embeddingService = CreateEmbeddingService(_config);
            }
            catch (Exception ex)
            {
                _embeddingService = null;
                LogMessage($"임베딩 서비스 생성 실패: {ex.GetType().Name}: {ex.Message}");
            }

            // 챗봇 즉시 사용 가능 (키워드 검색)
            // 임베딩 워밍업은 백그라운드 → 완료 후 하이브리드 검색 자동 전환
            if (_embeddingService != null && _store.HasEmbeddings)
            {
                StatusChanged?.Invoke($"RAG 준비 완료 ({_store.Count}개 문서, 키워드 검색) — 벡터 검색 로드 중...");
                _ = WarmupEmbeddingInBackgroundAsync().ContinueWith(
                    t => { try { var _ = t.Exception; } catch { } },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            else
            {
                StatusChanged?.Invoke($"RAG 준비 완료 ({_store.Count}개 문서, 키워드 검색)");
            }
            return;
        }

        // Fallback: build index from scratch (pre-built index missing)
        await RebuildIndexAsync(registry, koreanDescriptions, categoryNames, ct);
    }

    /// <summary>
    /// Background warmup: load nomic-embed-text into GPU.
    /// 60초 타임아웃, 10초 간격 5회 재시도. 챗봇은 키워드 검색으로 즉시 사용 가능.
    /// </summary>
    private async Task WarmupEmbeddingInBackgroundAsync()
    {
        const int maxRetries = 5;
        const int retryDelayMs = 10_000;
        const int warmupTimeoutSec = 60;

        var embeddingService = _embeddingService;
        if (embeddingService == null) return;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 1)
                    StatusChanged?.Invoke($"벡터 검색 로드 재시도 중... ({attempt}/{maxRetries})");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(warmupTimeoutSec));
                await embeddingService.EmbedTextAsync("warmup", cts.Token);
                _embeddingReady = true;
                StatusChanged?.Invoke($"RAG 준비 완료 ({_store.Count}개 문서, 하이브리드 검색)");

                try
                {
                    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Chat");
                    File.AppendAllText(Path.Combine(logDir, "rag_debug.log"),
                        $"[{DateTime.Now:HH:mm:ss}] WARMUP OK (attempt {attempt}/{maxRetries})\n");
                }
                catch { }

                return;
            }
            catch (Exception ex)
            {
                try
                {
                    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Chat");
                    File.AppendAllText(Path.Combine(logDir, "rag_debug.log"),
                        $"[{DateTime.Now:HH:mm:ss}] WARMUP attempt {attempt}/{maxRetries} FAILED: {ex.GetType().Name}: {ex.Message}\n");
                }
                catch { }

                if (attempt < maxRetries)
                {
                    try { await Task.Delay(retryDelayMs); } catch { return; }
                    continue;
                }

                _embeddingService = null;
                _embeddingReady = false;
                StatusChanged?.Invoke($"벡터 검색 실패 — BM25 키워드 검색만 사용");
            }
        }
    }

    /// <summary>
    /// Ensure Ollama is installed, running, and models are pulled.
    /// Returns false if setup needs user interaction (install permission).
    /// </summary>
    private async Task EnsureOllamaReadyAsync(CancellationToken ct)
    {
        _modelManager = new OllamaModelManager(_config.OllamaBaseUrl);
        _modelManager.StatusChanged += s => StatusChanged?.Invoke(s);

        bool serverReady = false;

        // Quick check: already running?
        if (await _modelManager.IsRunningAsync(ct))
        {
            // 이미 실행 중 → 그대로 사용 (재시작하지 않음)
            StatusChanged?.Invoke("Ollama 서버 연결됨");
            serverReady = true;
        }
        // Installed but not running? Start it (MAX_LOADED_MODELS=3 포함).
        else if (OllamaModelManager.IsInstalled())
        {
            serverReady = await _modelManager.StartAsync(ct);
        }

        if (!serverReady)
        {
            // Not installed — signal to ViewModel that install is needed
            StatusChanged?.Invoke("NEED_INSTALL");
            return;
        }

        // Ensure required models are installed (auto-pull if missing)
        var models = new[] { _config.OllamaChatModel, _config.OllamaVisionModel, _config.OllamaEmbedModel }
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToArray();

        if (models.Length > 0)
        {
            await _modelManager.EnsureModelsAsync(models, ct);

            // 모델을 VRAM에 미리 로드 (Ollama는 첫 요청 시에만 로드함)
            await _modelManager.WarmupModelsAsync(models, ct);
        }
    }

    /// <summary>
    /// Run Ollama setup (install + start only, no model pull).
    /// Called from ViewModel after user confirms installation.
    /// </summary>
    public async Task<bool> RunOllamaSetupAsync(CancellationToken ct = default)
    {
        _modelManager ??= new OllamaModelManager(_config.OllamaBaseUrl);
        _modelManager.StatusChanged += s => StatusChanged?.Invoke(s);

        if (!OllamaModelManager.IsInstalled())
        {
            var installed = await _modelManager.InstallAsync(ct);
            if (!installed) return false;
        }

        var started = await _modelManager.StartAsync(ct);
        if (!started) return false;

        // 필요한 모델 자동 다운로드
        var models = new[] { _config.OllamaChatModel, _config.OllamaVisionModel, _config.OllamaEmbedModel }
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToArray();

        if (models.Length > 0)
        {
            await _modelManager.EnsureModelsAsync(models, ct);
        }

        return true;
    }

    /// <summary>
    /// Rebuild the RAG index from scratch.
    /// </summary>
    public async Task RebuildIndexAsync(
        NodeRegistry? registry = null,
        Dictionary<string, (string Desc, string Apps)>? koreanDescriptions = null,
        Dictionary<string, string>? categoryNames = null,
        CancellationToken ct = default)
    {
        _isIndexing = true;
        StatusChanged?.Invoke("도움말 텍스트 추출 중...");

        try
        {
            // Extract content
            var chunks = HelpContentExtractor.ExtractAll(registry, koreanDescriptions, categoryNames);
            _store = new RagDocumentStore();
            _store.AddChunks(chunks);

            // Try embedding if service available
            _embeddingService = CreateEmbeddingService(_config);
            if (_embeddingService != null)
            {
                StatusChanged?.Invoke($"임베딩 생성 중... (0/{chunks.Count})");

                int done = 0;
                foreach (var chunk in chunks)
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        chunk.Embedding = await _embeddingService.EmbedTextAsync(chunk.Text, ct);
                    }
                    catch
                    {
                        // If embedding fails, continue without embeddings (will use keyword search)
                        _embeddingService = null;
                        break;
                    }
                    done++;
                    if (done % 10 == 0)
                        StatusChanged?.Invoke($"임베딩 생성 중... ({done}/{chunks.Count})");
                }
            }

            // Save text index cache
            var indexPath = RagDocumentStore.GetDefaultIndexPath();
            try
            {
                _store.SaveToFile(indexPath);

                // 소스 Data/ 폴더에도 동기화 (빌드 시 릴리즈에 반영되도록)
                var sourceDataPath = FindSourceDataIndexPath();
                if (sourceDataPath != null && sourceDataPath != indexPath)
                {
                    File.Copy(indexPath, sourceDataPath, overwrite: true);
                    LogMessage($"인덱스 소스 동기화: {sourceDataPath}");
                }
            }
            catch { /* ignore save errors */ }

            _isInitialized = true;
            StatusChanged?.Invoke($"RAG 준비 완료 ({_store.Count}개 문서)");
        }
        finally
        {
            _isIndexing = false;
        }
    }

    /// <summary>
    /// Answer a user question using RAG pipeline.
    /// Falls back to direct LLM query (with disclaimer) when no context found.
    /// </summary>
    public async Task<string> AskAsync(
        string question,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default)
    {
        // 직접 조회: 노드 카테고리/목록/상세는 LLM 없이 즉시 응답
        LogMessage($"[DIRECT] lookup={(_directLookup != null ? "있음" : "없음")}, question=\"{question}\"");
        if (_directLookup != null)
        {
            try
            {
                var directAnswer = _directLookup.TryAnswer(question);
                LogMessage($"[DIRECT] 결과={directAnswer?.Length ?? 0}자");
                if (directAnswer != null)
                {
                    LogMessage($"[DIRECT] HIT: \"{question}\" → {directAnswer[..Math.Min(directAnswer.Length, 100)]}");
                    return "@@DIRECT@@" + directAnswer;
                }
                LogMessage($"[DIRECT] MISS: \"{question}\" → RAG 경로로 전환");
            }
            catch (Exception ex)
            {
                LogMessage($"[DIRECT] ERROR: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var searchQuery = EnrichQueryWithHistory(question, history);
        var chunks = await RetrieveContextAsync(searchQuery, ct);

        if (chunks.Count == 0)
        {
            if (!IsPotentiallyOnTopic(question))
                return _prompts.OffTopicResponse;
            return await FallbackDirectAsync(question, history, ct);
        }

        var prompt = BuildRagPrompt(question, chunks);
        return await _chatService.ChatAsync(prompt, _prompts.SystemPrompt, history, ct);
    }

    /// <summary>
    /// Stream answer tokens using RAG pipeline.
    /// </summary>
    public IAsyncEnumerable<string> AskStreamAsync(
        string question,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default)
    {
        return AskStreamInternalAsync(question, history, ct);
    }

    private async IAsyncEnumerable<string> AskStreamInternalAsync(
        string question,
        IReadOnlyList<ChatMessage>? history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // 직접 조회: 노드 카테고리/목록/상세는 LLM 없이 즉시 응답
        string? _directResult = null;
        if (_directLookup != null)
        {
            try { _directResult = _directLookup.TryAnswer(question); }
            catch (Exception ex) { LogMessage($"[DIRECT-STREAM] ERROR: {ex.GetType().Name}: {ex.Message}"); }
        }
        if (_directResult != null)
        {
            LogMessage($"[DIRECT-STREAM] HIT: \"{question}\"");
            // "@@DIRECT@@" 접두사로 ViewModel에서 후처리(StripMarkdown) 건너뛰도록 표시
            yield return "@@DIRECT@@" + _directResult;
            yield break;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        // 짧은 후속 질문이면 이전 대화 맥락을 RAG 검색에 포함
        var searchQuery = EnrichQueryWithHistory(question, history);
        var chunks = await RetrieveContextAsync(searchQuery, ct);
        var retrievalMs = sw.ElapsedMilliseconds;

        if (chunks.Count == 0)
        {
            if (!IsPotentiallyOnTopic(question))
            {
                yield return _prompts.OffTopicResponse;
                LogTiming(question, "off_topic", retrievalMs, sw.ElapsedMilliseconds);
                yield break;
            }

            // Fallback: direct LLM with disclaimer prefix
            yield return _prompts.FallbackDisclaimer;
            var firstToken = true;
            await foreach (var token in _chatService.ChatStreamAsync(
                question, _prompts.FallbackSystemPrompt, history, ct))
            {
                if (firstToken) { firstToken = false; LogTiming(question, "fallback", retrievalMs, sw.ElapsedMilliseconds); }
                yield return token;
            }
            if (firstToken) LogTiming(question, "fallback", retrievalMs, sw.ElapsedMilliseconds);
            LogTiming(question, "fallback_done", retrievalMs, sw.ElapsedMilliseconds);
            yield break;
        }

        var prompt = BuildRagPrompt(question, chunks);
        var first = true;
        await foreach (var token in _chatService.ChatStreamAsync(prompt, _prompts.SystemPrompt, history, ct))
        {
            if (first) { first = false; LogTiming(question, "first_token", retrievalMs, sw.ElapsedMilliseconds); }
            yield return token;
        }
        LogTiming(question, "done", retrievalMs, sw.ElapsedMilliseconds);
    }

    private static void LogMessage(string message)
    {
        try
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Chat");
            var logPath = Path.Combine(logDir, "rag_debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { }
    }

    private static void LogTiming(string query, string phase, long retrievalMs, long totalMs)
    {
        try
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "Chat");
            var logPath = Path.Combine(logDir, "rag_debug.log");
            var msg = phase switch
            {
                "first_token" => $"[{DateTime.Now:HH:mm:ss}] TIMING: \"{query}\" 검색={retrievalMs}ms, 첫토큰={totalMs}ms\n",
                "done" => $"[{DateTime.Now:HH:mm:ss}] TIMING: \"{query}\" 총응답={totalMs}ms ({totalMs / 1000.0:F1}초)\n\n",
                "fallback" => $"[{DateTime.Now:HH:mm:ss}] TIMING: \"{query}\" [fallback] 검색={retrievalMs}ms, 첫토큰={totalMs}ms\n",
                "fallback_done" => $"[{DateTime.Now:HH:mm:ss}] TIMING: \"{query}\" [fallback] 총응답={totalMs}ms ({totalMs / 1000.0:F1}초)\n\n",
                "off_topic" => $"[{DateTime.Now:HH:mm:ss}] [OFF_TOPIC] \"{query}\" 차단 ({totalMs}ms)\n\n",
                _ => ""
            };
            if (msg.Length > 0) File.AppendAllText(logPath, msg);
        }
        catch { }
    }

    private async Task<string> FallbackDirectAsync(
        string question,
        IReadOnlyList<ChatMessage>? history,
        CancellationToken ct)
    {
        try
        {
            var response = await _chatService.ChatAsync(question, _prompts.FallbackSystemPrompt, history, ct);
            return _prompts.FallbackDisclaimer + response;
        }
        catch (Exception ex)
        {
            LogMessage($"Fallback LLM 실패: {ex.GetType().Name}: {ex.Message}");
            return _prompts.OffTopicResponse;
        }
    }

    public async Task<string> AskWithImageAsync(
        string question,
        byte[] imageData,
        IReadOnlyList<ChatMessage>? history = null,
        CancellationToken ct = default)
    {
        // 이미지 첨부 시: 긴 텍스트가 명백히 도메인 외인 경우만 차단
        if (!string.IsNullOrWhiteSpace(question) &&
            !IsPotentiallyOnTopic(question) &&
            question.Length > 30)
        {
            return !string.IsNullOrEmpty(_prompts.OffTopicResponse)
                ? _prompts.OffTopicResponse
                : "MVXTester 관련 질문만 답변할 수 있습니다.";
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var resized = ResizeImageIfNeeded(imageData, MaxImageDimension);

        LogMessage($"[IMAGE] 질문: \"{question}\"");

        // 1단계: VLM으로 이미지 캡셔닝 (이미지 내용을 텍스트로 변환)
        string caption;
        try
        {
            var rawCaption = await _chatService.ChatWithImageAsync(
                _prompts.ImageCaptioningPrompt,
                resized, null, null, ct);
            // 캡션 정리: EOS 토큰 이후 제거 + 중국어 제거 + 100자 제한
            var cleaned = rawCaption ?? "";
            foreach (var eos in new[] { "<|endoftext|>", "<|im_end|>", "<|end|>", "\n\n" })
            {
                var idx = cleaned.IndexOf(eos);
                if (idx >= 0) cleaned = cleaned[..idx];
            }
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[\u4E00-\u9FFF]+", "");
            // HTML 태그 제거
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<[^>]+>", "");
            caption = cleaned.Length > 150 ? cleaned[..150] : cleaned;
            caption = caption.Trim();
            LogMessage($"[IMAGE] 캡셔닝 ({sw.ElapsedMilliseconds}ms): \"{caption}\"");
        }
        catch (Exception ex)
        {
            caption = "";
            LogMessage($"[IMAGE] 캡셔닝 실패: {ex.GetType().Name}: {ex.Message}");
        }

        // 2단계: 직접 조회 시도 (캡션에서 노드명 감지 시 LLM 없이 즉시 응답)
        if (_directLookup != null)
        {
            var directAnswer = _directLookup.TryAnswer(question);
            if (directAnswer == null && !string.IsNullOrWhiteSpace(caption))
                directAnswer = _directLookup.TryAnswer($"{caption} {question}");
            if (directAnswer != null)
            {
                LogMessage($"[IMAGE] Direct lookup hit: {directAnswer[..Math.Min(directAnswer.Length, 80)]}");
                return directAnswer;
            }
        }

        // 3단계: 캡션 + 사용자 질문으로 RAG 검색 (RetrieveContextAsync 공유, 이미지는 2개)
        var searchQuery = string.IsNullOrWhiteSpace(caption)
            ? question
            : $"{caption} {question}";
        LogMessage($"[IMAGE] RAG 검색 쿼리: \"{searchQuery.Replace("\n", " ").Trim()}\"");

        var chunks = await RetrieveContextAsync(searchQuery, ct, topKOverride: 3);
        LogMessage($"[IMAGE] RAG 결과: {chunks.Count}개 청크");
        foreach (var c in chunks)
            LogMessage($"[IMAGE]   - ({c.Source}) {c.Text.Replace("\n", " ")[..Math.Min(c.Text.Length, 80)]}...");

        // 3단계: RAG 결과 + 이미지 + 질문으로 최종 답변
        var prompt = chunks.Count > 0
            ? BuildRagPrompt(question, chunks)
            : question;
        LogMessage($"[IMAGE] 최종 프롬프트 길이: {prompt.Length}자");

        var response = await _chatService.ChatWithImageAsync(prompt, resized, _prompts.ImageSystemPrompt, history, ct);
        LogMessage($"[IMAGE] 답변 완료 ({sw.ElapsedMilliseconds}ms, {response.Length}자)\n");
        return response;
    }

    private const int MaxImageDimension = 768;

    /// <summary>
    /// Resize image to fit within maxDim x maxDim to reduce memory/speed.
    /// Returns original if already small enough or if OpenCV is unavailable.
    /// </summary>
    private static byte[] ResizeImageIfNeeded(byte[] imageData, int maxDim)
    {
        try
        {
            using var mat = OpenCvSharp.Mat.FromImageData(imageData);
            if (mat.Empty()) return imageData;

            var h = mat.Height;
            var w = mat.Width;
            if (h <= maxDim && w <= maxDim) return imageData;

            var scale = Math.Min((double)maxDim / w, (double)maxDim / h);
            var newW = (int)(w * scale);
            var newH = (int)(h * scale);

            using var resized = new OpenCvSharp.Mat();
            OpenCvSharp.Cv2.Resize(mat, resized, new OpenCvSharp.Size(newW, newH));

            OpenCvSharp.Cv2.ImEncode(".jpg", resized,
                out var buf,
                new OpenCvSharp.ImageEncodingParam(OpenCvSharp.ImwriteFlags.JpegQuality, 85));
            return buf;
        }
        catch
        {
            return imageData;
        }
    }

    /// <summary>
    /// Find the best matching example project by keyword matching.
    /// Returns the example file name (without extension) or null if no good match.
    /// </summary>
    public Task<string?> FindExampleAsync(
        string question,
        Dictionary<string, string> exampleMapping,
        CancellationToken ct = default)
    {
        return Task.FromResult(FindExampleByKeyword(question, exampleMapping));
    }

    private static string? FindExampleByKeyword(
        string text, Dictionary<string, string> exampleMapping)
    {
        var normalized = KoreanTextNormalizer.Normalize(text);
        foreach (var (keyword, fileName) in exampleMapping.OrderByDescending(k => k.Key.Length))
        {
            if (normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return fileName;
        }
        return null;
    }

    /// <summary>Check if the chat backend is available.</summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return await _chatService.IsAvailableAsync(ct);
    }

    /// <summary>
    /// Update the configuration and recreate services.
    /// </summary>
    public void UpdateConfig(ChatConfig newConfig)
    {
        if (_chatService is IDisposable d1) d1.Dispose();
        if (_embeddingService is IDisposable d2) d2.Dispose();

        // _config is readonly field; update mutable reference for BuildRagPrompt
        _config = newConfig;
        _chatService = CreateChatService(newConfig);
        _embeddingService = CreateEmbeddingService(newConfig);
    }

    private async Task<List<DocumentChunk>> RetrieveContextAsync(
        string question, CancellationToken ct, int? topKOverride = null)
    {
        if (!_isInitialized || _store.Count == 0)
            return new List<DocumentChunk>();

        float[]? queryEmbedding = null;
        if (_store.HasEmbeddings && _embeddingService != null && _embeddingReady)
        {
            try
            {
                using var embedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                embedCts.CancelAfter(TimeSpan.FromSeconds(30));
                queryEmbedding = await _embeddingService.EmbedTextAsync(question, embedCts.Token);
            }
            catch (Exception ex)
            {
                LogMessage($"EMBED ERROR: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var topK = topKOverride ?? GetOptimalChunkCount(question);
        return _store.HybridSearch(queryEmbedding, question, topK);
    }

    /// <summary>
    /// 짧은 후속 질문에 이전 대화의 마지막 user 메시지를 결합하여 RAG 검색 품질 향상.
    /// 예: "아니 씨샾" + 이전 "시샾 스크립트 예제 알려줘" → "시샾 스크립트 예제 알려줘 씨샾"
    /// </summary>
    private static string EnrichQueryWithHistory(string question, IReadOnlyList<ChatMessage>? history)
    {
        if (question.Length >= 15 || history == null || history.Count == 0)
            return question;

        // 이전 대화에서 마지막 user 메시지 찾기
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i].Role == ChatRole.User && history[i].Content.Length > 10)
            {
                var prev = history[i].Content;
                if (prev.Length > 80) prev = prev[..80];
                return $"{prev} {question}";
            }
        }

        return question;
    }

    /// <summary>
    /// 질문 복잡도에 따라 청크 수를 동적 조절. 단순 질문은 적은 청크로 context 절약.
    /// </summary>
    private int GetOptimalChunkCount(string question)
    {
        if (question.Length < 20)
            return Math.Min(3, _config.MaxContextChunks);
        return _config.MaxContextChunks;
    }

    /// <summary>
    /// 도메인 키워드 존재 여부로 on-topic 판별. RAG 매칭 0개일 때 사용.
    /// </summary>
    private static bool IsPotentiallyOnTopic(string question)
    {
        var normalized = KoreanTextNormalizer.Normalize(question).ToLowerInvariant();
        return DomainKeywords.Any(k => normalized.Contains(k));
    }

    private static readonly string[] DomainKeywords =
    {
        // MVXTester
        "mvx", "노드", "파이프라인", "포트", "연결", "실행", "프로젝트",
        // 머신비전
        "머신비전", "비전", "영상", "이미지", "사진", "화면", "픽셀",
        "카메라", "캡처", "프레임",
        // 이미지 처리
        "필터", "블러", "에지", "canny", "threshold", "이진화", "임계",
        "모폴로지", "침식", "팽창", "컨투어", "윤곽", "히스토그램",
        "그레이", "색변환", "hsv", "rgb", "roi", "크롭", "리사이즈",
        "회전", "플립", "가우시안", "소벨", "라플라시안",
        // AI/검출
        "yolo", "ocr", "mediapipe", "얼굴", "포즈", "손인식", "객체검출",
        "템플릿", "매칭", "검출", "인식", "분류", "세그멘테이션",
        // 통신
        "시리얼", "tcp", "소켓", "serial", "통신", "rs232", "modbus",
        // 프로그래밍
        "파이썬", "python", "스크립트", "script", "c#", "csharp",
        // OpenCV/일반
        "opencv", "검사", "측정", "불량", "결함", "defect", "inspection",
        // 앱 관련
        "설치", "설정", "오류", "에러", "트러블", "도움말",
        "드래그", "속성", "파라미터", "프로퍼티",
    };

    private string BuildRagPrompt(string question, List<DocumentChunk> context)
    {
        if (context.Count == 0)
            return question;

        var sb = new StringBuilder();
        sb.AppendLine("---");

        // 가장 관련 높은 청크를 마지막에 배치 (recency bias 활용)
        for (int i = context.Count - 1; i >= 0; i--)
        {
            sb.AppendLine($"{context.Count - i}. {context[i].Text.Trim()}");
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"질문: {question}");
        sb.Append(_prompts.RagContextInstruction);

        return sb.ToString();
    }

    /// <summary>
    /// 소스 Data/rag_index.json 경로를 찾는다. 빌드 출력 → 상위로 탐색.
    /// </summary>
    private static string? FindSourceDataIndexPath()
    {
        try
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 8; i++)
            {
                dir = dir.Parent;
                if (dir == null) break;

                // 직접 Data/ 폴더 탐색
                var candidate = Path.Combine(dir.FullName, "Data", "rag_index.json");
                if (File.Exists(candidate))
                    return candidate;

                // src/ 레벨에서 MVXTester.Chat/Data/ 탐색
                var chatData = Path.Combine(dir.FullName, "MVXTester.Chat", "Data", "rag_index.json");
                if (File.Exists(chatData))
                    return chatData;
            }
        }
        catch { }
        return null;
    }

    private static IChatService CreateChatService(ChatConfig config)
    {
        return config.Provider.ToLowerInvariant() switch
        {
            "openai" or "claude" or "gemini" => new ApiChatService(config),
            _ => new OllamaChatService(config)
        };
    }

    private static IEmbeddingService? CreateEmbeddingService(ChatConfig config)
    {
        return config.EmbeddingBackend.ToLowerInvariant() switch
        {
            "ollama" => new OllamaEmbeddingService(config),
            _ => null
        };
    }

    public void Dispose()
    {
        _modelManager?.Dispose(); // Stops Ollama process if we started it
        if (_chatService is IDisposable d1) d1.Dispose();
        if (_embeddingService is IDisposable d2) d2.Dispose();
    }
}
