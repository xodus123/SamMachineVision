# MVXTester

노드 기반 머신비전 테스트 애플리케이션. 시각적 그래프 에디터에서 노드를 연결하여 이미지 처리 파이프라인을 구성하고 실행할 수 있습니다.

## 주요 기능

- **노드 기반 그래프 에디터** - 드래그 앤 드롭으로 노드 배치 및 연결
- **실시간 스트리밍** - HIK GigE 카메라, USB 카메라 등 실시간 영상 처리
- **146개 노드** - 24개 카테고리에 걸친 다양한 영상처리/검사/측정 노드
- **코드 생성** - 구성한 그래프를 Python 또는 C# 코드로 자동 변환
- **Undo/Redo** - 최대 100단계 작업 이력 관리
- **다크 테마** - Catppuccin Mocha 기반 다크 UI

## 스크린샷

| 구성요소 | 설명 |
|---------|------|
| 좌측 패널 | 노드 팔레트 (카테고리별 노드 검색/선택) |
| 중앙 캔버스 | 그래프 에디터 (노드 배치 및 연결) |
| 우측 패널 | 프로퍼티 에디터 (선택 노드 속성 편집) |
| 하단 패널 | 실행 출력 (처리 결과 이미지 확인) |

## 기술 스택

| 구분 | 기술 |
|------|------|
| **플랫폼** | .NET 8.0 / C# |
| **UI 프레임워크** | WPF (Windows Presentation Foundation) |
| **아키텍처** | MVVM (CommunityToolkit.Mvvm) |
| **노드 그래프** | Nodify 7.x |
| **영상처리** | OpenCvSharp4 (OpenCV 4.11) |
| **카메라 SDK** | HIK MvCameraControl.Net (동적 로딩) |
| **시리얼 통신** | System.IO.Ports |
| **직렬화** | System.Text.Json |

## 프로젝트 구조

```
MVXTester/
├── MVXTester.sln
└── src/
    ├── MVXTester.Core/           # 코어 프레임워크
    │   ├── Models/               # BaseNode, Port, NodeGraph, Connection
    │   ├── Engine/               # GraphExecutor, 코드 생성기
    │   ├── Registry/             # NodeRegistry, NodeCategory
    │   ├── Serialization/        # JSON 직렬화/역직렬화
    │   └── UndoRedo/             # Undo/Redo 매니저
    │
    ├── MVXTester.Nodes/          # 노드 구현체 (146개)
    │   ├── Input/                # 카메라, 이미지/비디오 입력
    │   ├── Output/               # 출력
    │   ├── Filter/               # 가우시안, 미디언, 양방향 필터 등
    │   ├── Edge/                 # Canny, Sobel, Laplacian
    │   ├── Threshold/            # 이진화, 적응형 이진화, Otsu
    │   ├── Morphology/           # 침식, 팽창, 모폴로지 연산
    │   ├── Contour/              # 외곽선 검출, 필터링, 모멘트
    │   ├── Detection/            # 템플릿 매칭, 허프 변환, Haar
    │   ├── Feature/              # ORB, AKAZE, SIFT, 블롭 검출
    │   ├── Color/                # 색공간 변환, 채널 분리/병합
    │   ├── Transform/            # 리사이즈, 회전, 어파인, 원근 변환
    │   ├── Drawing/              # 도형, 텍스트, 외곽선 그리기
    │   ├── Arithmetic/           # 사칙연산, 비트연산, 블렌딩
    │   ├── Histogram/            # 히스토그램 계산, 평활화, 비교
    │   ├── Segmentation/         # Watershed, GrabCut, KMeans
    │   ├── Inspection/           # 고수준 검사 (결함, 패턴, 정렬 등)
    │   ├── Measurement/          # 치수, 거리, 각도 측정
    │   ├── Value/                # 기본 타입 값 노드
    │   ├── Control/              # 조건분기, 반복, 비교
    │   ├── Communication/        # TCP, Serial 통신
    │   ├── Data/                 # CSV, 문자열 처리
    │   ├── Event/                # 이벤트 처리
    │   └── Script/               # Python 스크립트 실행
    │
    └── MVXTester.App/            # WPF 애플리케이션
        ├── ViewModels/           # MVVM ViewModel (9개)
        ├── Views/                # XAML View (5개)
        ├── Services/             # 클립보드 서비스
        ├── Converters/           # WPF 값 변환기
        ├── Themes/               # 다크 테마 (Catppuccin Mocha)
        └── UndoRedo/             # 앱 레벨 Undo 액션
```

## 노드 카테고리 (24개)

| 카테고리 | 노드 수 | 설명 |
|---------|---------|------|
| **Input** | 6 | HIK 카메라, USB 카메라, 이미지/비디오 읽기 |
| **Inspection** | 13 | 색상 객체 검출, 얼굴 인식, 결함 검사, 패턴 매칭 등 |
| **Measurement** | 3 | 치수 측정, 거리 측정, 각도 측정 |
| **Detection** | 9 | 템플릿 매칭, 허프 원/직선, 연결 성분 분석 |
| **Contour** | 13 | 외곽선 검출, 필터링, 모멘트, 타원/사각형 피팅 |
| **Feature** | 8 | ORB, AKAZE, SIFT, 블롭 검출, 특징점 매칭 |
| **Filter** | 10 | 가우시안, 양방향 필터, 디노이즈, 샤프닝, LUT |
| **Edge** | 4 | Canny, Sobel, Scharr, Laplacian |
| **Threshold** | 3 | 전역/적응형/Otsu 이진화 |
| **Morphology** | 3 | 침식, 팽창, 모폴로지 연산 |
| **Color** | 4 | 색공간 변환, 채널 분리/병합 |
| **Transform** | 8 | 리사이즈, 회전, 어파인, 원근, 피라미드 |
| **Drawing** | 10 | 도형, 텍스트, 외곽선, 바운딩 박스 그리기 |
| **Arithmetic** | 11 | 이미지 연산, 비트 연산, 마스크 적용, 블렌딩 |
| **Histogram** | 4 | 히스토그램 계산, 평활화, 비교, 역투영 |
| **Segmentation** | 3 | Watershed, GrabCut, KMeans |
| **Value** | 14 | Integer, Float, String, Point, Scalar 등 기본 타입 |
| **Control** | 6 | 조건분기(If), 반복(For), 비교, 스위치 |
| **Communication** | 3 | TCP 클라이언트/서버, 시리얼 포트 |
| **Data** | 7 | CSV 읽기/파싱, 문자열 처리 |
| **Event** | 3 | 이벤트 처리 |
| **Script** | 1 | Python 스크립트 실행 |

## 아키텍처

### 코어 프레임워크

```
BaseNode (추상 클래스)
├── Setup()          → 포트, 프로퍼티 정의
├── Process()        → 영상처리 로직 실행
├── Inputs/Outputs   → 타입 안전한 제네릭 포트
├── Properties       → 동적 속성 시스템 (NodeProperty)
├── Preview          → Mat? 미리보기 이미지
└── Error            → 에러 메시지 표시
```

### 그래프 실행 엔진

```
[단일 실행]
Execute() → TopologicalSort() (Kahn's Algorithm)
         → 각 노드 순차 실행 (Dirty 노드만)
         → 하류 노드 Dirty 전파

[스트리밍 실행]
ExecuteContinuous() → 목표 FPS 루프
                   → IStreamingSource 노드 Dirty 마킹
                   → TopologicalSort() → 순차 실행
                   → 프레임 단위 업데이트
```

### 데이터 흐름

```
OutputPort<Mat> ──→ InputPort<Mat>     타입 안전한 연결
OutputPort<int> ──→ InputPort<int>     사이클 검출로 DAG 보장
```

### MVVM 구조

```
MainViewModel
├── EditorViewModel       → 그래프 편집, 실행 제어
├── NodePaletteViewModel  → 노드 검색/선택, 아코디언 UI
├── PropertyEditorViewModel → 선택 노드 속성 편집
└── ExecuteOutputViewModel  → 실행 결과 표시
```

## 주요 기능 상세

### HIK GigE 카메라

- `MvCameraControl.Net.dll` 동적 로딩 (Assembly.LoadFrom + 리플렉션)
- 트리거 모드: Continuous, Software, Hardware
- Bayer 포맷 지원 (GR, RG, GB, BG)
- 노출 제어 (자동/수동), 게인, 감마 조정
- ROI(관심 영역) 설정

### 고수준 검사 노드 (Inspection)

단일 노드가 전체 비전 태스크를 수행하는 복합 노드:

- **ColorObjectDetector** - BGR→HSV→InRange→모폴로지→외곽선 필터링
- **ContourCenterFinder** - 이미지→이진화→외곽선→중심점 파이프라인
- **FaceDetector** - Haar Cascade 기반 얼굴 위치 검출
- **DefectDetector** - 기준 이미지 비교를 통한 결함 검출
- **PatternMatcher** - 다중 매칭 + NMS(비최대 억제) + PASS/FAIL 판정
- **BrightnessUniformity** - 그리드 기반 밝기 균일도 검사

### 코드 생성

그래프를 실행 가능한 코드로 변환:

- **Python** - `cv2`, `numpy` 기반 스크립트 생성
- **C#** - `OpenCvSharp` 기반 독립 실행 프로그램 생성
- 위상 정렬 순서로 코드 생성, 자동 import/using 관리

### 그래프 직렬화

- JSON 포맷 (`System.Text.Json`)
- 노드 위치, 속성 값, 연결 정보 저장/복원
- 파일 확장자: `.json`

## 키보드 단축키

| 단축키 | 기능 |
|--------|------|
| `Ctrl+N` | 새 그래프 |
| `Ctrl+O` | 그래프 열기 |
| `Ctrl+S` | 저장 |
| `Ctrl+Shift+S` | 다른 이름으로 저장 |
| `F5` | 실행 |
| `Ctrl+F5` | 강제 실행 |
| `F6` | 스트리밍 시작/중지 |
| `Escape` | 실행 취소 |
| `Ctrl+Z` | 되돌리기 |
| `Ctrl+Y` / `Ctrl+Shift+Z` | 다시 실행 |
| `Ctrl+C` / `Ctrl+X` / `Ctrl+V` | 복사 / 잘라내기 / 붙여넣기 |
| `Ctrl+D` | 복제 |
| `Ctrl+A` | 전체 선택 |
| `Delete` | 선택 노드 삭제 |

## 빌드 및 실행

### 요구 사항

- .NET 8.0 SDK
- Windows 10/11 (WPF)
- Visual Studio 2022 또는 `dotnet` CLI

### 빌드

```bash
dotnet build MVXTester.sln
```

### 실행

```bash
dotnet run --project src/MVXTester.App/MVXTester.App.csproj
```

### HIK 카메라 사용 시

HIK Vision의 MVS SDK를 설치하면 `MvCameraControl.Net.dll`이 자동으로 검색됩니다. SDK가 설치되지 않은 환경에서도 카메라 노드 외의 기능은 정상 작동합니다.

## 라이선스

This project is for internal use.
