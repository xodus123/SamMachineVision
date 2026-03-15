using System.IO;
using System.Windows;
using System.Windows.Controls;
using MVXTester.Chat;

namespace MVXTester.Chat.Views;

public partial class ChatSettingsDialog : Window
{
    public ChatConfig ResultConfig { get; private set; }
    public bool ShouldRebuildIndex { get; private set; }

    // Remember selected model ID per provider to restore when switching back
    private readonly Dictionary<string, string> _selectedModelPerProvider = new();

    // Model definitions per provider: (display name, model id, description)
    // Ollama 로컬 모델은 chat_config.json에서 고정 (설정 UI에서 변경 불가)

    private static readonly (string Display, string Id, string Desc)[] OpenAIModels =
    {
        ("GPT-5-mini (빠름, 저렴)", "gpt-5-mini", "빠르고 저렴한 모델. 일반적인 질문에 적합."),
        ("GPT-5.4 (최신)", "gpt-5.4", "최신 고성능 모델. 코딩과 추론에 우수."),
        ("GPT-5.4 Pro (최고)", "gpt-5.4-pro", "최고 성능. 복잡한 작업에 적합."),
    };

    private static readonly (string Display, string Id, string Desc)[] ClaudeModels =
    {
        ("Claude Haiku 4.5 (빠름)", "claude-haiku-4-5", "가장 빠른 모델. 비용 효율적."),
        ("Claude Sonnet 4.6 (균형)", "claude-sonnet-4-6", "속도와 품질의 균형."),
        ("Claude Opus 4.6 (최고)", "claude-opus-4-6", "최고 품질, 깊은 추론."),
    };

    private static readonly (string Display, string Id, string Desc)[] GeminiModels =
    {
        ("Gemini 2.5 Flash (빠름)", "gemini-2.5-flash", "빠르고 효율적인 모델."),
        ("Gemini 3.1 Flash-Lite (경량)", "gemini-3.1-flash-lite-preview", "비용 효율적인 대량 처리용."),
        ("Gemini 3.1 Pro (최고)", "gemini-3.1-pro-preview", "최신 추론 모델."),
    };

    public ChatSettingsDialog(ChatConfig config)
    {
        InitializeComponent();
        ResultConfig = config;
        LoadFromConfig(config);
    }

    private void LoadFromConfig(ChatConfig config)
    {
        // Initialize per-provider model memory from config
        if (!string.IsNullOrEmpty(config.OllamaChatModel))
            _selectedModelPerProvider["ollama"] = config.OllamaChatModel;
        if (!string.IsNullOrEmpty(config.ApiModel))
        {
            var apiProv = config.ApiProvider.ToLowerInvariant();
            if (!string.IsNullOrEmpty(apiProv) && apiProv != "ollama")
                _selectedModelPerProvider[apiProv] = config.ApiModel;
        }

        // Set provider combo
        var providerIndex = config.Provider.ToLowerInvariant() switch
        {
            "openai" => 1,
            "claude" => 2,
            "gemini" => 3,
            _ => 0
        };
        ProviderCombo.SelectedIndex = providerIndex;

        ApiKeyBox.Password = config.ApiKey;

        UpdateProviderPanels();

        // Select current model in combo
        SelectCurrentModel(config);
    }

    private void SelectCurrentModel(ChatConfig config)
    {
        var models = GetModelsForProvider(GetSelectedProvider());
        var currentModelId = config.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            ? config.OllamaChatModel
            : config.ApiModel;

        int selectedIndex = 0;
        for (int i = 0; i < models.Length; i++)
        {
            if (models[i].Id.Equals(currentModelId, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }
        ModelCombo.SelectedIndex = selectedIndex;
    }

    private static (string Display, string Id, string Desc)[] GetModelsForProvider(string provider)
    {
        return provider switch
        {
            "openai" => OpenAIModels,
            "claude" => ClaudeModels,
            "gemini" => GeminiModels,
            _ => Array.Empty<(string, string, string)>()
        };
    }

    private void PopulateModelCombo(string provider)
    {
        ModelCombo.Items.Clear();
        var models = GetModelsForProvider(provider);
        foreach (var m in models)
            ModelCombo.Items.Add(new ComboBoxItem { Content = m.Display, Tag = m.Id });

        // Restore previously selected model for this provider
        int selectedIndex = 0;
        if (_selectedModelPerProvider.TryGetValue(provider, out var savedModelId))
        {
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i].Id.Equals(savedModelId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        if (ModelCombo.Items.Count > 0)
            ModelCombo.SelectedIndex = selectedIndex;
    }

    private void SaveCurrentModelSelection()
    {
        var provider = GetSelectedProvider();
        var modelId = GetSelectedModelId();
        if (!string.IsNullOrEmpty(modelId))
            _selectedModelPerProvider[provider] = modelId;
    }

    private ChatConfig BuildConfig()
    {
        var provider = GetSelectedProvider();
        var selectedModelId = GetSelectedModelId();

        return new ChatConfig
        {
            Provider = provider,
            OllamaBaseUrl = ResultConfig.OllamaBaseUrl,
            OllamaChatModel = provider == "ollama" ? selectedModelId : ResultConfig.OllamaChatModel,
            OllamaVisionModel = ResultConfig.OllamaVisionModel,
            OllamaEmbedModel = ResultConfig.OllamaEmbedModel,
            EmbeddingBackend = "ollama",
            ApiProvider = provider == "ollama" ? ResultConfig.ApiProvider : provider,
            ApiKey = ApiKeyBox.Password,
            ApiModel = provider != "ollama" ? selectedModelId : ResultConfig.ApiModel,
            Temperature = ResultConfig.Temperature,
            MaxTokens = ResultConfig.MaxTokens,
            MaxContextChunks = ResultConfig.MaxContextChunks,
            RepeatPenalty = ResultConfig.RepeatPenalty,
            MaxHistoryMessages = ResultConfig.MaxHistoryMessages
        };
    }

    private string GetSelectedProvider()
    {
        if (ProviderCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            return tag;
        return "ollama";
    }

    private string GetSelectedModelId()
    {
        if (ModelCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            return tag;
        return "";
    }

    private void UpdateProviderPanels()
    {
        var provider = GetSelectedProvider();
        var isOllama = provider == "ollama";
        ApiPanel.Visibility = isOllama ? Visibility.Collapsed : Visibility.Visible;

        // Ollama: 고정 모델 표시, Cloud: 콤보박스 표시
        OllamaModelPanel.Visibility = isOllama ? Visibility.Visible : Visibility.Collapsed;
        CloudModelPanel.Visibility = isOllama ? Visibility.Collapsed : Visibility.Visible;

        if (isOllama)
        {
            OllamaModelInfoText.Text =
                $"Chat: {ResultConfig.OllamaChatModel}\n" +
                $"Vision: {ResultConfig.OllamaVisionModel}\n" +
                $"Embed: {ResultConfig.OllamaEmbedModel}";
        }
        else
        {
            PopulateModelCombo(provider);
        }
    }

    private void OnProviderChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;

        // Save current model selection before switching provider
        SaveCurrentModelSelection();

        // 프로바이더 전환 시 API 키 초기화
        ApiKeyBox.Password = "";

        UpdateProviderPanels();
    }

    private void OnModelChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || ModelCombo.SelectedItem == null) return;

        var provider = GetSelectedProvider();
        var models = GetModelsForProvider(provider);
        var idx = ModelCombo.SelectedIndex;
        if (idx >= 0 && idx < models.Length)
            ModelDescText.Text = models[idx].Desc;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        ResultConfig = BuildConfig();
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    // 임베딩 재구축은 개발자가 build_rag_index.py로 수행 (UI에서 제거됨)
}
