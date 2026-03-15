## MVXTester 프로그램 구조

### 아키텍처 개요

MVXTester는 4개 계층으로 구성됩니다:

1. **UI Layer (WPF + Nodify)**: MainWindow, EditorView, PropertyEditorView, NodePaletteView, CodePreviewDialog
2. **ViewModel Layer (CommunityToolkit.Mvvm)**: MainViewModel, EditorViewModel, NodePaletteViewModel, PropertyEditorViewModel
3. **Core Engine (MVXTester.Core)**: BaseNode, NodeGraph, GraphExecutor, Ports, Connections, NodeProperty, GraphSerializer, SubGraphAnalyzer
4. **Node Library (MVXTester.Nodes)**: 160+ 노드, 27개 카테고리, MediaPipe, YOLO, OCR, LLM/VLM Vision API

### 프로젝트 구조

```
MVXTester/
├── src/
│   ├── MVXTester.Core/          코어 엔진 (UI 독립)
│   │   ├── Models/
│   │   │   ├── BaseNode.cs      모든 노드의 추상 기본 클래스
│   │   │   ├── INode.cs         인터페이스, NodeProperty, Port 타입
│   │   │   ├── NodeGraph.cs     그래프 컨테이너 (노드 + 연결)
│   │   │   └── FunctionNode.cs  서브그래프 함수 노드
│   │   ├── Engine/
│   │   │   └── GraphExecutor.cs 실행 엔진 (위상정렬, 루프)
│   │   ├── Serialization/
│   │   │   ├── GraphSerializer.cs  JSON 직렬화
│   │   │   ├── ProjectArchive.cs   ZIP 아카이브 (.mvxp)
│   │   │   └── SubGraphAnalyzer.cs 경계 포트 분석
│   │   └── Registry/
│   │       └── NodeRegistry.cs  동적 노드 타입 등록
│   │
│   ├── MVXTester.Nodes/         노드 구현체
│   │   ├── Arithmetic/          11 노드 (Add, Subtract, Bitwise...)
│   │   ├── Color/               4 노드 (CvtColor, InRange...)
│   │   ├── Contour/             13 노드 (FindContours, Area...)
│   │   ├── Control/             12 노드 (For, While, Switch, Delay...)
│   │   ├── Data/                7 노드 (CSV, String ops...)
│   │   ├── Detection/           9 노드 (Template, Hough...)
│   │   ├── Drawing/             10 노드 (Circle, Rect, Text...)
│   │   ├── Edge/                4 노드 (Canny, Sobel...)
│   │   ├── Feature/             8 노드 (SIFT, ORB, Harris...)
│   │   ├── Filter/              10 노드 (Gaussian, Median...)
│   │   ├── Histogram/           4 노드 (CLAHE, Equalize...)
│   │   ├── Input/               7 노드 (Camera, ImageRead...)
│   │   ├── Inspection/          13 노드 (Defect, Pattern...)
│   │   ├── Measurement/         3 노드 (Distance, Angle...)
│   │   ├── Morphology/          3 노드 (Erode, Dilate...)
│   │   ├── Script/              2 노드 (Python, C# Script)
│   │   ├── Segmentation/        3 노드 (Watershed, GrabCut...)
│   │   ├── Threshold/           3 노드 (Fixed, Adaptive, Otsu)
│   │   ├── Transform/           8 노드 (Resize, Rotate, Warp...)
│   │   ├── Value/               14 노드 (Int, String, Math...)
│   │   ├── MediaPipe/           6 노드 (Face, Hand, Pose, Mesh)
│   │   ├── YOLO/                1 노드 (YOLOv8 Detection)
│   │   ├── OCR/                 2 노드 (PaddleOCR, Tesseract)
│   │   └── AI/                  3 노드 LLM/VLM (GPT-4o, Gemini, Claude)
│   │
│   └── MVXTester.App/           WPF 애플리케이션
│       ├── ViewModels/          MVVM ViewModels
│       ├── Views/               XAML 뷰
│       ├── Themes/              다크/라이트 테마 리소스
│       └── Services/            테마, 코드 생성 서비스
```

### 핵심 개념

#### BaseNode
모든 노드는 BaseNode을 상속합니다. Setup()에서 포트와 속성을 정의하고, Process()에서 메인 로직을 실행합니다. 노드는 타입이 지정된 입출력 포트와 설정 가능한 속성을 가집니다.

#### NodeGraph
그래프 컨테이너로 노드와 연결을 관리합니다. 연결 규칙(타입 호환성, 순환 방지)을 강제하고, dirty 상태를 추적하며, 실행을 위한 스레드 안전 스냅샷을 제공합니다.

#### GraphExecutor
실행 엔진입니다. Kahn 알고리즘으로 위상 정렬을 수행합니다. 세 가지 모드를 지원합니다:
- **Single-shot**: 단일 실행
- **Continuous streaming**: 카메라 연속 스트리밍
- **Runtime**: 이벤트 기반, dirty 노드만 재실행 (증분 실행)

루프 실행(For, While, ForEach)과 중첩 루프를 지원합니다.

#### 데이터 흐름
Pull-based 데이터 흐름입니다. InputPort.GetValue()가 연결을 따라 소스 OutputPort의 값을 가져옵니다. 노드는 위상 정렬 순서로 실행됩니다. Runtime 모드에서는 dirty 노드만 재실행됩니다.

#### FunctionNode
저장된 프로젝트(.mvxp)를 재사용 가능한 서브그래프로 로드합니다. SubGraphAnalyzer가 경계 포트를 식별합니다: 소스 노드(in-degree 0)는 입력, 싱크 노드(out-degree 0)는 출력이 됩니다. 내부적으로 자체 GraphExecutor로 서브그래프를 실행합니다.

### 노드 팔레트 카테고리별 노드 목록

노드 팔레트는 27개 카테고리로 구성됩니다:
- 입출력 (Input/Output): Image Read, Image Write, Video Read, Camera, Image Show
- 값 (Value): Integer, Float, Double, String, Bool, Point, Size, Scalar, Rect, Math Operation, Comparison, Logic Gate 등
- 색상 (Color): Convert Color, In Range, Split Channels, Merge Channels
- 필터 (Filter): Gaussian Blur, Median Blur, Bilateral Filter, Sharpen, Inpaint 등 10개
- 에지 검출 (Edge Detection): Canny Edge, Sobel Edge, Laplacian Edge, Scharr Edge
- 모폴로지 (Morphology): Erode, Dilate, Morphology Ex
- 임계값 (Threshold): Threshold, Adaptive Threshold, Otsu Threshold
- 윤곽선 (Contour): Find Contours, Draw Contours, Contour Area, Bounding Rect 등 13개
- 특징점 (Feature Detection): FAST, Harris Corner, ORB, SIFT 등 8개
- 그리기 (Drawing): Draw Line, Circle, Rectangle, Text 등 10개
- 변환 (Transform): Resize, Rotate, Crop, Flip, Warp Affine/Perspective 등 8개
- 히스토그램 (Histogram): Calc Histogram, Equalize, CLAHE 등 4개
- 연산 (Arithmetic): Add, Subtract, Bitwise AND/OR/NOT, Mask Apply 등 11개
- 검출 (Detection): Hough Lines/Circles, Template Match, Connected Components 등 9개
- 분할 (Segmentation): Flood Fill, GrabCut, Watershed
- 제어 (Control): Boolean, Compare, If Select, For, While, Delay 등 12개
- 데이터 처리 (Data Processing): String to Number, CSV Reader, String Split 등 7개
- 통신 (Communication): Serial Port, TCP Server, TCP Client
- 이벤트 (Event): Keyboard Event, Mouse Event, Mouse ROI, WaitKey
- 스크립트 (Script): Python Script, C# Script
- 검사 (Inspection): Alignment Checker, Defect Detector, Pattern Matcher 등 13개
- 측정 (Measurement): Angle Measure, Distance Measure, Object Measure
- MediaPipe: Face Detection, Hand Landmark, Pose Landmark 등 6개
- YOLO: YOLOv8 Detection
- OCR: PaddleOCR, Tesseract OCR
- LLM/VLM: Claude Vision, Gemini Vision, OpenAI Vision
- 함수 (Function): Import된 재사용 가능한 서브그래프 노드

#### 입출력 (Input/Output)
- Image Read(이미지 읽기): 이미지 파일을 불러옵니다
- Image Write(이미지 저장): 이미지를 파일로 저장합니다
- Video Read(비디오 읽기): 비디오 파일을 프레임 단위로 읽습니다
- Camera(카메라): USB/산업용 카메라에서 실시간 이미지를 캡처합니다
- Image Show(이미지 표시): 처리 결과 이미지를 화면에 표시합니다

#### 값 (Value)
- Integer(정수): 정수 값을 생성합니다
- Float(실수): 단정밀도 실수 값을 생성합니다
- Double(배정밀도): 배정밀도 실수 값을 생성합니다
- String(문자열): 문자열 값을 생성합니다
- Bool(논리값): true/false 값을 생성합니다
- Point(좌표점): X,Y 좌표 값을 생성합니다
- Size(크기): Width,Height 크기 값을 생성합니다
- Scalar(스칼라): 색상/다채널 값을 생성합니다 (B,G,R,A)
- Rect(사각영역): X,Y,Width,Height 사각형 영역을 정의합니다
- Math Operation(수학 연산): 사칙연산, 거듭제곱, 제곱근 등 수학 연산을 수행합니다
- Comparison(비교): 두 값을 비교합니다 (같음, 크다, 작다 등)
- Logic Gate(논리 게이트): AND, OR, NOT 등 논리 연산을 수행합니다
- List Create(리스트 생성): 여러 값을 리스트로 묶습니다
- String Format(문자열 서식): 형식 문자열로 텍스트를 조합합니다
- Print(출력): 값을 콘솔/로그에 출력합니다

#### 색상 (Color)
- Convert Color(색변환): 색 공간을 변환합니다 (BGR↔Gray, BGR↔HSV 등)
- In Range(범위 필터): 지정 색상 범위에 해당하는 픽셀을 추출합니다
- Split Channels(채널 분리): 이미지를 B,G,R 개별 채널로 분리합니다
- Merge Channels(채널 합치기): 개별 채널을 하나의 컬러 이미지로 합칩니다

#### 필터 (Filter)
- Gaussian Blur(가우시안 블러): 가우시안 커널로 이미지를 부드럽게 합니다
- Median Blur(미디언 블러): 중간값 필터로 점 노이즈를 제거합니다
- Bilateral Filter(양방향 필터): 에지를 보존하면서 노이즈를 제거합니다
- Box Filter(박스 필터): 균일 커널로 이미지를 평균화합니다
- Sharpen(선명화): 이미지를 선명하게 만듭니다
- Filter 2D(커스텀 필터): 사용자 정의 커널로 필터링합니다
- Non-Local Means Denoise(비지역 평균 디노이즈): 고급 노이즈 제거 알고리즘을 적용합니다
- Inpaint(인페인트): 손상/마스크 영역을 주변 정보로 복원합니다
- Normalize(정규화): 픽셀 값 범위를 정규화합니다
- LUT(룩업 테이블): 룩업 테이블로 픽셀 값을 매핑합니다

#### 에지 검출 (Edge Detection)
- Canny Edge(캐니 에지): Canny 알고리즘으로 에지를 검출합니다
- Sobel Edge(소벨 에지): Sobel 미분으로 에지를 검출합니다
- Laplacian Edge(라플라시안 에지): 라플라시안 연산으로 에지를 검출합니다
- Scharr Edge(샤르 에지): Scharr 연산으로 에지를 검출합니다

#### 모폴로지 (Morphology)
- Erode(침식): 밝은 영역을 축소하여 노이즈를 제거합니다
- Dilate(팽창): 밝은 영역을 확장하여 빈 공간을 채웁니다
- Morphology Ex(모폴로지 확장): Open, Close, Gradient 등 복합 모폴로지 연산을 수행합니다

#### 임계값 (Threshold)
- Threshold(임계값): 고정 임계값으로 이미지를 이진화합니다
- Adaptive Threshold(적응형 임계값): 영역별 다른 임계값으로 이진화합니다
- Otsu Threshold(오츠 임계값): 자동으로 최적 임계값을 찾아 이진화합니다

#### 윤곽선 (Contour)
- Find Contours(윤곽선 찾기): 이진 이미지에서 객체의 윤곽선을 추출합니다
- Draw Contours(윤곽선 그리기): 윤곽선을 이미지에 그립니다
- Contour Area(윤곽선 면적): 윤곽선의 면적을 계산합니다
- Contour Centers(윤곽선 중심): 윤곽선의 중심점을 계산합니다
- Contour Filter(윤곽선 필터): 면적/크기 조건으로 윤곽선을 필터링합니다
- Bounding Rect(바운딩 박스): 윤곽선을 감싸는 최소 사각형을 계산합니다
- Approx Poly(다각형 근사): 윤곽선을 다각형으로 근사합니다
- Convex Hull(볼록 껍질): 윤곽선의 볼록 껍질을 계산합니다
- Min Enclosing Circle(최소 외접원): 윤곽선을 감싸는 최소 원을 계산합니다
- Fit Ellipse(타원 피팅): 윤곽선에 타원을 피팅합니다
- Min Area Rect(최소 면적 사각형): 윤곽선의 최소 면적 회전 사각형을 계산합니다
- Moments(모멘트): 윤곽선의 모멘트를 계산합니다
- Match Shapes(형상 매칭): 두 윤곽선의 형상 유사도를 비교합니다

#### 특징점 (Feature Detection)
- FAST Features(FAST 특징점): FAST 알고리즘으로 코너를 빠르게 검출합니다
- Good Features To Track(추적 특징점): 추적에 적합한 특징점을 검출합니다
- Harris Corner(해리스 코너): Harris 코너 검출을 수행합니다
- ORB Features(ORB 특징점): ORB 특징점을 추출하고 기술합니다
- SIFT Features(SIFT 특징점): SIFT 특징점을 추출하고 기술합니다
- Shi-Tomasi Corners(시-토마시 코너): Shi-Tomasi 코너를 검출합니다
- Simple Blob Detector(블롭 검출기): 크기/모양 기준으로 블롭을 검출합니다
- Match Features(특징점 매칭): 두 이미지의 특징점을 매칭합니다

#### 그리기 (Drawing)
- Draw Line(선 그리기): 이미지에 직선을 그립니다
- Draw Circle(원 그리기): 이미지에 원을 그립니다
- Draw Rectangle(사각형 그리기): 이미지에 사각형을 그립니다
- Draw Ellipse(타원 그리기): 이미지에 타원을 그립니다
- Draw Text(텍스트 그리기): 이미지에 텍스트를 표시합니다
- Draw Grid(그리드 그리기): 이미지에 격자를 그립니다
- Draw Crosshair(십자선 그리기): 이미지에 십자선을 그립니다
- Draw Polylines(다각선 그리기): 이미지에 다각형/폴리라인을 그립니다
- Draw Bounding Boxes(바운딩박스 그리기): 검출 결과를 바운딩 박스로 표시합니다
- Draw Contours Info(윤곽선 정보 표시): 윤곽선에 면적, 중심점 등 정보를 표시합니다

#### 변환 (Transform)
- Resize(크기 변환): 이미지 크기를 변경합니다
- Rotate(회전): 이미지를 원하는 각도로 회전합니다
- Crop(자르기/크롭): 이미지의 특정 영역을 잘라냅니다 (ROI 추출)
- Flip(뒤집기): 이미지를 상하/좌우로 뒤집습니다
- Warp Affine(아핀 변환): 아핀 변환을 적용합니다 (이동, 회전, 크기, 기울기)
- Warp Perspective(원근 변환): 원근 변환을 적용합니다 (사다리꼴→사각형)
- Pyramid(피라미드): 이미지 피라미드를 생성합니다 (확대/축소)
- Distance Transform(거리 변환): 최근접 배경까지의 거리를 계산합니다

#### 히스토그램 (Histogram)
- Calc Histogram(히스토그램 계산): 이미지의 색상 분포를 계산합니다
- Equalize Histogram(히스토그램 균등화): 대비를 개선합니다
- CLAHE(적응형 히스토그램 균등화): 영역별로 대비를 개선합니다
- Calc Back Project(역투영 계산): 색상 히스토그램 기반으로 영역을 추적합니다

#### 연산 (Arithmetic)
- Add(덧셈): 두 이미지를 더합니다
- Subtract(뺄셈): 두 이미지를 뺍니다
- Multiply(곱셈): 두 이미지를 곱합니다
- Abs Diff(절대 차이): 두 이미지의 절대 차이를 계산합니다
- Bitwise AND(비트 AND): 비트 AND 연산을 수행합니다
- Bitwise OR(비트 OR): 비트 OR 연산을 수행합니다
- Bitwise XOR(비트 XOR): 비트 XOR 연산을 수행합니다
- Bitwise NOT(비트 NOT): 비트 반전을 수행합니다
- Weighted Add(가중 합): 가중치를 적용하여 두 이미지를 합성합니다
- Mask Apply(마스크 적용): 마스크를 이미지에 적용합니다
- Image Blend(이미지 블렌드): 두 이미지를 알파 블렌딩합니다

#### 검출 (Detection)
- Hough Lines(허프 직선): 직선을 검출합니다
- Hough Circles(허프 원): 원을 검출합니다
- Template Match(템플릿 매칭): 템플릿 이미지와 일치하는 위치를 찾습니다
- Template Match Multi(다중 템플릿 매칭): 여러 개의 매칭 위치를 모두 찾습니다
- Haar Cascade(하르 캐스케이드): 사전 학습된 분류기로 객체를 검출합니다
- Min Max Loc(최소최대 위치): 이미지에서 최소/최대 밝기 위치를 찾습니다
- Pixel Count(픽셀 카운트): 특정 조건의 픽셀 수를 셉니다
- Line Profile(라인 프로파일): 선을 따라 픽셀 값 프로파일을 추출합니다
- Connected Components(연결 성분): 연결된 영역을 라벨링합니다

#### 분할 (Segmentation)
- Flood Fill(플러드 필): 연결된 동일 색상 영역을 채웁니다
- GrabCut(그랩컷): 전경/배경을 자동 분리합니다
- Watershed(워터셰드): 마커 기반 영역 분할을 수행합니다

#### 제어 (Control)
- Boolean(불린): true/false 값을 생성합니다
- Compare(비교): 두 값을 비교하여 true/false를 출력합니다
- Boolean Logic(불린 논리): AND, OR, NOT 논리 연산을 수행합니다
- If Select(조건 선택): 조건에 따라 두 입력 중 하나를 선택합니다
- Switch(스위치): 여러 조건 분기를 처리합니다
- For Loop(For 루프): 지정 횟수만큼 반복 실행합니다
- For(간단 반복): Start~End 범위를 Step 간격으로 반복합니다
- ForEach(각각 반복): 리스트의 각 항목에 대해 반복합니다
- While(조건 반복): 조건이 참인 동안 반복합니다
- BreakIf(중단 조건): 조건이 참이면 루프를 중단합니다
- Collect(수집): 루프 결과를 리스트로 수집합니다
- Delay(지연): 지정 시간만큼 대기합니다

#### 데이터 처리 (Data Processing)
- String to Number(문자→숫자): 문자열을 숫자로 변환합니다
- Number to String(숫자→문자): 숫자를 문자열로 변환합니다
- CSV Reader(CSV 읽기): CSV 파일을 읽어옵니다
- CSV Parser(CSV 파싱): CSV 텍스트를 파싱합니다
- String Split(문자열 분할): 구분자로 문자열을 나눕니다
- String Join(문자열 결합): 여러 문자열을 하나로 결합합니다
- String Replace(문자열 치환): 문자열의 일부를 치환합니다

#### 통신 (Communication)
- Serial Port(시리얼 포트): RS232/RS485 시리얼 통신을 수행합니다
- TCP Server(TCP 서버): TCP 서버로 데이터를 수신합니다
- TCP Client(TCP 클라이언트): TCP 클라이언트로 데이터를 전송합니다

#### 이벤트 (Event)
- Keyboard Event(키보드 이벤트): 키보드 입력을 감지합니다
- Mouse Event(마우스 이벤트): 마우스 클릭/이동을 감지합니다
- Mouse ROI(마우스 ROI): 마우스로 관심 영역을 지정합니다
- WaitKey(키 대기): 키 입력을 대기합니다

#### 스크립트 (Script)
- Python Script(파이썬 스크립트): 파이썬 코드를 실행합니다
- C# Script(C# 스크립트): C# 코드를 실행합니다

#### 검사 (Inspection)
- Alignment Checker(정렬 검사): 객체의 정렬 상태를 검사합니다
- Brightness Uniformity(밝기 균일도): 밝기 균일도를 측정합니다
- Circle Detector(원형 검출): 원형 객체를 검출합니다
- Color Object Detector(색상 객체 검출): 특정 색상의 객체를 검출합니다
- Contour Center Finder(윤곽 중심 찾기): 윤곽선의 중심점을 찾습니다
- Defect Detector(결함 검출): 기준 이미지와 비교하여 결함을 검출합니다
- Edge Inspector(에지 검사): 에지의 품질/위치를 검사합니다
- Face Detector(얼굴 검출): 얼굴을 검출합니다
- Object Counter(객체 카운터): 객체의 개수를 셉니다
- Pattern Matcher(패턴 매칭): 패턴을 찾아 위치/각도를 반환합니다
- Presence Checker(존재 확인): 특정 영역에 객체가 있는지 확인합니다
- Scratch Detector(스크래치 검출): 표면의 스크래치를 검출합니다
- Shape Classifier(형상 분류): 형상을 분류합니다 (원, 삼각형, 사각형 등)

#### 측정 (Measurement)
- Angle Measure(각도 측정): 두 선 사이의 각도를 측정합니다
- Distance Measure(거리 측정): 두 점 사이의 거리를 측정합니다
- Object Measure(객체 측정): 객체의 폭, 높이, 면적을 측정합니다

#### MediaPipe
- MP Face Detection(얼굴 검출): MediaPipe BlazeFace로 얼굴을 검출합니다
- MP Face Mesh(얼굴 메시): 468개 얼굴 랜드마크를 추출합니다
- MP Hand Landmark(손 랜드마크): 손의 21개 관절점을 추적합니다
- MP Pose Landmark(포즈 추정): 33개 신체 키포인트를 추출합니다
- MP Object Detection(객체 검출): SSD MobileNet으로 COCO 80클래스를 검출합니다
- MP Selfie Segmentation(배경 분리): 인물과 배경을 분리합니다

#### YOLO
- YOLOv8 Detection(YOLOv8 검출): YOLOv8 모델로 객체를 검출합니다

#### OCR
- PaddleOCR(PaddleOCR): PaddleOCR로 텍스트를 인식합니다 (한/중/영)
- Tesseract OCR(Tesseract OCR): Tesseract로 텍스트를 인식합니다

#### LLM/VLM
- Claude Vision(Claude 비전): Claude API로 이미지를 분석합니다
- Gemini Vision(Gemini 비전): Gemini API로 이미지를 분석합니다
- OpenAI Vision(OpenAI 비전): GPT-4o API로 이미지를 분석합니다

#### 함수 (Function)
- Function(함수): 저장된 프로젝트를 재사용 가능한 서브그래프로 호출합니다
