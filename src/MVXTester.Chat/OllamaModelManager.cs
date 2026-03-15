using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MVXTester.Chat;

/// <summary>
/// Manages Ollama lifecycle: installation, process start/stop, model pull.
/// Flow: CheckInstalled → Install (if needed) → StartProcess → EnsureModels → Ready
/// </summary>
public sealed class OllamaModelManager : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private Process? _ollamaProcess;
    private bool _processOwned; // true if we started the process
    private bool _needsUpdate;  // true if 412 detected (version too old)

    public bool NeedsUpdate => _needsUpdate;

    private const string OllamaExeName = "ollama.exe";
    private const string OllamaAppName = "ollama app.exe";
    private const string SetupUrl = "https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe";

    public event Action<string>? StatusChanged;
    public event Action<double>? DownloadProgress; // percent 0-100

    public OllamaModelManager(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
    }

    // ══════════════════════════════════════════════
    //  1. Detection: Is Ollama installed / running?
    // ══════════════════════════════════════════════

    /// <summary>Check if Ollama server is reachable.</summary>
    public async Task<bool> IsRunningAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var response = await _http.GetAsync($"{_baseUrl}/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Find the ollama.exe path if installed.</summary>
    public static string? FindOllamaPath()
    {
        // 1. Default install location
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var defaultPath = Path.Combine(localAppData, "Programs", "Ollama", OllamaExeName);
        if (File.Exists(defaultPath)) return defaultPath;

        // 2. Check Program Files
        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var progPath = Path.Combine(progFiles, "Ollama", OllamaExeName);
        if (File.Exists(progPath)) return progPath;

        // 3. Check PATH environment
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), OllamaExeName);
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }

        // 4. Check if ollama process is already running → get its path
        try
        {
            var procs = Process.GetProcessesByName("ollama");
            foreach (var p in procs)
            {
                try
                {
                    var path = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        return path;
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
        catch { }

        return null;
    }

    /// <summary>Check if Ollama is installed on this machine.</summary>
    public static bool IsInstalled() => FindOllamaPath() != null;

    // ══════════════════════════════════════════════
    //  2. Installation: Download and install Ollama
    // ══════════════════════════════════════════════

    /// <summary>
    /// Download OllamaSetup.exe and run silent install.
    /// Returns true on success.
    /// </summary>
    public async Task<bool> InstallAsync(CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MVXTester_Ollama");
        Directory.CreateDirectory(tempDir);
        var setupPath = Path.Combine(tempDir, "OllamaSetup.exe");

        try
        {
            // Download installer
            StatusChanged?.Invoke("Ollama 설치 파일 다운로드 중...");
            using var response = await _http.GetAsync(SetupUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(setupPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long downloaded = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                downloaded += bytesRead;
                if (totalBytes > 0)
                {
                    var percent = (double)downloaded / totalBytes * 100;
                    var downloadedMB = downloaded / (1024.0 * 1024.0);
                    var totalMB = totalBytes / (1024.0 * 1024.0);
                    StatusChanged?.Invoke($"Ollama 다운로드 중... ({downloadedMB:F0}/{totalMB:F0} MB)");
                    DownloadProgress?.Invoke(percent);
                }
            }

            // Run silent install (UseShellExecute=true → Windows UAC 권한 요청 가능)
            StatusChanged?.Invoke("Ollama 설치 중...");
            var psi = new ProcessStartInfo
            {
                FileName = setupPath,
                Arguments = "/VERYSILENT /NORESTART /SUPPRESSMSGBOXES",
                UseShellExecute = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                StatusChanged?.Invoke("설치 프로세스 시작 실패");
                return false;
            }

            await process.WaitForExitAsync(ct);

            // Verify installation
            await Task.Delay(2000, ct); // Wait for install to finalize
            if (FindOllamaPath() != null)
            {
                StatusChanged?.Invoke("Ollama 설치 완료");
                return true;
            }

            StatusChanged?.Invoke("Ollama 설치 확인 실패 — 수동 설치가 필요할 수 있습니다");
            return false;
        }
        catch (OperationCanceledException)
        {
            StatusChanged?.Invoke("Ollama 설치 취소됨");
            return false;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Ollama 설치 오류: {ex.Message}");
            return false;
        }
        finally
        {
            // Cleanup installer
            try { if (File.Exists(setupPath)) File.Delete(setupPath); } catch { }
        }
    }

    // ══════════════════════════════════════════════
    //  3. Process Management: Start / Stop Ollama
    // ══════════════════════════════════════════════

    /// <summary>
    /// Start the Ollama server process. If already running, does nothing.
    /// </summary>
    public async Task<bool> StartAsync(CancellationToken ct = default)
    {
        // Already running?
        if (await IsRunningAsync(ct))
            return true;

        var ollamaPath = FindOllamaPath();
        if (ollamaPath == null)
        {
            StatusChanged?.Invoke("Ollama 실행 파일을 찾을 수 없습니다");
            return false;
        }

        try
        {
            StatusChanged?.Invoke("Ollama 서버 시작 중...");

            // Try starting "ollama serve" (the server command)
            var psi = new ProcessStartInfo
            {
                FileName = ollamaPath,
                Arguments = "serve",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            // 챗 모델 + 비전 모델 + 임베딩 모델을 동시에 GPU에 유지 (모델 스왑 방지)
            psi.Environment["OLLAMA_MAX_LOADED_MODELS"] = "3";
            psi.Environment["OLLAMA_NUM_PARALLEL"] = "2";

            _ollamaProcess = Process.Start(psi);
            if (_ollamaProcess == null)
            {
                StatusChanged?.Invoke("Ollama 시작 실패");
                return false;
            }

            _processOwned = true;

            // Wait for server to become ready (up to 15 seconds)
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(500, ct);
                if (await IsRunningAsync(ct))
                {
                    StatusChanged?.Invoke("Ollama 서버 시작됨");
                    return true;
                }
            }

            StatusChanged?.Invoke("Ollama 서버 시작 대기 시간 초과");
            return false;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Ollama 시작 오류: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stop the Ollama process (only if we started it).
    /// </summary>
    public void Stop()
    {
        if (_ollamaProcess == null || !_processOwned) return;

        try
        {
            if (!_ollamaProcess.HasExited)
            {
                _ollamaProcess.Kill(entireProcessTree: true);
                _ollamaProcess.WaitForExit(5000);
            }
        }
        catch { }
        finally
        {
            _ollamaProcess.Dispose();
            _ollamaProcess = null;
            _processOwned = false;
        }
    }

    /// <summary>
    /// Kill existing Ollama and restart with OLLAMA_MAX_LOADED_MODELS=3.
    /// Use when Ollama is already running but wasn't started by this app.
    /// </summary>
    public async Task<bool> RestartWithMultiModelAsync(CancellationToken ct = default)
    {
        StatusChanged?.Invoke("Ollama 종료 중...");
        ForceKillAll();

        // 이전 프로세스가 완전히 종료될 때까지 대기 (최대 10초)
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(500, ct);
            if (!await IsRunningAsync(ct))
                break;
            if (i == 19)
            {
                StatusChanged?.Invoke("Ollama 종료 대기 시간 초과, 강제 재시작...");
                ForceKillAll();
                await Task.Delay(2000, ct);
            }
        }

        // 확실히 종료된 후 MAX_LOADED_MODELS=3으로 새 프로세스 시작
        StatusChanged?.Invoke("Ollama 시작 중 (다중 모델 로드 설정)...");
        return await StartAsync(ct);
    }

    /// <summary>
    /// Force-kill ALL running Ollama processes (업데이트 후 구버전 프로세스 제거용).
    /// </summary>
    public static void ForceKillAll()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("ollama"))
            {
                try { proc.Kill(entireProcessTree: true); proc.WaitForExit(3000); }
                catch { }
                finally { proc.Dispose(); }
            }
            // "ollama app" 프로세스도 종료 (Windows tray app)
            foreach (var proc in Process.GetProcessesByName("ollama app"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); }
                catch { }
                finally { proc.Dispose(); }
            }
        }
        catch { }
    }

    // ══════════════════════════════════════════════
    //  4. Model Management: List / Pull
    // ══════════════════════════════════════════════

    /// <summary>Get list of installed model names.</summary>
    public async Task<List<string>> GetInstalledModelsAsync(CancellationToken ct = default)
    {
        var models = new List<string>();
        try
        {
            using var response = await _http.GetAsync($"{_baseUrl}/api/tags", ct);
            if (!response.IsSuccessStatusCode) return models;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var modelName = name.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                            models.Add(modelName);
                    }
                }
            }
        }
        catch { }
        return models;
    }

    /// <summary>
    /// Ensure required models are installed. Auto-pulls missing models.
    /// </summary>
    public async Task<bool> EnsureModelsAsync(
        string chatModel, string embedModel, CancellationToken ct = default)
        => await EnsureModelsAsync(new[] { chatModel, embedModel }, ct);

    /// <summary>
    /// Ensure all specified models are installed.
    /// </summary>
    public async Task<bool> EnsureModelsAsync(
        IEnumerable<string> modelNames, CancellationToken ct = default)
    {
        var installed = await GetInstalledModelsAsync(ct);
        var installedNormalized = installed.Select(NormalizeModelName).ToHashSet();

        var modelsToCheck = modelNames
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();

        foreach (var model in modelsToCheck)
        {
            if (installedNormalized.Contains(NormalizeModelName(model)))
            {
                StatusChanged?.Invoke($"{model} ✓");
                continue;
            }

            StatusChanged?.Invoke($"{model} 다운로드 중...");
            var success = await PullModelAsync(model, ct);

            // 버전 부족 또는 pull 실패 시 → Ollama 버전 확인 후 자동 업데이트
            if (!success && !_needsUpdate)
            {
                // pull 실패했는데 _needsUpdate 안 걸린 경우 → 버전 직접 확인
                if (!await IsVersionRecentAsync(ct))
                    _needsUpdate = true;
            }

            if (!success && _needsUpdate)
            {
                StatusChanged?.Invoke("Ollama 최신 버전으로 업데이트 중...");
                Stop();          // 우리가 시작한 프로세스 중지
                ForceKillAll();  // 모든 Ollama 프로세스 강제 종료 (구버전 제거)
                await Task.Delay(2000, ct); // 프로세스 정리 대기

                var updated = await InstallAsync(ct);
                if (updated)
                {
                    _needsUpdate = false;
                    _ollamaProcess = null;
                    _processOwned = false;

                    // 업데이트된 새 버전으로 서버 시작
                    var restarted = await StartAsync(ct);
                    if (restarted)
                    {
                        StatusChanged?.Invoke($"{model} 다운로드 재시도 중...");
                        success = await PullModelAsync(model, ct);
                    }
                }
                else
                {
                    StatusChanged?.Invoke("Ollama 업데이트 실패 — 수동 설치: https://ollama.com");
                }
            }

            if (!success)
            {
                StatusChanged?.Invoke($"{model} 다운로드 실패");
                return false;
            }
        }

        return true;
    }

    /// <summary>Pull a model with streaming progress.</summary>
    public async Task<bool> PullModelAsync(string modelName, CancellationToken ct = default)
    {
        try
        {
            var body = new { name = modelName, stream = true };
            var json = JsonSerializer.Serialize(body);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/pull")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                var code = (int)response.StatusCode;
                // 412, 404, 또는 "newer version"/"not found" 등 → Ollama 업데이트 필요 가능성
                if (code == 412 ||
                    error.Contains("newer version", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("requires", StringComparison.OrdinalIgnoreCase) ||
                    (code == 404 && !await IsVersionRecentAsync(ct)))
                {
                    _needsUpdate = true;
                    StatusChanged?.Invoke($"{modelName}: Ollama 업데이트가 필요합니다.");
                }
                else
                {
                    StatusChanged?.Invoke($"다운로드 실패 ({response.StatusCode}): {Truncate(error, 200)}");
                }
                return false;
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";

                    if (root.TryGetProperty("total", out var total) &&
                        root.TryGetProperty("completed", out var completed))
                    {
                        var totalBytes = total.GetInt64();
                        var completedBytes = completed.GetInt64();
                        if (totalBytes > 0)
                        {
                            var percent = (double)completedBytes / totalBytes * 100;
                            var totalMB = totalBytes / (1024.0 * 1024.0);
                            var completedMB = completedBytes / (1024.0 * 1024.0);
                            StatusChanged?.Invoke(
                                $"{modelName}: {status} ({completedMB:F0}/{totalMB:F0} MB, {percent:F0}%)");
                            DownloadProgress?.Invoke(percent);
                        }
                    }
                    else
                    {
                        StatusChanged?.Invoke($"{modelName}: {status}");
                    }

                    if (root.TryGetProperty("error", out var err))
                    {
                        StatusChanged?.Invoke($"다운로드 오류: {err.GetString()}");
                        return false;
                    }
                }
                catch (JsonException) { }
            }

            StatusChanged?.Invoke($"{modelName} 다운로드 완료 ✓");
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusChanged?.Invoke($"{modelName} 다운로드 취소됨");
            return false;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"다운로드 오류: {ex.Message}");
            return false;
        }
    }

    // ══════════════════════════════════════════════
    //  4b. VRAM Warmup: Load models into GPU
    // ══════════════════════════════════════════════

    /// <summary>
    /// Load models into GPU VRAM by sending model-only requests (no prompt).
    /// Ollama loads the model and returns immediately without generating.
    /// </summary>
    public async Task WarmupModelsAsync(
        IEnumerable<string> modelNames, CancellationToken ct = default)
    {
        foreach (var model in modelNames.Where(m => !string.IsNullOrEmpty(m)).Distinct())
        {
            try
            {
                StatusChanged?.Invoke($"{model} VRAM 로드 중...");

                // 프롬프트 없이 model + keep_alive만 보내면 VRAM 로드 후 즉시 리턴
                var body = new { model, keep_alive = "30m" };
                var json = JsonSerializer.Serialize(body);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(120));
                using var response = await _http.SendAsync(request, cts.Token);

                StatusChanged?.Invoke(response.IsSuccessStatusCode
                    ? $"{model} VRAM 로드 완료 ✓"
                    : $"{model} VRAM 로드 실패 ({response.StatusCode})");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"{model} VRAM 로드 실패: {ex.Message}");
            }
        }
    }

    // ══════════════════════════════════════════════
    //  5. Full Setup: Install → Start → Pull Models
    // ══════════════════════════════════════════════

    /// <summary>
    /// Full automatic setup. Returns true when everything is ready.
    /// </summary>
    public async Task<bool> SetupAsync(
        string chatModel, string visionModel, string embedModel, CancellationToken ct = default)
    {
        var models = new[] { chatModel, visionModel, embedModel }.Distinct().ToArray();

        // Step 1: Check if Ollama is already running
        if (await IsRunningAsync(ct))
        {
            StatusChanged?.Invoke("Ollama 서버 연결됨");
            return await EnsureModelsAsync(models, ct);
        }

        // Step 2: Check if installed → start it
        if (IsInstalled())
        {
            var started = await StartAsync(ct);
            if (started)
                return await EnsureModelsAsync(models, ct);
        }

        // Step 3: Not installed → install first
        StatusChanged?.Invoke("Ollama가 설치되어 있지 않습니다. 자동 설치를 시작합니다...");
        var installed = await InstallAsync(ct);
        if (!installed) return false;

        // Step 4: Start after install
        var startedAfterInstall = await StartAsync(ct);
        if (!startedAfterInstall) return false;

        // Step 5: Pull models
        return await EnsureModelsAsync(models, ct);
    }

    // ══════════════════════════════════════════════
    //  Utilities
    // ══════════════════════════════════════════════

    private static string NormalizeModelName(string name)
    {
        var n = name.ToLowerInvariant().Trim();
        if (n.EndsWith(":latest"))
            n = n[..^":latest".Length];
        return n;
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length > maxLen ? s[..maxLen] + "..." : s;

    /// <summary>
    /// Ollama 버전이 0.7 이상인지 확인. 구버전이면 false (업데이트 필요).
    /// </summary>
    private async Task<bool> IsVersionRecentAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var resp = await _http.GetAsync($"{_baseUrl}/api/version", cts.Token);
            if (!resp.IsSuccessStatusCode) return false;
            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("version", out var ver))
            {
                var version = ver.GetString() ?? "";
                // "0.6.2" → 버전 파싱, 0.7.0 미만이면 구버전
                if (Version.TryParse(version, out var v))
                    return v >= new Version(0, 7, 0);
            }
        }
        catch { }
        return true; // 확인 실패 시 최신으로 간주 (불필요한 업데이트 방지)
    }

    public void Dispose()
    {
        Stop();
        _http.Dispose();
    }
}
