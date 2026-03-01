using System.Text.Json;

namespace MVXTester.Nodes.AI;

/// <summary>
/// Loads API configuration (keys, models) from Models/API/api_config.json.
/// Config is cached and shared across all AI nodes.
/// </summary>
public static class ApiConfigHelper
{
    private static Dictionary<string, ApiProviderConfig>? _cache;
    private static readonly object _lock = new();

    public class ApiProviderConfig
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "";
    }

    /// <summary>
    /// Load config for a specific provider (openai, gemini, claude).
    /// Returns null if config file not found or provider not configured.
    /// </summary>
    public static ApiProviderConfig? GetConfig(string provider)
    {
        lock (_lock)
        {
            if (_cache == null)
                LoadConfig();

            return _cache!.TryGetValue(provider.ToLowerInvariant(), out var config) ? config : null;
        }
    }

    /// <summary>
    /// Reload config from disk (call when user updates the JSON file).
    /// </summary>
    public static void Reload()
    {
        lock (_lock)
        {
            _cache = null;
            LoadConfig();
        }
    }

    private static void LoadConfig()
    {
        _cache = new Dictionary<string, ApiProviderConfig>(StringComparer.OrdinalIgnoreCase);

        var configPath = FindConfigFile();
        if (configPath == null || !File.Exists(configPath))
            return;

        try
        {
            var json = File.ReadAllText(configPath);
            var doc = JsonDocument.Parse(json);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var providerName = prop.Name.ToLowerInvariant();
                var apiKey = "";
                var model = "";

                if (prop.Value.TryGetProperty("api_key", out var keyElem))
                    apiKey = keyElem.GetString() ?? "";
                if (prop.Value.TryGetProperty("model", out var modelElem))
                    model = modelElem.GetString() ?? "";

                _cache[providerName] = new ApiProviderConfig
                {
                    ApiKey = apiKey,
                    Model = model
                };
            }
        }
        catch
        {
            // Config parse error - ignore, nodes will show "API Key required"
        }
    }

    private static string? FindConfigFile()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Check Models/API/api_config.json
        var path = Path.Combine(baseDir, "Models", "API", "api_config.json");
        if (File.Exists(path)) return path;

        // Fallback: check parent directories (for dev runs)
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            dir = dir.Parent;
            if (dir == null) break;
            path = Path.Combine(dir.FullName, "Models", "API", "api_config.json");
            if (File.Exists(path)) return path;
        }

        return null;
    }
}
