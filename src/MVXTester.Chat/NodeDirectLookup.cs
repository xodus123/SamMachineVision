using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MVXTester.Core.Models;
using MVXTester.Core.Registry;

namespace MVXTester.Chat;

/// <summary>
/// NodeRegistry에서 직접 조회하여 LLM 없이 정확한 답변을 반환.
/// 패턴 매칭으로 노드 카테고리, 목록, 상세 정보를 즉시 응답.
/// </summary>
public sealed class NodeDirectLookup
{
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, (string Desc, string Apps)> _koreanDescs;
    private readonly Dictionary<string, string> _categoryNames; // "Input" -> "입출력"

    // 노드명 인덱스: 다양한 한/영 표현 → 정규 영문명
    private readonly Dictionary<string, string> _nameIndex;

    // 카테고리 인덱스: 한/영 표현 → 정규 카테고리 키 (NodeCategories 값)
    private readonly Dictionary<string, string> _categoryIndex;

    // 에러/트러블슈팅 키워드 (이 키워드 포함 시 직접 조회 건너뜀)
    private static readonly string[] ErrorKeywords =
    {
        "오류", "에러", "error", "안됨", "안돼", "안나", "실패", "failed",
        "왜 안", "안 되", "못 ", "문제", "버그", "트러블", "해결"
    };

    // 상세 질문 키워드 (노드명 + 이 키워드 → 상세 정보 반환)
    private static readonly string[] DetailKeywords =
    {
        "설명", "뭐", "파라미터", "속성", "포트", "입력", "출력",
        "사용법", "어떻게", "알려", "무슨"
    };

    // 카테고리/목록 질문 키워드
    private static readonly string[] CategoryQueryKeywords =
    {
        "카테고리", "분류", "어디", "어느", "속하", "그룹", "목록"
    };

    // 목록 나열 키워드
    private static readonly string[] ListKeywords =
    {
        "목록", "리스트", "어떤", "뭐가", "뭐 있", "있나", "알려", "종류", "몇"
    };

    public NodeDirectLookup(
        NodeRegistry registry,
        Dictionary<string, (string Desc, string Apps)>? koreanDescs,
        Dictionary<string, string>? categoryNames)
    {
        _registry = registry;
        _koreanDescs = koreanDescs ?? new();
        _categoryNames = categoryNames ?? new();
        _nameIndex = BuildNameIndex();
        _categoryIndex = BuildCategoryIndex();
    }

    /// <summary>
    /// 질문에 대해 직접 응답을 시도. 매칭 안 되면 null 반환.
    /// </summary>
    public string? TryAnswer(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return null;

        var normalized = KoreanTextNormalizer.Normalize(question).ToLowerInvariant();

        // 에러/트러블슈팅 질문은 건너뜀 (RAG+LLM이 더 적합)
        if (ErrorKeywords.Any(k => normalized.Contains(k)))
            return null;

        // 패턴 0: 전체 주제 질문 (요약 직접 반환)
        var topicAnswer = TryTopicAnswer(normalized);
        if (topicAnswer != null)
            return topicAnswer;

        // 패턴 1: 전체 카테고리 목록
        if (IsAllCategoryQuery(normalized))
            return FormatAllCategories();

        // 패턴 2: 특정 카테고리의 노드 목록
        var category = FindCategory(normalized);
        if (category != null && ListKeywords.Any(k => normalized.Contains(k)))
            return FormatCategoryNodes(category);

        // 패턴 3: 특정 노드의 카테고리 조회
        var nodeName = FindNodeName(normalized);
        if (nodeName != null && CategoryQueryKeywords.Any(k => normalized.Contains(k)))
            return FormatNodeCategory(nodeName);

        // 패턴 4: 특정 노드 상세 (포트/속성/설명)
        if (nodeName != null &&
            (DetailKeywords.Any(k => normalized.Contains(k)) || question.Length < 25))
            return FormatNodeDetail(nodeName);

        // 패턴 5: 카테고리만 언급 (노드명 없음) + 질문 키워드
        if (category != null && DetailKeywords.Any(k => normalized.Contains(k)))
            return FormatCategoryNodes(category);

        return null;
    }

    // ══════════════════════════════════════════════
    //  패턴 감지
    // ══════════════════════════════════════════════

    /// <summary>
    /// 전체 주제 질문 매칭. 요약 텍스트를 직접 반환.
    /// </summary>
    private string? TryTopicAnswer(string q)
    {
        // 툴바/메뉴 기능
        if (q.Contains("툴바") || q.Contains("메뉴") || q.Contains("버튼"))
        {
            if (DetailKeywords.Any(k => q.Contains(k)) || q.Contains("기능") || q.Length < 20)
                return FormatToolbarSummary();
        }

        // 노드 팔레트
        if (q.Contains("팔레트") || q.Contains("palette"))
        {
            if (DetailKeywords.Any(k => q.Contains(k)) || q.Contains("목록") || q.Length < 20)
                return FormatAllCategories();
        }

        // 튜토리얼/예제 목록
        if (q.Contains("튜토리얼") || q.Contains("tutorial") || q.Contains("학습") ||
            (q.Contains("예제") && (q.Contains("목록") || q.Contains("전체") || q.Contains("어떤"))))
            return FormatTutorialSummary();

        // 단축키
        if (q.Contains("단축키") || q.Contains("shortcut") || q.Contains("키보드"))
            return FormatShortcutSummary();

        // 프로그램 설명/개요
        if ((q.Contains("프로그램") || q.Contains("앱") || q.Contains("mvxtester") || q.Contains("소프트웨어")) &&
            (DetailKeywords.Any(k => q.Contains(k)) || q.Contains("뭐") || q.Length < 25))
            return FormatProgramSummary();

        // 실행 모드
        if (q.Contains("실행") && (q.Contains("모드") || q.Contains("종류") || q.Contains("차이")))
            return FormatExecutionModes();

        return null;
    }

    private static string FormatToolbarSummary()
    {
        return @"[MVXTester 툴바 기능]

- New (Ctrl+N): 새 그래프 생성
- Open (Ctrl+O): 프로젝트 파일 열기
- Save (Ctrl+S) / Save As (Ctrl+Shift+S): 프로젝트 저장
- Import Function: 함수 노드 가져오기 (.mvxp 파일을 재사용 가능한 노드로 등록)
- Python Code: 현재 그래프를 Python(OpenCV) 코드로 변환
- C# Code: 현재 그래프를 C#(OpenCvSharp) 코드로 변환
- Execute (F5): 파이프라인 실행 (dirty 노드만 재실행)
- Force Execute (Ctrl+F5): 모든 노드 강제 재실행
- Stream (F6): 연속 스트리밍 모드 (실시간 카메라용)
- Auto Execute: 속성 변경 시 자동 재실행
- HelperBot: AI 도움말 챗봇
- Help (F1): 사용자 설명서
- 테마 전환: Dark/Light 테마".TrimStart();
    }

    private static string FormatTutorialSummary()
    {
        return @"[MVXTester 튜토리얼 목록]

- Tutorial 1: 기본 이미지 처리 (Image Read -> Grayscale -> Blur -> Canny Edge)
- Tutorial 2: 컨투어를 이용한 객체 검출 (이진화 -> FindContours -> DrawContours)
- Tutorial 3: 라이브 카메라 처리 (Camera -> 실시간 에지 검출)
- Tutorial 4: 템플릿 매칭 (Template Match로 패턴 찾기)
- Tutorial 5: 제어 흐름 사용 (For 루프로 배치 이미지 처리)
- Tutorial 6: 재사용 가능한 함수 만들기 (Import Function)
- Tutorial 7: 산업용 검사 (불량 검출 파이프라인)
- Tutorial 8: 얼굴 검출 (MediaPipe Face Detection)
- Tutorial 9: 손 추적 (MediaPipe Hand Landmark)
- Tutorial 10: 포즈 추정 (MediaPipe Pose Landmark)
- Tutorial 11: 페이스 메시 468 포인트 (MediaPipe Face Mesh)
- Tutorial 12: 배경 제거 (MediaPipe Selfie Segmentation)
- Tutorial 13: 객체 검출 COCO 80 클래스 (MediaPipe Object Detection)
- Tutorial 14: YOLOv8 객체 검출
- Tutorial 15: 실시간 YOLOv8 카메라
- Tutorial 16: 텍스트 인식 PaddleOCR (한/중/영)
- Tutorial 17: 한국어/영어 OCR (Tesseract)
- Tutorial 18: Tesseract OCR 상세".TrimStart();
    }

    private static string FormatShortcutSummary()
    {
        return @"[MVXTester 키보드 단축키]

- Ctrl+N: 새 그래프
- Ctrl+O: 프로젝트 열기
- Ctrl+S: 저장
- Ctrl+Shift+S: 다른 이름으로 저장
- F5: 실행 / Runtime 중지
- Ctrl+F5: 강제 실행 (모든 노드)
- F6: 스트리밍 토글
- Escape: 실행 중지
- F1: 도움말
- Ctrl+Z: 실행 취소
- Ctrl+Y: 다시 실행
- Delete: 선택 노드 삭제
- Ctrl+A: 전체 선택
- Ctrl+C/V: 복사/붙여넣기".TrimStart();
    }

    private static string FormatProgramSummary()
    {
        return @"[MVXTester 프로그램 개요]

MVXTester(SamMachineVision)은 노드 기반 비주얼 프로그래밍 환경으로, 코드 작성 없이 이미지 처리 파이프라인을 구축할 수 있습니다.

주요 특징:
- 노드를 캔버스에 드래그 & 연결하여 비전 워크플로우 생성
- 160개+ 노드: 이미지 처리, 에지 검출, 모폴로지, 컨투어, YOLO, OCR, MediaPipe 등
- 실시간 카메라 지원: USB, HIKROBOT, Cognex GigE
- 코드 자동 생성: Python(OpenCV), C#(OpenCvSharp)
- AI 도움말 챗봇 (HelperBot) 내장

실행 모드:
- Execute (F5): dirty 노드만 재실행
- Force Execute (Ctrl+F5): 모든 노드 강제 재실행
- Stream (F6): 연속 스트리밍 (실시간 카메라용)

기술 스택: .NET 8, WPF, OpenCvSharp, ONNX Runtime".TrimStart();
    }

    private static string FormatExecutionModes()
    {
        return @"[MVXTester 실행 모드]

- Execute (F5): Runtime 모드. dirty 상태 노드만 재실행합니다. 파라미터 튜닝에 적합합니다.
- Force Execute (Ctrl+F5): 모든 노드를 강제로 재실행합니다. 전체 파이프라인을 처음부터 다시 실행합니다.
- Stream (F6): 최대 FPS로 매 프레임마다 모든 노드를 재실행합니다. 라이브 카메라 실시간 처리용입니다.
- Auto Execute: 체크하면 속성 변경 시 자동으로 재실행합니다 (150ms 디바운스). Runtime 모드(F5) 실행 중에만 동작합니다.".TrimStart();
    }

    private static bool IsAllCategoryQuery(string q)
    {
        return (q.Contains("전체") || q.Contains("모든") || q.Contains("모두")) &&
               (q.Contains("카테고리") || q.Contains("분류") || q.Contains("목록"));
    }

    private string? FindNodeName(string query)
    {
        // 긴 키부터 매칭 (가우시안 블러 > 블러)
        foreach (var (key, name) in _nameIndex.OrderByDescending(k => k.Key.Length))
        {
            // 짧은 키(4자 이하)는 단어 경계에서만 매칭 ("for"가 "force"에 매칭 방지)
            if (key.Length <= 4)
            {
                var idx = query.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var before = idx > 0 ? query[idx - 1] : ' ';
                var after = idx + key.Length < query.Length ? query[idx + key.Length] : ' ';
                if (char.IsLetterOrDigit(before) || char.IsLetterOrDigit(after)) continue;
                return name;
            }

            if (query.Contains(key))
                return name;
        }
        return null;
    }

    private string? FindCategory(string query)
    {
        foreach (var (key, cat) in _categoryIndex.OrderByDescending(k => k.Key.Length))
        {
            if (query.Contains(key))
                return cat;
        }
        return null;
    }

    // ══════════════════════════════════════════════
    //  응답 포맷팅
    // ══════════════════════════════════════════════

    private string FormatAllCategories()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[MVXTester 노드 카테고리 목록]");
        sb.AppendLine();

        var byCategory = _registry.GetByCategory();
        int idx = 1;
        foreach (var (cat, entries) in byCategory)
        {
            var korName = GetCategoryDisplayName(cat);
            sb.AppendLine($"{idx}. {korName} ({cat}) - {entries.Count}개 노드");
            idx++;
        }

        return sb.ToString().TrimEnd();
    }

    private string? FormatCategoryNodes(string category)
    {
        var entries = _registry.Entries
            .Where(e => e.Category == category)
            .OrderBy(e => e.Name)
            .ToList();

        if (entries.Count == 0) return null;

        var korName = GetCategoryDisplayName(category);
        var sb = new StringBuilder();
        sb.AppendLine($"[{korName} ({category}) 카테고리] ({entries.Count}개 노드)");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            var desc = _koreanDescs.TryGetValue(entry.Name, out var kd) ? kd.Desc : entry.Description;
            // 설명이 길면 첫 문장만
            var shortDesc = desc.Length > 60 ? desc[..60].TrimEnd() + "..." : desc;
            sb.AppendLine($"- {entry.Name}({GetKoreanNodeName(entry.Name)}): {shortDesc}");
        }

        return sb.ToString().TrimEnd();
    }

    private string? FormatNodeCategory(string nodeName)
    {
        var entry = _registry.Entries.FirstOrDefault(e =>
            e.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
        if (entry == null) return null;

        var korCat = GetCategoryDisplayName(entry.Category);
        var sb = new StringBuilder();
        sb.AppendLine($"{entry.Name}({GetKoreanNodeName(entry.Name)}) 노드는 [{korCat}] 카테고리에 있습니다.");

        if (_koreanDescs.TryGetValue(entry.Name, out var kd))
        {
            sb.AppendLine();
            sb.AppendLine($"설명: {kd.Desc}");
            if (!string.IsNullOrEmpty(kd.Apps))
            {
                sb.AppendLine();
                sb.AppendLine($"응용: {kd.Apps}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string? FormatNodeDetail(string nodeName)
    {
        var entry = _registry.Entries.FirstOrDefault(e =>
            e.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
        if (entry == null) return null;

        var korCat = GetCategoryDisplayName(entry.Category);
        var sb = new StringBuilder();
        sb.AppendLine($"[{entry.Name}({GetKoreanNodeName(entry.Name)})] - {korCat} 카테고리");
        sb.AppendLine();

        // 한국어 설명
        if (_koreanDescs.TryGetValue(entry.Name, out var kd))
        {
            sb.AppendLine(kd.Desc);
            if (!string.IsNullOrEmpty(kd.Apps))
            {
                sb.AppendLine();
                sb.AppendLine($"응용 분야: {kd.Apps}");
            }
        }
        else if (!string.IsNullOrEmpty(entry.Description))
        {
            sb.AppendLine(entry.Description);
        }

        // 포트/속성 정보 (노드 인스턴스 생성)
        try
        {
            var node = _registry.CreateNode(entry);
            try
            {
                // 입력 포트
                if (node.Inputs.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("입력 포트:");
                    foreach (var p in node.Inputs)
                        sb.AppendLine($"- {p.Name} ({FriendlyType(p.DataType)})");
                }

                // 출력 포트
                if (node.Outputs.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("출력 포트:");
                    foreach (var p in node.Outputs)
                        sb.AppendLine($"- {p.Name} ({FriendlyType(p.DataType)})");
                }

                // 속성
                if (node.Properties.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("속성(파라미터):");
                    foreach (var p in node.Properties)
                    {
                        var range = "";
                        if (p.MinValue != null && p.MaxValue != null)
                            range = $", 범위: {p.MinValue}~{p.MaxValue}";
                        var def = p.DefaultValue != null ? $", 기본값: {p.DefaultValue}" : "";
                        sb.AppendLine($"- {p.DisplayName}: {FriendlyType(p.ValueType)}{def}{range}");
                    }
                }
            }
            finally
            {
                if (node is IDisposable d) d.Dispose();
            }
        }
        catch
        {
            // 노드 인스턴스 생성 실패 시 설명만 반환
        }

        return sb.ToString().TrimEnd();
    }

    // ══════════════════════════════════════════════
    //  인덱스 빌드
    // ══════════════════════════════════════════════

    private Dictionary<string, string> BuildNameIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _registry.Entries)
        {
            var name = entry.Name;
            var lower = name.ToLowerInvariant();

            // 영문명
            index.TryAdd(lower, name);
            // 공백 제거 버전
            index.TryAdd(lower.Replace(" ", ""), name);
        }

        // KoreanDescriptions에서 한국어 별칭 추출
        foreach (var (engName, (desc, _)) in _koreanDescs)
        {
            // 설명 첫 부분에서 한국어 명칭 추출 ("가우시안 블러를 적용하여..." → "가우시안 블러")
            var korName = ExtractKoreanName(desc);
            if (!string.IsNullOrEmpty(korName))
            {
                index.TryAdd(korName.ToLowerInvariant(), engName);
                index.TryAdd(korName.Replace(" ", "").ToLowerInvariant(), engName);
            }
        }

        // JSON 파일에서 한국어 별칭 로드 (node_aliases.json)
        var jsonAliases = LoadAliasesFromJson();
        if (jsonAliases?.NodeAliases != null)
        {
            foreach (var (alias, name) in jsonAliases.NodeAliases)
                index.TryAdd(alias.ToLowerInvariant(), name);
        }

        return index;
    }

    private Dictionary<string, string> BuildCategoryIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 영문 카테고리명 → 카테고리 값
        foreach (var entry in _registry.Entries)
        {
            index.TryAdd(entry.Category.ToLowerInvariant(), entry.Category);
        }

        // 한국어 카테고리명
        foreach (var (shortKey, korName) in _categoryNames)
        {
            // shortKey: "Input", korName: "입출력"
            // 실제 카테고리 값 찾기
            var catValue = _registry.Entries
                .FirstOrDefault(e => e.Category.StartsWith(shortKey, StringComparison.OrdinalIgnoreCase))
                ?.Category;
            if (catValue != null)
            {
                index.TryAdd(korName, catValue);
                index.TryAdd(shortKey.ToLowerInvariant(), catValue);
            }
        }

        // JSON 파일에서 카테고리 별칭 로드 (node_aliases.json)
        var jsonAliases = LoadAliasesFromJson();
        if (jsonAliases?.CategoryAliases != null)
        {
            foreach (var (alias, catValue) in jsonAliases.CategoryAliases)
                index.TryAdd(alias.ToLowerInvariant(), catValue);
        }

        return index;
    }

    // ══════════════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════════════

    private string GetCategoryDisplayName(string category)
    {
        // "Input/Output" → "Input" → "입출력"
        foreach (var (key, kor) in _categoryNames)
        {
            if (category.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                return $"{kor}({category})";
        }
        return category;
    }

    private string GetKoreanNodeName(string engName)
    {
        if (_koreanDescs.TryGetValue(engName, out var kd))
        {
            var korName = ExtractKoreanName(kd.Desc);
            if (!string.IsNullOrEmpty(korName))
                return korName;
        }
        return engName;
    }

    private static string ExtractKoreanName(string description)
    {
        if (string.IsNullOrEmpty(description)) return "";

        // "가우시안 블러를 적용하여..." → "가우시안 블러"
        // "이미지에서 사각형 영역을 잘라내는..." → ""  (주어가 한국어 명칭이 아님)
        var match = Regex.Match(description, @"^([가-힣a-zA-Z0-9\s]{2,15}?)[을를의에로는이가]");
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }

    // ══════════════════════════════════════════════
    //  JSON 별칭 로드
    // ══════════════════════════════════════════════

    private static AliasData? _cachedAliases;

    private static AliasData? LoadAliasesFromJson()
    {
        if (_cachedAliases != null) return _cachedAliases;

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, "Models", "Chat", "node_aliases.json");

            if (!File.Exists(path))
            {
                // 상위 디렉토리 탐색
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 5; i++)
                {
                    dir = dir.Parent;
                    if (dir == null) break;
                    var candidate = Path.Combine(dir.FullName, "Data", "node_aliases.json");
                    if (File.Exists(candidate)) { path = candidate; break; }
                    candidate = Path.Combine(dir.FullName, "Models", "Chat", "node_aliases.json");
                    if (File.Exists(candidate)) { path = candidate; break; }
                }
            }

            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _cachedAliases = JsonSerializer.Deserialize<AliasData>(json, options);
            return _cachedAliases;
        }
        catch
        {
            return null;
        }
    }

    private sealed class AliasData
    {
        [System.Text.Json.Serialization.JsonPropertyName("node_aliases")]
        public Dictionary<string, string>? NodeAliases { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("category_aliases")]
        public Dictionary<string, string>? CategoryAliases { get; set; }
    }

    // ══════════════════════════════════════════════

    private static string FriendlyType(Type type)
    {
        var name = type.Name;
        return name switch
        {
            "Mat" => "이미지(Mat)",
            "Int32" => "정수",
            "Single" => "실수(float)",
            "Double" => "실수(double)",
            "String" => "문자열",
            "Boolean" => "불리언",
            "Point" => "좌표(Point)",
            "Size" => "크기(Size)",
            "Scalar" => "색상(Scalar)",
            "Rect" => "사각형(Rect)",
            _ => name
        };
    }
}
