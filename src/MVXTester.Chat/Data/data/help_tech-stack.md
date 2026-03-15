## MVXTester 기술 스택

### 기술 스택 요약

MVXTester에 사용된 주요 기술:
- 런타임: .NET 8.0, C# 12, WPF (Windows)
- 컴퓨터 비전: OpenCvSharp 4 (OpenCV C# 래퍼)
- 노드 에디터: Nodify (WPF 노드 그래프 라이브러리)
- MVVM: CommunityToolkit.Mvvm
- 카메라: USB 웹캠 + HIKROBOT MVS SDK + Cognex GigE Vision SDK
- 실행 엔진: 위상 정렬(Kahn 알고리즘) + dirty 플래그 기반 최적화
- 코드 생성: Python(OpenCV) + C#(OpenCvSharp) 자동 변환
- AI 추론: ONNX Runtime (MediaPipe, YOLOv8)
- OCR: PaddleOCR + Tesseract 5
- LLM/VLM: OpenAI, Claude, Gemini Vision API

### 런타임

- **.NET 8.0**: 모던 크로스 플랫폼 런타임 (WPF UI는 Windows 전용)
- **C# 12**: 최신 언어 기능 (primary constructors, collection expressions)
- **WPF**: Windows Presentation Foundation 데스크톱 UI
- **MVVM**: CommunityToolkit.Mvvm 8.x를 통한 Model-View-ViewModel 패턴

### 컴퓨터 비전

- **OpenCvSharp4 (4.10.x)**: OpenCV의 .NET 래퍼
  - 핵심 이미지 타입: Mat, Point, Rect, Scalar, Size
  - 전체 OpenCV API: 필터, 엣지 검출, 컨투어, 특징점 검출
  - 이미지 I/O: imread/imwrite, VideoCapture
  - 산업용 카메라를 위한 Bayer 패턴 디모자이킹

### 노드 에디터

- **Nodify 7.x**: WPF 노드 에디터 라이브러리
  - NodifyEditor: 팬/줌 기능의 메인 캔버스
  - Node, Connector: 드래그 가능한 노드 UI 요소
  - PendingConnection: 드래그 중 와이어 시각화
  - 미니맵, 그리드 배경, 그룹핑 지원

### MVVM 프레임워크

- **CommunityToolkit.Mvvm 8.x**
  - [ObservableProperty]: INotifyPropertyChanged 자동 생성
  - [RelayCommand]: ICommand 구현 자동 생성
  - ObservableObject: ViewModel 기본 클래스
  - 소스 제너레이터로 보일러플레이트 제거

### 직렬화

- **System.Text.Json**: 고성능 JSON 직렬화
  - camelCase 네이밍 정책
  - JsonElement으로 동적 속성 역직렬화
  - .mvx (일반 JSON)과 .mvxp (ZIP 아카이브) 형식 지원

- **System.IO.Compression**: .mvxp 프로젝트 파일용 ZIP 아카이브 지원
  - 그래프 JSON + 참조 이미지/비디오 에셋 번들

### 통합 카메라 노드

- **Camera 노드**: 모든 카메라 타입을 하나의 노드로 통합
  - USB, HIK, Cognex 카메라 자동 감지
  - 통합 디바이스 목록에서 선택
  - 카메라 타입별 속성 자동 조정:
    - USB: Width, Height, FPS, Backend (DirectShow/MSMF)
    - HIK: TriggerMode, ExposureTime, Gain, Gamma, PixelFormat 등
    - Cognex: TriggerMode, ExposureTime, Brightness, PixelFormat

- **HIKROBOT MVS SDK**: MvCameraControl.Net.dll
  - GigE Vision 및 USB3 Vision 산업용 카메라
  - 리플렉션을 통한 동적 어셈블리 로딩 (컴파일 타임 의존성 없음)

- **Cognex VisionPro 9.x SDK**: Cognex.Vision.*.dll
  - GigE Vision 카메라 (CIC 시리즈)
  - 2단계 SDK 로딩: 열거를 위한 어셈블리만 → 취득을 위한 전체 초기화

### 실행 엔진

- **GraphExecutor**: 커스텀 실행 엔진
  - Kahn 알고리즘으로 위상 정렬
  - Dirty 플래그 증분 실행
  - 루프 실행: For, While, ForEach (break 지원)
  - 스레드 안전 그래프 스냅샷
  - 세 가지 모드: single-shot, continuous streaming, event-driven runtime

### 코드 생성

- **Python Code Generator**: 그래프를 독립 Python/OpenCV 스크립트로 변환
- **C# Code Generator**: 그래프를 독립 C#/OpenCvSharp 코드로 변환
  - 두 생성기 모두 위상 정렬 순서를 따름
  - 노드 이름 기반 변수 명명

### ONNX Runtime

- **Microsoft.ML.OnnxRuntime 1.20.x**
  - MediaPipe 얼굴/손/포즈/메시/세그멘테이션 모델
  - YOLOv8 객체 검출 모델 (nano~xlarge)
  - PaddleOCR 검출 + 인식 모델
  - 비ASCII 경로 안전을 위한 바이트 로딩 InferenceSession
  - CPU 추론 (GPU 선택 사항: CUDA/DirectML provider)

### OCR 엔진

- **PaddleOCR (ONNX)**: PP-OCRv5 검출 + CTC 인식
  - 다국어: 한국어, 중국어, 일본어, 영어
  - 2단계: 텍스트 검출(DB) + 텍스트 인식(CTC decode)
  - 모델: ppocr_det.onnx, ppocr_rec.onnx, ppocr_keys.txt

- **Tesseract 5.2.x**: NuGet 패키지를 통한 클래식 OCR 엔진
  - 100+ 언어 지원 (traineddata 파일)
  - 페이지 세그멘테이션 모드, 신뢰도 필터링
  - 단어/라인/블록 수준 검출

### LLM/VLM Vision API

- **OpenAI GPT-4o**: Chat Completions API (vision)
- **Google Gemini**: GenerateContent API (inline_data)
- **Anthropic Claude**: Messages API (base64 image)
  - 모두 HttpClient REST API 사용 (SDK 의존성 없음)
  - 이미지: Mat → PNG → base64 → API
  - Models/API/api_config.json에서 API 키 자동 로드
  - 60초 타임아웃, 응답을 출력 이미지에 오버레이

### 빌드 시스템

- **MSBuild / dotnet CLI**
  - 멀티 프로젝트 솔루션: Core, Nodes, App
  - Core와 Nodes는 클래스 라이브러리 (UI 독립)
  - App은 WPF 실행 파일
  - NuGet 패키지 관리
