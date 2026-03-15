using System.IO;
using System.Text;
using MVXTester.Core.Registry;

namespace MVXTester.Chat;

/// <summary>
/// Extracts help content for RAG indexing using a hybrid approach:
///   - Static: Pre-generated .md files from Models/Chat/data/ (textbook, help guides)
///   - Runtime: Node metadata from KoreanDescriptions + NodeRegistry
///   - Runtime: Example project list from examples/ folder
/// </summary>
public static class HelpContentExtractor
{
    private const int MaxChunkLength = 1500;
    private const string DataFolderName = "Models/Chat/data";

    /// <summary>
    /// Extract all available help content as document chunks.
    /// </summary>
    public static List<DocumentChunk> ExtractAll(
        NodeRegistry? registry = null,
        Dictionary<string, (string Desc, string Apps)>? koreanDescriptions = null,
        Dictionary<string, string>? categoryNames = null)
    {
        var chunks = new List<DocumentChunk>();

        // 1. Runtime: Node descriptions from KoreanDescriptions dictionary
        if (koreanDescriptions != null)
            chunks.AddRange(ExtractNodeDescriptions(koreanDescriptions));

        // 2. Runtime: Node registry metadata (ports, properties from live instances)
        if (registry != null)
            chunks.AddRange(ExtractNodeRegistryInfo(registry));

        // 3. Static: Pre-generated .md files (textbook + help guides)
        chunks.AddRange(ExtractMarkdownFiles());

        // 4. Runtime: Example project list
        chunks.AddRange(ExtractExampleProjects());

        return chunks;
    }

    // ═══════════════════════════════════════════════════════════
    //  1+2. Node Descriptions + Registry (병합: 한 노드 = 1 청크)
    // ═══════════════════════════════════════════════════════════

    private static List<DocumentChunk> ExtractNodeDescriptions(
        Dictionary<string, (string Desc, string Apps)> descriptions)
    {
        // node_reference는 더 이상 별도 생성 안 함 — ExtractNodeRegistryInfo에서 병합
        return new List<DocumentChunk>();
    }

    private static List<DocumentChunk> ExtractNodeRegistryInfo(NodeRegistry registry)
    {
        var chunks = new List<DocumentChunk>();
        // KoreanDescriptions 로드 (병합용)
        var korDescs = ViewModels.NodeDescriptions.GetKoreanDescriptions();

        foreach (var entry in registry.Entries)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[노드] {entry.Name} (카테고리: {entry.Category})");

                // 한국어 설명 병합 (기존 node_reference 역할)
                if (korDescs.TryGetValue(entry.Name, out var kd))
                {
                    sb.AppendLine($"설명: {kd.Desc}");
                    if (!string.IsNullOrEmpty(kd.Apps))
                        sb.AppendLine($"응용: {kd.Apps}");
                }
                else if (!string.IsNullOrEmpty(entry.Description))
                {
                    sb.AppendLine($"설명: {entry.Description}");
                }

                // 포트/속성 (기존 node_registry 역할)
                var node = registry.CreateNode(entry);
                try
                {
                    if (node.Inputs.Count > 0)
                    {
                        sb.Append("입력: ");
                        sb.AppendLine(string.Join(", ", node.Inputs.Select(p => $"{p.Name}({p.DataType.Name})")));
                    }
                    if (node.Outputs.Count > 0)
                    {
                        sb.Append("출력: ");
                        sb.AppendLine(string.Join(", ", node.Outputs.Select(p => $"{p.Name}({p.DataType.Name})")));
                    }
                    if (node.Properties.Count > 0)
                    {
                        sb.Append("속성: ");
                        sb.AppendLine(string.Join(", ", node.Properties.Select(p => p.DisplayName)));
                    }
                }
                finally
                {
                    if (node is IDisposable disposable) disposable.Dispose();
                }

                chunks.Add(new DocumentChunk
                {
                    Id = $"node_{Sanitize(entry.Name)}",
                    Text = sb.ToString().TrimEnd(),
                    Source = "node",
                    Category = entry.Category
                });
            }
            catch
            {
                // Skip nodes that can't be instantiated
            }
        }

        return chunks;
    }

    // ═══════════════════════════════════════════════════════════
    //  3. Pre-generated Markdown files (textbook + help guides)
    // ═══════════════════════════════════════════════════════════

    private static List<DocumentChunk> ExtractMarkdownFiles()
    {
        var chunks = new List<DocumentChunk>();
        var dataDir = FindDirectory(AppDomain.CurrentDomain.BaseDirectory, DataFolderName);
        if (dataDir == null) return chunks;

        try
        {
            var mdFiles = Directory.GetFiles(dataDir, "*.md").OrderBy(f => f).ToList();
            foreach (var filePath in mdFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var isTextbook = fileName.StartsWith("textbook_", StringComparison.OrdinalIgnoreCase);
                var source = isTextbook ? "textbook" : "help";

                chunks.AddRange(ParseMarkdownIntoChunks(filePath, source));
            }
        }
        catch
        {
            // Skip if directory can't be read
        }

        return chunks;
    }

    /// <summary>
    /// Parse a markdown file into document chunks by splitting on headings (## / ### / ####).
    /// Each section (heading + body) becomes one or more chunks.
    /// </summary>
    private static List<DocumentChunk> ParseMarkdownIntoChunks(string filePath, string source)
    {
        var chunks = new List<DocumentChunk>();

        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var lines = text.Split('\n');

            string currentHeading = "";
            var currentBody = new StringBuilder();
            int sectionIdx = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();

                // Check for markdown headings (##, ###, ####)
                if (trimmed.StartsWith("## ") || trimmed.StartsWith("### "))
                {
                    // Save previous section
                    FlushSection(chunks, fileName, source, ref sectionIdx, currentHeading, currentBody);
                    currentBody.Clear();
                    currentHeading = trimmed.TrimStart('#', ' ');
                }
                else if (trimmed.StartsWith("#### "))
                {
                    // H4 is a sub-heading within the current section
                    currentBody.AppendLine();
                    currentBody.AppendLine(trimmed.TrimStart('#', ' '));
                }
                else
                {
                    currentBody.AppendLine(line);
                }
            }

            // Save last section
            FlushSection(chunks, fileName, source, ref sectionIdx, currentHeading, currentBody);
        }
        catch
        {
            // Skip files that can't be read
        }

        return chunks;
    }

    private static void FlushSection(
        List<DocumentChunk> chunks, string fileName, string source,
        ref int sectionIdx, string heading, StringBuilder body)
    {
        if (body.Length == 0 && string.IsNullOrEmpty(heading))
            return;

        var prefix = source == "textbook" ? "[학습교재]" : "[도움말]";
        var headingLine = string.IsNullOrEmpty(heading) ? "" : $"{prefix} {heading.Trim()}";
        var bodyText = body.ToString().Trim();
        var fullText = string.IsNullOrEmpty(headingLine)
            ? bodyText
            : $"{headingLine}\n{bodyText}";

        if (fullText.Length < 30) return;

        var subChunks = SplitLongText(fullText, MaxChunkLength);
        int subIdx = 0;
        foreach (var sub in subChunks)
        {
            var trimmed = sub.Trim();
            if (trimmed.Length < 50) continue; // Skip tiny fragments

            // Prepend heading context to sub-chunks that lost it
            var chunkText = trimmed;
            if (subIdx > 0 && !string.IsNullOrEmpty(headingLine) && !trimmed.StartsWith(prefix))
            {
                chunkText = $"{headingLine} (계속)\n{trimmed}";
            }

            chunks.Add(new DocumentChunk
            {
                Id = $"{source}_{fileName}_s{sectionIdx}_{subIdx}",
                Text = StripMarkdownDecorations(chunkText),
                Source = source,
                Category = ExtractCategoryFromHeading(heading)
            });
            subIdx++;
        }
        sectionIdx++;
    }

    // ═══════════════════════════════════════════════════════════
    //  4. Example projects
    // ═══════════════════════════════════════════════════════════

    private static List<DocumentChunk> ExtractExampleProjects()
    {
        var chunks = new List<DocumentChunk>();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var examplesDir = FindDirectory(baseDir, "examples");
        if (examplesDir == null) return chunks;

        try
        {
            var exampleFiles = Directory.GetFiles(examplesDir, "*.mvxp")
                .Concat(Directory.GetFiles(examplesDir, "*.mvx"))
                .OrderBy(f => f)
                .ToList();

            if (exampleFiles.Count == 0) return chunks;

            var sb = new StringBuilder();
            sb.AppendLine("[예제 프로젝트] MVXTester 예제 목록");
            sb.AppendLine("Open 메뉴 또는 Ctrl+O로 examples/ 폴더에서 예제를 열 수 있습니다.");
            foreach (var file in exampleFiles)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var desc = name.Replace("_", " ").Replace("-", " ");
                sb.AppendLine($"- {desc} ({Path.GetExtension(file)})");
            }

            chunks.Add(new DocumentChunk
            {
                Id = "examples_list",
                Text = sb.ToString(),
                Source = "examples",
                Category = "예제"
            });
        }
        catch { }

        return chunks;
    }

    // ═══════════════════════════════════════════════════════════
    //  Utilities
    // ═══════════════════════════════════════════════════════════

    private const int OverlapLength = 200;

    private static List<string> SplitLongText(string text, int maxLen)
    {
        if (text.Length <= maxLen)
            return new List<string> { text };

        var chunks = new List<string>();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var allParas = paragraphs.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

        var current = new StringBuilder();
        string lastPara = ""; // 오버랩용: 직전 청크의 마지막 문단

        for (int i = 0; i < allParas.Count; i++)
        {
            var para = allParas[i];

            // 개별 문단이 maxLen 초과 시 강제 분할
            if (para.Length > maxLen)
            {
                if (current.Length > 0)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }
                // 긴 문단을 maxLen 단위로 자르기
                for (int pos = 0; pos < para.Length; pos += maxLen - OverlapLength)
                {
                    var end = Math.Min(pos + maxLen, para.Length);
                    chunks.Add(para[pos..end]);
                }
                lastPara = "";
                continue;
            }

            if (current.Length + para.Length + 2 > maxLen && current.Length > 0)
            {
                chunks.Add(current.ToString());
                lastPara = allParas[i - 1]; // 직전 문단 저장

                // 오버랩: 직전 문단을 다음 청크 시작에 포함 (200자 이내만)
                current.Clear();
                if (lastPara.Length <= OverlapLength)
                {
                    current.Append(lastPara);
                }
            }

            if (current.Length > 0) current.AppendLine();
            current.Append(para);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString());

        return chunks;
    }

    private static string ExtractCategoryFromHeading(string heading)
    {
        if (string.IsNullOrEmpty(heading)) return "일반";

        var h = heading.ToLowerInvariant();
        if (h.Contains("input") || h.Contains("output") || h.Contains("입출력")) return "Input/Output";
        if (h.Contains("color") || h.Contains("색상")) return "Color";
        if (h.Contains("filter") || h.Contains("필터")) return "Filter";
        if (h.Contains("edge") || h.Contains("에지")) return "Edge";
        if (h.Contains("morphology") || h.Contains("모폴로지")) return "Morphology";
        if (h.Contains("threshold") || h.Contains("임계")) return "Threshold";
        if (h.Contains("contour") || h.Contains("윤곽")) return "Contour";
        if (h.Contains("feature") || h.Contains("특징")) return "Feature";
        if (h.Contains("drawing") || h.Contains("그리기")) return "Drawing";
        if (h.Contains("transform") || h.Contains("변환")) return "Transform";
        if (h.Contains("histogram") || h.Contains("히스토그램")) return "Histogram";
        if (h.Contains("arithmetic") || h.Contains("산술")) return "Arithmetic";
        if (h.Contains("detection") || h.Contains("검출") || h.Contains("탐지")) return "Detection";
        if (h.Contains("segmentation") || h.Contains("분할")) return "Segmentation";
        if (h.Contains("value") || h.Contains("값")) return "Value";
        if (h.Contains("control") || h.Contains("제어")) return "Control";
        if (h.Contains("communication") || h.Contains("통신")) return "Communication";
        if (h.Contains("data") || h.Contains("데이터")) return "Data";
        if (h.Contains("event") || h.Contains("이벤트")) return "Event";
        if (h.Contains("script") || h.Contains("스크립트")) return "Script";
        if (h.Contains("inspection") || h.Contains("검사")) return "Inspection";
        if (h.Contains("measurement") || h.Contains("측정")) return "Measurement";
        if (h.Contains("mediapipe")) return "MediaPipe";
        if (h.Contains("yolo")) return "YOLO";
        if (h.Contains("ocr")) return "OCR";
        if (h.Contains("llm") || h.Contains("vlm") || h.Contains("vision")) return "LLM/VLM";
        if (h.Contains("tutorial") || h.Contains("튜토리얼") || h.Contains("예제")) return "튜토리얼";
        if (h.Contains("사용") || h.Contains("usage")) return "사용법";
        if (h.Contains("단축키") || h.Contains("shortcut") || h.Contains("keyboard")) return "단축키";
        if (h.Contains("설치") || h.Contains("setup") || h.Contains("install")) return "설치";
        if (h.Contains("구조") || h.Contains("architecture") || h.Contains("structure")) return "구조";
        if (h.Contains("기술") || h.Contains("tech")) return "기술스택";
        if (h.Contains("카메라") || h.Contains("camera")) return "카메라";
        if (h.Contains("파이프라인") || h.Contains("pipeline")) return "파이프라인";

        return "일반";
    }

    private static string? FindDirectory(string baseDir, string dirPath)
    {
        var path = Path.Combine(baseDir, dirPath);
        if (Directory.Exists(path)) return path;

        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            dir = dir.Parent;
            if (dir == null) break;
            path = Path.Combine(dir.FullName, dirPath);
            if (Directory.Exists(path)) return path;
        }
        return null;
    }

    /// <summary>
    /// 청크 텍스트에서 마크다운 장식을 제거하여 토큰 절약.
    /// 볼드(**), 테이블 구분선(| --- |), 코드블록(```) 등 제거.
    /// </summary>
    private static string StripMarkdownDecorations(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var lines = text.Split('\n');
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            var l = line;

            // 테이블 구분선 (| --- | --- |) 제거
            if (System.Text.RegularExpressions.Regex.IsMatch(l.Trim(), @"^\|[\s\-:|\+]+\|$"))
                continue;

            // 코드블록 (```) 제거
            if (l.TrimStart().StartsWith("```"))
                continue;

            // 수평선 (---, ***) 제거
            if (System.Text.RegularExpressions.Regex.IsMatch(l.Trim(), @"^[-\*_]{3,}$"))
                continue;

            // 볼드 (**text**) -> text
            l = l.Replace("**", "");

            // 테이블 행에서 | 구분자를 공백으로 변환
            if (l.Trim().StartsWith('|') && l.Trim().EndsWith('|'))
            {
                l = l.Trim().Trim('|');
                var cells = l.Split('|');
                l = string.Join("  ", cells.Select(c => c.Trim()).Where(c => c.Length > 0));
            }

            sb.AppendLine(l);
        }

        var result = sb.ToString();

        // 최대 청크 크기 강제 (오버랩 버그로 초대형 청크 방지)
        if (result.Length > MaxChunkLength + OverlapLength)
            result = result[..(MaxChunkLength + OverlapLength)];

        return result.TrimEnd();
    }

    private static string Sanitize(string name) =>
        name.Replace(" ", "_").Replace("/", "_").ToLowerInvariant();
}
