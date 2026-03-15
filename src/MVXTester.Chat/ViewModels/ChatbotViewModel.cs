// fire-and-forget async (BeginInvoke, background tasks) 의도적 사용
#pragma warning disable CS4014

using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MVXTester.Chat;
using MVXTester.Core.Registry;
using System.Windows.Input;

namespace MVXTester.Chat.ViewModels;

/// <summary>
/// Chat message display model for the UI.
/// </summary>
/// <summary>
/// ViewModel for the AI Chatbot tab in the right panel.
/// </summary>
public partial class ChatbotViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private RagEngine? _ragEngine;
    private bool _managerStatusSubscribed;
    private ChatConfig _config;
    private CancellationTokenSource? _sendCts;
    private byte[]? _attachedImageData;
    private Task? _setupTask; // Tracks ongoing Ollama init/model download

    /// <summary>
    /// 챗봇에서 예제 파일 로드를 요청할 때 발생하는 이벤트. 예제 파일명(확장자 제외)을 전달합니다.
    /// </summary>
    public event Action<string>? LoadExampleRequested;

    /// <summary>
    /// 챗봇에서 도움말 창을 열도록 요청할 때 발생하는 이벤트.
    /// </summary>
    public event Action? OpenHelpRequested;

    private static readonly Dictionary<string, string> ExampleMapping = new()
    {
        { "에지 검출", "01_EdgePartInspection" },
        { "edge", "01_EdgePartInspection" },
        { "canny", "01_EdgePartInspection" },
        { "색상 분류", "02_ColorClassification" },
        { "color classification", "02_ColorClassification" },
        { "바코드", "03_BarcodeQRDetection" },
        { "qr", "03_BarcodeQRDetection" },
        { "전처리", "04_PreprocessPipeline" },
        { "preprocess", "04_PreprocessPipeline" },
        { "roi 비교", "05_ROIComparison" },
        { "roi comparison", "05_ROIComparison" },
        { "pcb 검사", "06_PCBDefectInspection" },
        { "pcb", "06_PCBDefectInspection" },
        { "나사 검사", "07_ScrewPresenceCheck" },
        { "screw", "07_ScrewPresenceCheck" },
        { "치수 측정", "08_AutoDimensionMeasure" },
        { "dimension", "08_AutoDimensionMeasure" },
        { "라벨 정렬", "09_LabelAlignmentCheck" },
        { "label", "09_LabelAlignmentCheck" },
        { "표면 스크래치", "10_SurfaceScratchDetect" },
        { "scratch", "10_SurfaceScratchDetect" },
        { "원 직경", "11_CircleDiameterMeasure" },
        { "circle diameter", "11_CircleDiameterMeasure" },
        { "템플릿 매칭", "12_PatternMatchAssembly" },
        { "template match", "12_PatternMatchAssembly" },
        { "pattern match", "12_PatternMatchAssembly" },
        { "컨베이어 카운팅", "13_ConveyorCounting" },
        { "conveyor", "13_ConveyorCounting" },
        { "카운팅", "13_ConveyorCounting" },
        { "색상 일관성", "14_ColorConsistency" },
        { "color consistency", "14_ColorConsistency" },
        { "복합 결함", "15_CompoundDefectInspect" },
        { "compound defect", "15_CompoundDefectInspect" },
        { "히스토그램", "16_HistogramMonitoring" },
        { "histogram", "16_HistogramMonitoring" },
        { "적응형 임계값", "17_AdaptiveThreshold" },
        { "adaptive threshold", "17_AdaptiveThreshold" },
        { "워터셰드", "18_WatershedSeparation" },
        { "watershed", "18_WatershedSeparation" },
        { "다중 roi", "19_MultiROIInspection" },
        { "multi roi", "19_MultiROIInspection" },
        { "ok/ng", "20_AutoOKNG" },
        { "okng", "20_AutoOKNG" },
        { "ok ng", "20_AutoOKNG" },
        { "판정", "20_AutoOKNG" },
        { "얼굴 검출", "21_FaceDetection" },
        { "face detection", "21_FaceDetection" },
        { "손 인식", "22_HandGestureRecognition" },
        { "hand gesture", "22_HandGestureRecognition" },
        { "제스처", "22_HandGestureRecognition" },
        { "포즈 추정", "23_PoseEstimation" },
        { "pose", "23_PoseEstimation" },
        { "페이스 메쉬", "24_FaceMeshAnalysis" },
        { "face mesh", "24_FaceMeshAnalysis" },
        { "배경 제거", "25_BackgroundRemoval" },
        { "background removal", "25_BackgroundRemoval" },
        { "yolo", "26_YoloObjectDetection" },
        { "ocr", "27_OCRTextExtraction" },
        { "텍스트 인식", "27_OCRTextExtraction" },
        { "ai 비전", "28_AIVisionReport" },
        { "ai vision", "28_AIVisionReport" },
        { "파이썬", "29_PythonIntegration" },
        { "python", "29_PythonIntegration" },
        { "멀티 카메라", "30_MultiCameraProcessing" },
        { "multi camera", "30_MultiCameraProcessing" },
        { "c# 스크립트", "31_CSharpScriptExample" },
        { "c# script", "31_CSharpScriptExample" },
        { "씨샵 스크립트", "31_CSharpScriptExample" },
        { "씨샾 스크립트", "31_CSharpScriptExample" },
        { "csharp", "31_CSharpScriptExample" },
    };

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private string _statusText = "Initializing...";
    [ObservableProperty] private bool _isSending;
    [ObservableProperty] private string _currentModel = "";
    [ObservableProperty] private bool _hasAttachedImage;
    [ObservableProperty] private BitmapImage? _attachedImageSource;

    // Store for providing to RAG
    private readonly NodeRegistry? _registry;
    private static readonly Dictionary<string, (string Desc, string Apps)> KoreanDescriptions = NodeDescriptions.GetKoreanDescriptions();
    private static readonly Dictionary<string, string> CategoryNames = NodeDescriptions.GetCategoryNames();

    public ChatbotViewModel(NodeRegistry? registry = null)
    {
        _registry = registry;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _config = ChatConfig.Load();
        _currentModel = GetModelDisplayName();

        // Initialize RAG engine in background (tracked for SendMessage to await)
        _setupTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _config = ChatConfig.Load();
            _dispatcher.BeginInvoke(() => CurrentModel = GetModelDisplayName());

            var ragEngine = new RagEngine(_config);
            ragEngine.StatusChanged += status =>
            {
                _dispatcher.BeginInvoke(() =>
                {
                    if (status == "NEED_INSTALL")
                    {
                        PromptOllamaInstall();
                        return;
                    }
                    StatusText = status;
                });
            };

            // Run heavy initialization on background thread to avoid blocking UI
            await Task.Run(async () =>
            {
                await ragEngine.InitializeAsync(_registry, KoreanDescriptions, CategoryNames);
            });

            _ragEngine = ragEngine;

            var available = await Task.Run(() => ragEngine.IsAvailableAsync());
            if (available)
                _dispatcher.BeginInvoke(() =>
                    StatusText = $"Ready ({_ragEngine.DocumentCount} docs)");
        }
        catch (Exception ex)
        {
            _dispatcher.BeginInvoke(() =>
                StatusText = $"초기화 실패: {ex.Message}");
        }
    }

    private void PromptOllamaInstall()
    {
        var result = MessageBox.Show(
            "AI 챗봇을 사용하려면 Ollama가 필요합니다.\n" +
            "자동으로 설치할까요?\n\n" +
            "(인터넷 연결 필요, 약 150MB 다운로드 + 모델 약 3.5GB)",
            "Ollama 설치",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _ = RunOllamaSetupAsync();
        }
        else
        {
            StatusText = "Ollama 미설치 — 설정에서 API 모드로 전환하세요";
        }
    }

    private async Task RunOllamaSetupAsync()
    {
        if (_ragEngine == null) return;

        try
        {
            IsSending = true; // Block input during setup
            var success = await _ragEngine.RunOllamaSetupAsync();

            if (success)
            {
                // Now build RAG index
                await _ragEngine.InitializeAsync(_registry, KoreanDescriptions, CategoryNames);
                StatusText = $"Ready ({_ragEngine.DocumentCount} docs)";
            }
            else
            {
                StatusText = "Ollama 설치 실패 — 수동 설치: https://ollama.com";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"설치 오류: {ex.Message}";
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text) || IsSending) return;

        // 보안/무관한 질문 사전 차단
        if (IsBlockedQuery(text))
        {
            Messages.Add(new ChatMessageViewModel { Content = text, IsUser = true });
            var blockedMsg = new ChatMessageViewModel
            {
                Content = PromptConfig.Load().OffTopicResponse,
                IsUser = false
            };
            AttachHelpButton(blockedMsg);
            Messages.Add(blockedMsg);
            InputText = "";
            return;
        }

        // Add user message (with attached image if present)
        BitmapImage? userImage = null;
        if (_attachedImageData != null)
        {
            userImage = new BitmapImage();
            userImage.BeginInit();
            userImage.StreamSource = new MemoryStream(_attachedImageData);
            userImage.DecodePixelHeight = 120;
            userImage.CacheOption = BitmapCacheOption.OnLoad;
            userImage.EndInit();
            userImage.Freeze();
        }
        var userMsg = new ChatMessageViewModel
        {
            Content = text,
            IsUser = true,
            AttachedImage = userImage
        };
        Messages.Add(userMsg);
        InputText = "";

        // 이미지 데이터를 로컬 변수에 저장하고 하단 미리보기 즉시 제거
        var imageToSend = _attachedImageData;
        if (_attachedImageData != null)
            ClearAttachedImage();

        IsSending = true;
        StatusText = "Thinking...";

        // Prepare assistant message for streaming
        var assistantMsg = new ChatMessageViewModel
        {
            Content = "",
            IsUser = false
        };
        Messages.Add(assistantMsg);

        _sendCts = new CancellationTokenSource();
        var ct = _sendCts.Token;

        try
        {
            // 진행 중인 초기화/모델 다운로드 완료 대기
            if (_setupTask != null)
            {
                StatusText = "초기화 대기 중...";
                await _setupTask;
                _setupTask = null;
                StatusText = "Thinking...";
            }

            if (_ragEngine == null)
            {
                assistantMsg.Content =
                    "챗봇 초기화에 실패했습니다.\n\n" +
                    "해결 방법:\n" +
                    "1. 앱을 재시작해 보세요.\n" +
                    "2. 설정(톱니바퀴)에서 프로바이더와 연결 정보를 확인하세요.";
                StatusText = "Error";
                return;
            }

            // Ollama 프로바이더: 서버 시작 + 모델 존재 확인 (매 전송 시)
            if (_config.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            {
                StatusText = "Ollama 확인 중...";
                var (serverOk, modelsOk) = await EnsureOllamaRunningAsync(ct);
                if (!serverOk)
                {
                    assistantMsg.Content =
                        "Ollama 서버에 연결할 수 없습니다.\n\n" +
                        "해결 방법:\n" +
                        "1. 아래 링크에서 Ollama를 설치하세요.\n" +
                        "2. 설치 후 앱을 재시작하면 자동으로 연결됩니다.\n" +
                        "3. 또는 설정에서 OpenAI/Claude/Gemini API 모드로 전환하세요.";
                    assistantMsg.ActionUrl = "https://ollama.com";
                    assistantMsg.ActionUrlText = "Ollama 다운로드 (ollama.com)";
                    StatusText = "Error";
                    return;
                }
                if (!modelsOk)
                {
                    assistantMsg.Content =
                        "AI 모델 다운로드에 실패했습니다.\n\n" +
                        "해결 방법:\n" +
                        "1. Ollama를 최신 버전으로 업데이트하세요.\n" +
                        "2. 인터넷 연결을 확인하세요.\n" +
                        "3. 설정에서 다른 모델을 선택하거나 API 모드로 전환하세요.";
                    assistantMsg.ActionUrl = "https://ollama.com";
                    assistantMsg.ActionUrlText = "Ollama 최신 버전 다운로드";
                    StatusText = "모델 다운로드 실패";
                    return;
                }
                StatusText = "Thinking...";
            }

            // Build conversation history (last N messages, excluding the current ones)
            var history = BuildHistory();

            if (imageToSend != null)
            {
                // Image + text question (with conversation history)
                var response = await _ragEngine.AskWithImageAsync(text, imageToSend, history, ct);
                if (response.StartsWith("@@DIRECT@@"))
                {
                    assistantMsg.Content = response["@@DIRECT@@".Length..];
                }
                else
                {
                    var cleaned = StripMarkdown(response);
                    cleaned = TrimRepeatedSuffix(cleaned);
                    assistantMsg.Content = cleaned;
                }
            }
            else
            {
                // Streaming text response with repetition detection
                var sb = new StringBuilder();
                await foreach (var token in _ragEngine.AskStreamAsync(text, history, ct))
                {
                    sb.Append(token);

                    // DirectLookup 결과는 후처리 건너뜀 (이미 정리된 텍스트)
                    if (sb.ToString().StartsWith("@@DIRECT@@"))
                    {
                        var directContent = sb.ToString()["@@DIRECT@@".Length..];
                        await _dispatcher.InvokeAsync(() => assistantMsg.Content = directContent);
                        break;
                    }

                    // 반복 루프 감지: 최근 텍스트에서 동일 문장이 3회 이상 반복되면 중단
                    if (sb.Length > 200 && IsRepeating(sb.ToString()))
                    {
                        _sendCts?.Cancel();
                        break;
                    }

                    var current = StripMarkdown(sb.ToString());
                    await _dispatcher.InvokeAsync(() => assistantMsg.Content = current);
                }

                // DirectLookup이 아닌 경우만 후처리
                if (!sb.ToString().StartsWith("@@DIRECT@@"))
                {
                    var final = StripMarkdown(sb.ToString());
                    final = TrimRepeatedSuffix(final);
                    await _dispatcher.InvokeAsync(() => assistantMsg.Content = final);
                }
            }

            {
                // LLM 폴백 답변, off-topic 차단, 실패 시 도움말 버튼 추가
                if (assistantMsg.Content.Contains("관련 자료를 찾을 수 없습니다") ||
                    assistantMsg.Content.Contains("도움말 자료에서 관련 내용을 찾지 못해") ||
                    assistantMsg.Content.Contains("관련 질문만 답변할 수 있습니다"))
                {
                    AttachHelpButton(assistantMsg);
                }

                // 답변 완료 후 예제 매칭 (질문 텍스트 키워드 기반)
                // 오류/트러블슈팅 질문이면 예제 추천 안 함
                if (!IsErrorQuestion(text))
                {
                    var matchedExample = await _ragEngine.FindExampleAsync(text, ExampleMapping, ct);
                    if (matchedExample != null)
                    {
                        assistantMsg.LoadExampleCommand = new RelayCommand(() =>
                            LoadExampleRequested?.Invoke(matchedExample));
                        assistantMsg.ExampleFileName = matchedExample;
                    }
                }

                StatusText = "Ready";
            }
        }
        catch (HttpRequestException ex)
        {
            var detail = ex.Message;
            if (detail.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                // 모델이 Ollama에 없는 경우
                assistantMsg.Content =
                    "선택한 AI 모델이 설치되어 있지 않습니다.\n\n" +
                    "해결 방법:\n" +
                    "1. 앱을 재시작하면 필요한 모델이 자동 다운로드됩니다.\n" +
                    "2. 또는 설정에서 다른 모델을 선택하세요.";
            }
            else if (detail.Contains("API error") || detail.Contains("Unauthorized") ||
                     detail.Contains("401") || detail.Contains("403"))
            {
                // API 키 오류
                assistantMsg.Content =
                    "API 인증에 실패했습니다.\n\n" +
                    "해결 방법:\n" +
                    "1. 설정(톱니바퀴)에서 API 키가 올바른지 확인하세요.\n" +
                    "2. API 키가 만료되지 않았는지 확인하세요.\n" +
                    "3. 해당 모델에 대한 접근 권한이 있는지 확인하세요.";
            }
            else if (detail.Contains("429") || detail.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            {
                // 요청 제한
                assistantMsg.Content =
                    "API 요청 한도를 초과했습니다.\n\n" +
                    "잠시 후 다시 시도해 주세요.";
            }
            else
            {
                // 서버 연결 실패
                var isOllama = _config.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase);
                assistantMsg.Content = isOllama
                    ? "Ollama 서버와 연결이 끊어졌습니다.\n\n" +
                      "해결 방법:\n" +
                      "1. 앱을 재시작해 보세요.\n" +
                      "2. 작업 관리자에서 Ollama가 실행 중인지 확인하세요.\n" +
                      "3. 또는 설정에서 API 모드로 전환하세요."
                    : "API 서버에 연결할 수 없습니다.\n\n" +
                      "해결 방법:\n" +
                      "1. 인터넷 연결을 확인하세요.\n" +
                      "2. 설정에서 API 키와 모델을 확인하세요.";
            }
            StatusText = "Ready";
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrEmpty(assistantMsg.Content))
                assistantMsg.Content = "응답이 취소되었습니다.";
            StatusText = "Ready";
        }
        catch (Exception)
        {
            assistantMsg.Content = "응답 생성 중 오류가 발생했습니다.\n\n앱을 재시작하거나 설정을 확인해 주세요.";
            StatusText = "Ready";
        }
        finally
        {
            IsSending = false;
            _sendCts?.Dispose();
            _sendCts = null;
        }
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _sendCts?.Cancel();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        ClearAttachedImage();
        StatusText = "Ready";
    }

    [RelayCommand]
    private void AttachImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.tif|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _attachedImageData = File.ReadAllBytes(dialog.FileName);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(_attachedImageData);
                bitmap.DecodePixelHeight = 60;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                AttachedImageSource = bitmap;
                HasAttachedImage = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지 로드 실패: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void RemoveImage()
    {
        ClearAttachedImage();
    }

    /// <summary>
    /// Called from ChatbotView code-behind when user pastes an image via Ctrl+V.
    /// </summary>
    public void PasteImageFromClipboard(System.Windows.Media.Imaging.BitmapSource source)
    {
        try
        {
            // Encode BitmapSource to PNG byte[]
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            _attachedImageData = ms.ToArray();

            // Create preview thumbnail
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(_attachedImageData);
            bitmap.DecodePixelHeight = 60;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            AttachedImageSource = bitmap;
            HasAttachedImage = true;
        }
        catch
        {
            // Silently ignore paste failures
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var dialog = new Views.ChatSettingsDialog(_config)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            var oldProvider = _config.Provider;
            _config = dialog.ResultConfig;
            _config.Save();
            CurrentModel = GetModelDisplayName();

            // 재임베딩 요청 시 인덱스 재구축
            if (dialog.ShouldRebuildIndex)
            {
                StatusText = "RAG 인덱스 재구축 중...";
                _setupTask = RebuildIndexFromScratchAsync();
                return;
            }

            if (_ragEngine != null)
            {
                _ragEngine.UpdateConfig(_config);
                StatusText = "설정 업데이트됨";

                // Ollama 모델 변경 시 자동 다운로드 + 서비스 확인 (추적해서 SendMessage에서 대기)
                if (_config.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                    _setupTask = EnsureOllamaModelsAfterSettingsAsync();
                else
                    _setupTask = VerifyAfterSettingsChangeAsync(oldProvider);
            }
            else
            {
                // RAG engine was null (initial init failed) — re-initialize with new config
                StatusText = "재초기화 중...";
                _setupTask = InitializeAsync();
            }
        }
    }

    private async Task EnsureOllamaModelsAfterSettingsAsync()
    {
        if (_ragEngine == null) return;

        try
        {
            // ModelManager가 없으면 새로 생성
            var manager = _ragEngine.ModelManager
                ?? new OllamaModelManager(_config.OllamaBaseUrl);

            if (!_managerStatusSubscribed)
            {
                manager.StatusChanged += s =>
                    _dispatcher.BeginInvoke(() => StatusText = s);
                _managerStatusSubscribed = true;
            }

            // Ollama 서버 확인 + 시작
            if (!await manager.IsRunningAsync())
            {
                if (OllamaModelManager.IsInstalled())
                    await manager.StartAsync();
                else
                {
                    _dispatcher.BeginInvoke(() =>
                        StatusText = "Ollama가 설치되어 있지 않습니다.");
                    return;
                }
            }

            var models = new[] { _config.OllamaChatModel, _config.OllamaVisionModel, _config.OllamaEmbedModel }
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct().ToArray();

            _dispatcher.BeginInvoke(() => StatusText = "모델 확인 중...");
            await Task.Run(() => manager.EnsureModelsAsync(models));
            _dispatcher.BeginInvoke(() =>
                StatusText = $"Ready ({_ragEngine.DocumentCount} docs)");
        }
        catch (Exception ex)
        {
            _dispatcher.BeginInvoke(() =>
                StatusText = $"모델 다운로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// SendMessage에서 호출: Ollama 서버 자동 시작 + 필요한 모델 전부 확인/다운로드.
    /// 반환값: (서버 연결 성공 여부, 모델 준비 완료 여부)
    /// </summary>
    private async Task<(bool ServerOk, bool ModelsOk)> EnsureOllamaRunningAsync(CancellationToken ct)
    {
        try
        {
            var manager = _ragEngine?.ModelManager
                ?? new OllamaModelManager(_config.OllamaBaseUrl);

            if (!_managerStatusSubscribed)
            {
                manager.StatusChanged += s =>
                    _dispatcher.BeginInvoke(() => StatusText = s);
                _managerStatusSubscribed = true;
            }

            // 1. 서버 실행 확인 → 안 떠있으면 시작
            if (!await manager.IsRunningAsync(ct))
            {
                if (!OllamaModelManager.IsInstalled())
                    return (false, false);

                var started = await manager.StartAsync(ct);
                if (!started) return (false, false);
            }

            // 2. 필요한 모델 전부 확인 (챗 + 비전 + 임베딩)
            var models = new[] { _config.OllamaChatModel, _config.OllamaVisionModel, _config.OllamaEmbedModel }
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct().ToArray();
            var modelsReady = await manager.EnsureModelsAsync(models, ct);
            return (true, modelsReady);
        }
        catch
        {
            return (false, false);
        }
    }

    private async Task VerifyAfterSettingsChangeAsync(string oldProvider)
    {
        if (_ragEngine == null) return;

        try
        {
            var available = await Task.Run(() => _ragEngine.IsAvailableAsync());
            _dispatcher.BeginInvoke(() =>
            {
                if (available)
                    StatusText = $"Ready ({_ragEngine.DocumentCount} docs)";
                else if (_config.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                    StatusText = "Ollama 서버에 연결할 수 없습니다. Ollama가 실행 중인지 확인하세요.";
                else
                    StatusText = "API 키를 확인하세요.";
            });
        }
        catch
        {
            _dispatcher.BeginInvoke(() =>
                StatusText = "서비스 연결 확인 실패");
        }
    }

    /// <summary>
    /// 설정 다이얼로그에서 "임베딩 재구축" 요청 시: 기존 캐시 삭제 → _isInitialized 리셋 → 재초기화.
    /// </summary>
    private async Task RebuildIndexFromScratchAsync()
    {
        try
        {
            // RagEngine 재생성 (InitializeAsync는 _isInitialized guard가 있으므로 새로 만듦)
            _ragEngine = null;
            await InitializeAsync();
            _dispatcher.BeginInvoke(() =>
                StatusText = $"RAG 재구축 완료 ({_ragEngine?.DocumentCount ?? 0} docs)");
        }
        catch (Exception ex)
        {
            _dispatcher.BeginInvoke(() =>
                StatusText = $"RAG 재구축 실패: {ex.Message}");
        }
    }

    private async Task RebuildIndexAsync()
    {
        if (_ragEngine == null) return;
        try
        {
            await _ragEngine.RebuildIndexAsync(_registry, KoreanDescriptions, CategoryNames);

            // 재구축 후 Ollama 연결 재확인 (임베딩 과부하로 끊길 수 있음)
            var manager = _ragEngine.ModelManager;
            if (manager != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (await manager.IsRunningAsync()) break;
                    await Task.Delay(1000);
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"인덱스 재구축 실패: {ex.Message}";
        }
    }

    private void AttachHelpButton(ChatMessageViewModel msg)
    {
        msg.OpenHelpCommand = new RelayCommand(() => OpenHelpRequested?.Invoke());
    }

    private void ClearAttachedImage()
    {
        _attachedImageData = null;
        AttachedImageSource = null;
        HasAttachedImage = false;
    }

    private List<ChatMessage> BuildHistory()
    {
        var history = new List<ChatMessage>();
        var maxHistory = _config?.MaxHistoryMessages ?? 10;

        // 현재 전송 중인 user+assistant 메시지 제외
        var skipLast = Math.Max(0, Messages.Count - 2);
        var allMsgs = Messages.Take(skipLast).ToList();

        if (allMsgs.Count <= maxHistory)
        {
            // 10턴 이하: 전체 히스토리 그대로
            foreach (var m in allMsgs)
            {
                history.Add(m.IsUser ? ChatMessage.User(m.Content) : ChatMessage.Assistant(m.Content));
            }
        }
        else
        {
            // 10턴 초과: 오래된 메시지를 요약 → 최근 메시지는 그대로
            var olderMsgs = allMsgs.Take(allMsgs.Count - maxHistory).ToList();
            var recentMsgs = allMsgs.TakeLast(maxHistory).ToList();

            // 오래된 대화를 압축 요약 (Q→A 쌍을 한 줄로)
            var summary = CompactOlderMessages(olderMsgs);
            history.Add(ChatMessage.User("[이전 대화 요약]\n" + summary));
            history.Add(ChatMessage.Assistant("네, 이전 대화 내용을 참고하겠습니다."));

            foreach (var m in recentMsgs)
            {
                history.Add(m.IsUser ? ChatMessage.User(m.Content) : ChatMessage.Assistant(m.Content));
            }
        }

        return history;
    }

    /// <summary>
    /// 오래된 메시지를 Q→A 한 줄 요약으로 압축. 각 메시지의 첫 80자만 유지.
    /// </summary>
    private static string CompactOlderMessages(List<ChatMessageViewModel> messages)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < messages.Count; i++)
        {
            if (!messages[i].IsUser) continue;
            var q = Truncate(messages[i].Content, 80);
            var a = (i + 1 < messages.Count && !messages[i + 1].IsUser)
                ? Truncate(messages[i + 1].Content, 80) : "";
            sb.AppendLine($"- Q: {q} -> A: {a}");
            if (i + 1 < messages.Count && !messages[i + 1].IsUser) i++;
        }
        return sb.ToString();
    }

    private static string Truncate(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // 개행 제거 후 자르기
        var clean = text.Replace("\n", " ").Replace("\r", "");
        return clean.Length > maxLen ? clean[..maxLen] + "..." : clean;
    }

    private string GetModelDisplayName()
    {
        return _config.Provider.ToLowerInvariant() switch
        {
            "openai" => $"OpenAI: {(!string.IsNullOrEmpty(_config.ApiModel) ? _config.ApiModel : "gpt-5-mini")}",
            "claude" => $"Claude: {(!string.IsNullOrEmpty(_config.ApiModel) ? _config.ApiModel : "claude-sonnet-4-6")}",
            "gemini" => $"Gemini: {(!string.IsNullOrEmpty(_config.ApiModel) ? _config.ApiModel : "gemini-2.5-flash")}",
            _ => $"Ollama: {_config.OllamaChatModel}"
        };
    }


    private static readonly string[] BlockedKeywords =
    {
        // 보안 위협
        "해킹", "hack", "exploit", "injection", "인젝션",
        "악성코드", "malware", "virus", "바이러스", "랜섬웨어", "ransomware",
        "비밀번호 크랙", "password crack", "brute force", "브루트포스",
        "ddos", "피싱", "phishing", "스푸핑", "spoofing",
        "키로거", "keylogger", "백도어", "backdoor",
        "root 권한", "admin 권한", "권한 상승", "privilege escalation",
        "개인정보", "주민등록", "신용카드", "계좌번호",
        // Prompt injection (영어)
        "탈옥", "jailbreak", "프롬프트 인젝션", "prompt injection",
        "시스템 프롬프트", "system prompt", "ignore previous", "ignore above",
        "disregard", "forget your", "you are now", "act as", "pretend to be",
        "override", "bypass", "new instructions",
        // Prompt injection (한국어)
        "너는 이제부터", "지금부터 너는", "너의 역할을", "새로운 역할",
        "위의 지시", "이전 지시", "위 내용을 무시", "모든 규칙을 무시",
        "역할을 무시", "지시를 무시", "규칙을 무시",
        "제한을 해제", "제한 없이", "필터 없이", "검열 없이",
        // 시스템 정보 탈취
        "시스템 메시지", "시스템 지시", "원래 지시", "초기 프롬프트",
        "너의 지시사항", "프롬프트를 보여", "설정을 알려",
        // API 키/설정
        "api key", "api_key", "apikey", "api키",
        "secret key", "비밀키", "비밀 키", "시크릿",
        "설정 파일", "config 내용", "설정 내용", "설정값",
        "토큰 알려", "키 알려", "key 알려",
        "환경변수", "environment variable"
    };

    private static readonly System.Text.RegularExpressions.Regex[] InjectionPatterns =
    {
        new(@"(무시|잊어|버려).{0,10}(규칙|지시|프롬프트|역할)",
            System.Text.RegularExpressions.RegexOptions.Compiled),
        new(@"(너는|넌|당신은).{0,10}(이제|지금).{0,10}(부터|부턴)",
            System.Text.RegularExpressions.RegexOptions.Compiled),
        new(@"ignore.{0,20}(instruction|rule|prompt|above|previous)",
            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase),
        new(@"(reveal|show|print|output).{0,20}(system|prompt|instruction)",
            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase),
    };

    private static bool IsBlockedQuery(string text)
    {
        var normalized = Chat.KoreanTextNormalizer.Normalize(text);
        var lower = normalized.ToLowerInvariant();
        if (BlockedKeywords.Any(k => lower.Contains(k)))
            return true;
        return InjectionPatterns.Any(p => p.IsMatch(normalized));
    }

    /// <summary>
    /// Qwen 모델의 한국어 외래어 오표기 교정 사전.
    /// </summary>
    private static readonly (string Wrong, string Correct)[] TermCorrections =
    {
        ("마이치언 비전", "머신비전"),
        ("마이신 비전", "머신비전"),
        ("마신 비전", "머신비전"),
        ("Machine Vison", "Machine Vision"),
        ("머쉰비전", "머신비전"),
        ("머쉰 비전", "머신비전"),
        ("가우시언", "가우시안"),
        ("가우션", "가우시안"),
        ("쓰레숄드", "임계값"),
        ("쓰레쉬홀드", "임계값"),
        ("트레숄드", "임계값"),
        ("콘투어", "컨투어"),
        ("몰폴로지", "모폴로지"),
        ("모폴러지", "모폴로지"),
        ("세그먼테이션", "세그멘테이션"),
        ("히스토그래므", "히스토그램"),
        ("텐플릿", "템플릿"),
        ("바이너리제이션", "이진화"),
        ("칼리브레이션", "캘리브레이션"),
        ("파이프 라인", "파이프라인"),
        ("크로프", "크롭"),
        ("크롭핑", "크롭"),
        ("Cropping node", "Crop(크롭) 노드"),
        ("Cropping Node", "Crop(크롭) 노드"),
    };

    private static readonly string[] ErrorKeywords =
    {
        "오류", "에러", "error", "안됨", "안돼", "안나", "실패", "failed",
        "왜 안", "안 되", "못 ", "문제", "버그", "bug", "crash",
        "exception", "트러블", "trouble", "fix", "고장"
    };

    private static bool IsErrorQuestion(string text)
    {
        var lower = text.ToLowerInvariant();
        return ErrorKeywords.Any(k => lower.Contains(k));
    }

    /// <summary>
    /// 텍스트 끝부분에서 동일 문장이 3회 이상 반복되는지 감지.
    /// 소형 LLM의 반복 루프 방지용.
    /// </summary>
    private static bool IsRepeating(string text)
    {
        // 마지막 500자만 검사
        var tail = text.Length > 500 ? text[^500..] : text;
        var lines = tail.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3) return false;

        // 마지막 줄과 동일한 줄이 3개 이상이면 반복
        var lastLine = lines[^1].Trim();
        if (lastLine.Length < 10) return false;

        int count = 0;
        for (int i = lines.Length - 1; i >= 0 && i >= lines.Length - 6; i--)
        {
            if (lines[i].Trim() == lastLine)
                count++;
        }
        return count >= 3;
    }

    /// <summary>
    /// 문단 단위 반복 제거. 같은 문단이 2회 이상 나오면 첫 번째만 유지.
    /// </summary>
    private static string TrimRepeatedSuffix(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 빈 줄 기준으로 문단 분리
        var paragraphs = System.Text.RegularExpressions.Regex.Split(text, @"\n\s*\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        if (paragraphs.Count < 2) return text;

        // 이미 등장한 문단을 추적, 중복 제거
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<string>();

        foreach (var para in paragraphs)
        {
            // 짧은 문단(10자 미만)은 중복 체크 안 함 (구분선, 라벨 등)
            if (para.Length < 10 || seen.Add(para))
                unique.Add(para);
        }

        // 마지막 문단이 불완전하게 잘린 경우 제거 (문장 끝 기호로 안 끝남)
        if (unique.Count > 1)
        {
            var last = unique[^1].TrimEnd();
            if (last.Length > 0 && !last.EndsWith('.') && !last.EndsWith('다') &&
                !last.EndsWith('요') && !last.EndsWith("니다") && !last.EndsWith(')') &&
                !last.EndsWith('!') && !last.EndsWith('?'))
            {
                unique.RemoveAt(unique.Count - 1);
            }
        }

        return string.Join("\n\n", unique).TrimEnd();
    }

    private static string StripMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        try
        {
            return StripMarkdownInternal(text);
        }
        catch
        {
            // 포맷 처리 실패 시 원본 텍스트 그대로 반환 (에러 노출 방지)
            return text;
        }
    }

    private static string StripMarkdownInternal(string text)
    {
        // 리터럴 \n 문자열을 실제 줄바꿈으로 치환 (모델이 문자열로 출력하는 경우)
        text = text.Replace("\\n", "\n");

        // 프롬프트 구분자가 답변에 노출된 경우 제거
        text = text.Replace("[참고 자료 시작]", "").Replace("[참고 자료 끝]", "");
        text = text.Replace("[참고자료 시작]", "").Replace("[참고자료 끝]", "");

        // 중국어 문자 제거 (Qwen 모델이 간헐적으로 중국어를 섞어 출력)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[\u4E00-\u9FFF]+", "");

        // Qwen 모델 한국어 외래어 오표기 교정
        foreach (var (wrong, correct) in TermCorrections)
            text = text.Replace(wrong, correct);

        // VLM 이미지 응답에서 "스크린샷 -" 접두사 제거
        text = System.Text.RegularExpressions.Regex.Replace(text, @"스크린샷\s*[-—–]\s*", "");

        // 모델이 생성한 가짜 URL 제거 (hallucination)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\(https?://[^\)]+\)", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"https?://\S+", "");

        // 숫자 이모지 (1⃣, 2⃣ 등) 제거
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\d[\uFE0F]?\u20E3", "");

        // [대괄호] 링크 표기 정리 → 내용만 유지
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]", "$1");

        // Remove emoji characters (Unicode emoji ranges)
        // .NET regex uses \uD83C\uDF00 surrogate pairs, not \U0001F300
        text = System.Text.RegularExpressions.Regex.Replace(text,
            @"[\u2600-\u27BF\uFE0F]|[\uD83C-\uDBFF][\uDC00-\uDFFF]", "");

        var lines = text.Split('\n');
        var sb = new StringBuilder();
        bool inCodeBlock = false;

        foreach (var line in lines)
        {
            var l = line;

            // 코드 블록 (```) 제거
            if (l.TrimStart().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            // 마크다운 테이블 구분선 (|---|---| 등) 제거
            if (!inCodeBlock && System.Text.RegularExpressions.Regex.IsMatch(l.Trim(), @"^\|[\s\-:|\+]+\|$"))
                continue;

            // 마크다운 테이블 행 → | 제거하고 공백 구분
            if (!inCodeBlock && l.Trim().StartsWith('|') && l.Trim().EndsWith('|'))
            {
                l = l.Trim().Trim('|');
                var cells = l.Split('|');
                l = string.Join("  ", cells.Select(c => c.Trim()).Where(c => c.Length > 0));
            }

            // 헤딩 (#, ##, ###) → 일반 텍스트
            if (!inCodeBlock && l.TrimStart().StartsWith('#'))
                l = l.TrimStart().TrimStart('#').TrimStart();

            // 볼드/이탤릭 (**text**, *text*, __text__, _text_)
            l = System.Text.RegularExpressions.Regex.Replace(l, @"\*\*(.+?)\*\*", "$1");
            l = System.Text.RegularExpressions.Regex.Replace(l, @"\*(.+?)\*", "$1");
            l = System.Text.RegularExpressions.Regex.Replace(l, @"__(.+?)__", "$1");
            l = System.Text.RegularExpressions.Regex.Replace(l, @"(?<!\w)_(.+?)_(?!\w)", "$1");

            // 인라인 코드 (`code`)
            l = System.Text.RegularExpressions.Regex.Replace(l, @"`(.+?)`", "$1");

            // 마크다운 리스트 (- item) → · item
            if (!inCodeBlock && System.Text.RegularExpressions.Regex.IsMatch(l, @"^\s*[-\*]\s"))
                l = System.Text.RegularExpressions.Regex.Replace(l, @"^(\s*)[-\*]\s", "$1· ");

            // 수평선 (---, ***, ___) 제거
            if (!inCodeBlock && System.Text.RegularExpressions.Regex.IsMatch(l.Trim(), @"^[-\*_]{3,}$"))
                continue;

            sb.AppendLine(l);
        }

        // Collapse excessive blank lines (3+ → 2)
        var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\n{3,}", "\n\n");
        return result.TrimEnd();
    }

}
