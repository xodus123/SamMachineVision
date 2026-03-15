namespace MVXTester.Chat.ViewModels;

/// <summary>
/// 노드 한국어 설명 및 카테고리 이름 데이터.
/// ChatbotViewModel에서 분리된 정적 데이터.
/// </summary>
internal static class NodeDescriptions
{
    public static Dictionary<string, (string Desc, string Apps)> GetKoreanDescriptions()
    {
        return new Dictionary<string, (string Desc, string Apps)>
        {
            // Input/Output
            ["Image Read"] = ("파일 경로에서 이미지를 읽어 Mat 형식으로 출력합니다.", "이미지 파일 기반 검사, 오프라인 이미지 분석"),
            ["Image Write"] = ("입력받은 이미지를 지정된 경로에 파일로 저장합니다.", "검사 결과 이미지 저장"),
            ["Video Read"] = ("비디오 파일을 프레임 단위로 읽습니다.", "비디오 파일 분석, 알고리즘 테스트"),
            ["Camera"] = ("USB, HIKROBOT, Cognex GigE 카메라를 통합 지원하는 카메라 노드입니다.", "실시간 라인 검사, 산업용 비전 시스템"),
            ["Image Show"] = ("입력 이미지를 OpenCV 윈도우에 표시합니다.", "실시간 결과 확인, 디버깅용 이미지 표시"),
            // Value
            ["Integer"] = ("정수 상수값을 출력합니다.", "좌표값 설정, 카운터 초기값"),
            ["Float"] = ("실수(float) 상수값을 출력합니다.", "가중치 설정, 스케일 팩터"),
            ["Double"] = ("배정밀도 실수(double) 상수값을 출력합니다.", "고정밀 연산, 측정값 기준"),
            ["String"] = ("문자열 상수값을 출력합니다.", "파일 경로 지정, 라벨 텍스트"),
            ["Bool"] = ("불리언(true/false) 상수값을 출력합니다.", "조건 플래그, 기능 토글"),
            ["Point"] = ("2D 좌표(X, Y)를 출력합니다.", "ROI 좌표 설정, 그리기 위치"),
            ["Size"] = ("크기(Width, Height)를 출력합니다.", "이미지 리사이즈 크기, 커널 크기"),
            ["Scalar"] = ("4개 컴포넌트의 스칼라값을 출력합니다.", "색상값 지정, 그리기 색상 설정"),
            ["Rect"] = ("사각형(X, Y, Width, Height)을 출력합니다.", "ROI 영역 설정, 크롭 범위"),
            ["Math Operation"] = ("두 숫자에 대해 산술 연산을 수행합니다.", "치수 계산, 스케일 변환"),
            ["Comparison"] = ("두 숫자를 비교하여 불리언 결과를 출력합니다.", "임계값 판정, 크기 비교"),
            ["Logic Gate"] = ("두 불리언 값에 대한 논리 연산을 수행합니다.", "다중 조건 결합, 복합 검사 로직"),
            ["List Create"] = ("여러 입력값을 하나의 리스트로 합칩니다.", "배치 처리용 데이터 수집"),
            ["String Format"] = ("서식 문자열에 입력 인자를 대입하여 결과 문자열을 생성합니다.", "검사 결과 메시지 생성"),
            ["Print"] = ("입력값을 텍스트로 포맷하여 노드 미리보기에 표시합니다.", "디버깅, 중간 결과 확인"),
            // Color
            ["Convert Color"] = ("이미지의 색상 공간을 변환합니다.", "그레이스케일 변환, HSV 색상 분석"),
            ["In Range"] = ("지정 범위 내의 픽셀만 추출하여 이진 마스크를 생성합니다.", "색상 기반 객체 검출"),
            ["Split Channels"] = ("다채널 이미지를 개별 채널로 분리합니다.", "채널별 분석"),
            ["Merge Channels"] = ("개별 채널 이미지를 하나의 다채널 이미지로 합칩니다.", "채널별 처리 후 결합"),
            // Filter
            ["Gaussian Blur"] = ("가우시안 블러를 적용하여 이미지를 부드럽게 만듭니다.", "전처리 노이즈 제거"),
            ["Median Blur"] = ("중앙값 블러를 적용합니다. 소금-후추 노이즈 제거에 효과적입니다.", "임펄스 노이즈 제거"),
            ["Bilateral Filter"] = ("에지를 보존하면서 노이즈를 제거하는 양방향 필터입니다.", "얼굴 피부 보정"),
            ["Box Filter"] = ("박스(평균) 필터를 적용합니다.", "빠른 스무딩, 이미지 평균화"),
            ["Sharpen"] = ("언샤프 마스킹으로 이미지를 선명하게 만듭니다.", "흐릿한 이미지 개선"),
            ["Filter 2D"] = ("사용자 정의 컨볼루션 커널을 적용합니다.", "커스텀 에지 검출, 엠보싱"),
            ["Non-Local Means Denoise"] = ("비-로컬 평균 알고리즘으로 고급 노이즈를 제거합니다.", "고품질 노이즈 제거"),
            ["Inpaint"] = ("마스크로 지정된 손상 영역을 주변 정보 기반으로 복원합니다.", "이미지 복원"),
            ["Normalize"] = ("이미지 강도 범위를 정규화합니다.", "콘트라스트 향상"),
            ["LUT"] = ("룩업 테이블로 감마, 밝기, 대비를 조정합니다.", "밝기/대비 조정"),
            // Edge
            ["Canny Edge"] = ("캐니 에지 검출을 수행합니다.", "윤곽 검출, 물체 경계 추출"),
            ["Sobel Edge"] = ("소벨 에지 검출을 수행합니다.", "방향별 에지 분석"),
            ["Laplacian Edge"] = ("라플라시안 에지 검출을 수행합니다.", "전방위 에지 검출"),
            ["Scharr Edge"] = ("샤르 에지 검출을 수행합니다.", "정밀 에지 검출"),
            // Morphology
            ["Erode"] = ("침식(Erosion) 연산을 적용합니다.", "노이즈 제거, 객체 분리"),
            ["Dilate"] = ("팽창(Dilation) 연산을 적용합니다.", "객체 연결, 구멍 메우기"),
            ["Morphology Ex"] = ("고급 모폴로지 연산을 수행합니다.", "배경 추출, 조명 보정"),
            // Threshold
            ["Threshold"] = ("고정 임계값으로 이미지를 이진화합니다.", "객체 분리, 배경 제거"),
            ["Adaptive Threshold"] = ("적응형 임계값을 적용합니다.", "조명 변화가 있는 환경의 이진화"),
            ["Otsu Threshold"] = ("오쓰 알고리즘으로 최적 임계값을 자동 결정합니다.", "자동 임계값 결정"),
            // Contour
            ["Find Contours"] = ("이진 이미지에서 윤곽선을 검출합니다.", "객체 검출, 형상 분석"),
            ["Draw Contours"] = ("검출된 윤곽선을 이미지 위에 그립니다.", "윤곽선 시각화"),
            ["Contour Area"] = ("각 윤곽선의 면적을 계산합니다.", "면적 기반 필터링"),
            ["Contour Centers"] = ("모멘트로 각 윤곽선의 중심점을 찾습니다.", "객체 위치 추적"),
            ["Contour Filter"] = ("면적과 둘레 범위로 윤곽선을 필터링합니다.", "노이즈 윤곽 제거"),
            ["Bounding Rect"] = ("각 윤곽선의 바운딩 박스를 계산합니다.", "객체 위치/크기 추출"),
            ["Approx Poly"] = ("윤곽선을 다각형으로 근사화합니다.", "형상 단순화"),
            ["Convex Hull"] = ("각 윤곽선의 볼록 껍질을 계산합니다.", "볼록성 분석"),
            ["Min Enclosing Circle"] = ("각 윤곽선을 감싸는 최소 원을 계산합니다.", "원형 객체 측정"),
            ["Fit Ellipse"] = ("각 윤곽선에 최적 타원을 적합합니다.", "타원형 객체 분석"),
            ["Min Area Rect"] = ("각 윤곽선의 최소 면적 회전 사각형을 계산합니다.", "회전된 객체 크기/각도 측정"),
            ["Moments"] = ("각 윤곽선의 이미지 모멘트를 계산합니다.", "무게중심 계산"),
            ["Match Shapes"] = ("형상 매칭으로 윤곽선간 유사도를 비교합니다.", "형상 기반 분류"),
            // Feature
            ["FAST Features"] = ("FAST 코너 검출을 수행합니다.", "실시간 특징점 검출"),
            ["Good Features To Track"] = ("Shi-Tomasi 코너 검출을 수행합니다.", "광학 흐름 추적용 특징점"),
            ["Harris Corner"] = ("해리스 코너 검출을 수행합니다.", "코너 검출, 특징점 기반 정렬"),
            ["ORB Features"] = ("ORB 특징점을 검출하고 기술자를 생성합니다.", "실시간 특징 매칭"),
            ["SIFT Features"] = ("SIFT 특징점을 검출합니다.", "파노라마 스티칭, 정밀 객체 인식"),
            ["Shi-Tomasi Corners"] = ("Shi-Tomasi 방법으로 코너를 검출합니다.", "코너 기반 추적"),
            ["Simple Blob Detector"] = ("SimpleBlobDetector로 블롭을 검출합니다.", "점/구멍 검출"),
            ["Match Features"] = ("두 이미지의 특징 기술자를 매칭합니다.", "이미지 유사도 비교"),
            // Drawing
            ["Draw Line"] = ("이미지 위에 직선을 그립니다.", "측정선 표시"),
            ["Draw Circle"] = ("이미지 위에 원을 그립니다.", "검출 위치 마킹"),
            ["Draw Rectangle"] = ("이미지 위에 사각형을 그립니다.", "ROI 영역 표시"),
            ["Draw Ellipse"] = ("이미지 위에 회전 가능한 타원을 그립니다.", "타원형 검출 결과 표시"),
            ["Draw Text"] = ("이미지 위에 텍스트를 그립니다.", "검사 결과 라벨링"),
            ["Draw Grid"] = ("이미지 위에 격자 오버레이를 그립니다.", "정렬 검사"),
            ["Draw Crosshair"] = ("이미지 위에 십자선을 그립니다.", "중심점 마킹"),
            ["Draw Polylines"] = ("이미지 위에 다각형 선을 그립니다.", "윤곽선 시각화"),
            ["Draw Bounding Boxes"] = ("Rect 배열로부터 바운딩 박스를 그립니다.", "객체 검출 결과 시각화"),
            ["Draw Contours Info"] = ("윤곽선에 중심점, 면적 등 정보 라벨을 함께 그립니다.", "윤곽선 분석 결과 시각화"),
            // Transform
            ["Resize"] = ("이미지 크기를 변경합니다.", "입력 이미지 크기 통일"),
            ["Rotate"] = ("이미지를 지정 각도로 회전합니다.", "기울기 보정"),
            ["Crop"] = ("이미지에서 지정된 영역을 잘라냅니다.", "관심 영역 추출"),
            ["Flip"] = ("이미지를 수평/수직으로 뒤집습니다.", "미러링 보정"),
            ["Warp Affine"] = ("3쌍의 대응점으로 아핀 변환을 적용합니다.", "이미지 정렬"),
            ["Warp Perspective"] = ("4쌍의 대응점으로 원근 변환을 적용합니다.", "문서 스캔 보정"),
            ["Pyramid"] = ("이미지 피라미드 연산을 수행합니다.", "다중 해상도 분석"),
            ["Distance Transform"] = ("이진 이미지의 거리 변환을 수행합니다.", "워터셰드 마커 생성"),
            // Histogram
            ["Calc Histogram"] = ("이미지의 히스토그램을 계산합니다.", "밝기 분포 분석"),
            ["Equalize Histogram"] = ("히스토그램 평활화로 콘트라스트를 개선합니다.", "콘트라스트 향상"),
            ["CLAHE"] = ("제한된 대비 적응형 히스토그램 평활화를 적용합니다.", "조명 불균일 보정"),
            ["Calc Back Project"] = ("히스토그램 역투영을 수행합니다.", "색상 기반 객체 추적"),
            // Arithmetic
            ["Add"] = ("두 이미지를 픽셀 단위로 더합니다.", "이미지 합성"),
            ["Subtract"] = ("두 이미지를 픽셀 단위로 뺍니다.", "배경 제거"),
            ["Multiply"] = ("두 이미지를 픽셀 단위로 곱합니다.", "마스크 적용"),
            ["Abs Diff"] = ("두 이미지의 절대 차이를 계산합니다.", "변화 검출"),
            ["Bitwise AND"] = ("두 이미지의 비트 AND 연산을 수행합니다.", "마스크 적용"),
            ["Bitwise OR"] = ("두 이미지의 비트 OR 연산을 수행합니다.", "마스크 합집합"),
            ["Bitwise XOR"] = ("두 이미지의 비트 XOR 연산을 수행합니다.", "차이점 하이라이트"),
            ["Bitwise NOT"] = ("이미지의 비트 NOT(반전) 연산을 수행합니다.", "이미지 반전"),
            ["Weighted Add"] = ("가중 합(알파 블렌딩)을 수행합니다.", "이미지 블렌딩"),
            ["Mask Apply"] = ("마스크를 이미지에 적용합니다.", "ROI 추출"),
            ["Image Blend"] = ("두 이미지를 알파값으로 블렌딩합니다.", "이미지 합성"),
            // Detection
            ["Hough Lines"] = ("허프 변환으로 직선을 검출합니다.", "직선 검출, 차선 인식"),
            ["Hough Circles"] = ("허프 변환으로 원을 검출합니다.", "원형 부품 검출"),
            ["Template Match"] = ("템플릿 매칭으로 이미지에서 패턴을 찾습니다.", "패턴 검색"),
            ["Template Match Multi"] = ("다수의 템플릿 매치를 찾습니다.", "반복 패턴 검출"),
            ["Haar Cascade"] = ("Haar 캐스케이드 분류기로 객체를 검출합니다.", "얼굴 검출"),
            ["Min Max Loc"] = ("이미지의 최소/최대 픽셀 값과 위치를 찾습니다.", "극값 탐색"),
            ["Pixel Count"] = ("비영 픽셀 수를 세고 비율을 계산합니다.", "영역 비율 측정"),
            ["Line Profile"] = ("직선 위의 픽셀 강도 프로파일을 생성합니다.", "에지 품질 분석"),
            ["Connected Components"] = ("연결 성분 라벨링을 수행합니다.", "개별 객체 식별"),
            // Segmentation
            ["Flood Fill"] = ("시드 포인트에서 유사한 영역을 채웁니다.", "영역 채우기"),
            ["GrabCut"] = ("GrabCut으로 전경/배경을 분리합니다.", "객체 분리"),
            ["Watershed"] = ("워터셰드 알고리즘으로 세그먼트합니다.", "접촉 객체 분리"),
            // Control
            ["Boolean"] = ("제어 흐름용 불리언 상수를 출력합니다.", "조건부 실행 플래그"),
            ["Compare"] = ("두 숫자를 비교하여 불리언 결과를 출력합니다.", "검사 판정"),
            ["Boolean Logic"] = ("두 불리언 값에 대한 논리 연산을 수행합니다.", "복합 조건 로직"),
            ["If Select"] = ("조건에 따라 두 값 중 하나를 선택합니다.", "조건부 경로 분기"),
            ["Switch"] = ("인덱스에 따라 여러 입력 중 하나를 선택합니다.", "다중 선택"),
            ["For Loop"] = ("순차적으로 인덱스를 생성합니다.", "반복 카운터"),
            ["For"] = ("시작~끝 범위의 for 루프를 실행합니다.", "배치 이미지 처리"),
            ["ForEach"] = ("컬렉션의 각 요소에 대해 반복 실행합니다.", "배열 요소별 처리"),
            ["While"] = ("BreakIf로 중단할 때까지 반복 실행합니다.", "수렴 조건까지 반복"),
            ["BreakIf"] = ("조건이 참이면 현재 루프를 중단합니다.", "루프 조기 종료"),
            ["Collect"] = ("루프 반복의 결과를 배열로 수집합니다.", "루프 결과 집계"),
            ["Delay"] = ("지정된 밀리초 동안 실행을 지연합니다.", "타이밍 제어"),
            // Data
            ["String to Number"] = ("문자열을 숫자로 변환합니다.", "통신 수신 데이터 파싱"),
            ["Number to String"] = ("숫자를 포맷 문자열로 변환합니다.", "결과 표시 포맷팅"),
            ["CSV Reader"] = ("CSV 파일을 읽어 데이터 배열과 헤더를 출력합니다.", "검사 데이터 로드"),
            ["CSV Parser"] = ("입력 문자열에서 CSV를 파싱합니다.", "동적 데이터 파싱"),
            ["String Split"] = ("구분자로 문자열을 분할합니다.", "데이터 파싱"),
            ["String Join"] = ("문자열 배열을 구분자로 결합합니다.", "데이터 조합"),
            ["String Replace"] = ("문자열에서 찾기/바꾸기를 수행합니다.", "데이터 정제"),
            // Communication
            ["Serial Port"] = ("시리얼(COM) 포트 통신을 수행합니다.", "PLC 통신, 센서 데이터 수신"),
            ["TCP Server"] = ("TCP 서버를 구동합니다.", "MES 연동, 원격 모니터링"),
            ["TCP Client"] = ("TCP 클라이언트로 서버에 연결합니다.", "PLC 이더넷 통신"),
            // Event
            ["Keyboard Event"] = ("Image Show 윈도우에서 키보드 이벤트를 수신합니다.", "키보드 트리거"),
            ["Mouse Event"] = ("Image Show 윈도우에서 마우스 이벤트를 수신합니다.", "마우스 좌표 추적"),
            ["Mouse ROI"] = ("마우스 드래그로 ROI 사각형을 그립니다.", "동적 ROI 설정"),
            ["WaitKey"] = ("키 입력을 대기합니다.", "실행 흐름 제어"),
            // Script
            ["Python Script"] = ("시스템 Python으로 스크립트를 실행합니다.", "커스텀 알고리즘"),
            ["C# Script"] = ("OpenCvSharp을 포함한 C# 스크립트를 실행합니다.", "커스텀 C# 알고리즘"),
            // Inspection
            ["Alignment Checker"] = ("객체의 방향 각도를 측정하여 정렬 상태를 검사합니다.", "부품 정렬 검사"),
            ["Brightness Uniformity"] = ("격자로 나눈 이미지의 밝기 균일성을 검사합니다.", "디스플레이 균일성 검사"),
            ["Circle Detector"] = ("허프 변환으로 원형 객체를 검출합니다.", "원형 부품 검출"),
            ["Color Object Detector"] = ("HSV 색상 범위로 특정 색상의 객체를 검출합니다.", "색상별 부품 분류"),
            ["Contour Center Finder"] = ("이미지에서 객체의 중심을 자동으로 찾습니다.", "객체 위치 측정"),
            ["Defect Detector"] = ("기준 이미지와 비교하여 결함을 검출합니다.", "외관 결함 검출"),
            ["Edge Inspector"] = ("허프 직선 검출로 에지/라인을 찾아 정렬 분석합니다.", "에지 정렬 검사"),
            ["Face Detector"] = ("Haar 캐스케이드로 얼굴을 검출합니다.", "출입 관리"),
            ["Object Counter"] = ("이미지에서 객체를 자동 카운팅합니다.", "부품 카운팅"),
            ["Pattern Matcher"] = ("템플릿 매칭+NMS로 패턴의 위치를 찾습니다.", "마크 검사"),
            ["Presence Checker"] = ("ROI 영역 내의 채워진 비율로 객체 존재 여부를 판단합니다.", "부품 유무 확인"),
            ["Scratch Detector"] = ("모폴로지로 표면 선형 스크래치를 검출합니다.", "표면 스크래치 검사"),
            ["Shape Classifier"] = ("윤곽선을 원, 사각형, 삼각형 등으로 분류합니다.", "형상 기반 부품 분류"),
            // Measurement
            ["Angle Measure"] = ("두 주요 직선 사이의 각도를 측정합니다.", "조립 각도 확인"),
            ["Distance Measure"] = ("두 검출 객체의 중심 간 거리를 측정합니다.", "부품 간격 측정"),
            ["Object Measure"] = ("윤곽선 분석으로 객체의 폭과 높이를 측정합니다.", "부품 치수 측정"),
            // MediaPipe
            ["MP Face Detection"] = ("MediaPipe BlazeFace로 얼굴을 검출합니다.", "실시간 얼굴 검출"),
            ["MP Face Mesh"] = ("MediaPipe Face Mesh로 468개 얼굴 랜드마크를 검출합니다.", "표정 분석"),
            ["MP Hand Landmark"] = ("MediaPipe로 21개 손 랜드마크를 검출합니다.", "제스처 인식"),
            ["MP Pose Landmark"] = ("MediaPipe BlazePose로 33개 신체 포즈 랜드마크를 검출합니다.", "자세 분석"),
            ["MP Object Detection"] = ("SSD MobileNet V2로 COCO 80개 클래스 객체를 검출합니다.", "범용 객체 검출"),
            ["MP Selfie Segmentation"] = ("MediaPipe로 사람/배경을 분리합니다.", "배경 블러/교체"),
            // YOLO
            ["YOLOv8 Detection"] = ("YOLOv8 ONNX 모델로 객체를 검출합니다.", "실시간 객체 검출"),
            // OCR
            ["PaddleOCR"] = ("PaddleOCR로 다국어 텍스트를 검출/인식합니다.", "문서 OCR, 시리얼 넘버 인식"),
            ["Tesseract OCR"] = ("Tesseract 엔진으로 텍스트를 인식합니다.", "인쇄 문서 OCR"),
            // LLM/VLM
            ["Claude Vision"] = ("Anthropic Claude API로 이미지를 분석합니다.", "이미지 분석 리포트"),
            ["Gemini Vision"] = ("Google Gemini API로 이미지를 분석합니다.", "이미지 설명 생성"),
            ["OpenAI Vision"] = ("OpenAI GPT-4o API로 이미지를 분석합니다.", "이미지 캡셔닝"),
        };
    }

    public static Dictionary<string, string> GetCategoryNames()
    {
        return new Dictionary<string, string>
        {
            ["Input"] = "입출력",
            ["Color"] = "색상",
            ["Filter"] = "필터",
            ["Edge"] = "에지 검출",
            ["Morphology"] = "모폴로지",
            ["Threshold"] = "임계값",
            ["Contour"] = "윤곽선",
            ["Feature"] = "특징점",
            ["Drawing"] = "그리기",
            ["Transform"] = "변환",
            ["Histogram"] = "히스토그램",
            ["Arithmetic"] = "연산",
            ["Detection"] = "검출",
            ["Segmentation"] = "분할",
            ["Value"] = "값",
            ["Control"] = "제어",
            ["Communication"] = "통신",
            ["Data"] = "데이터 처리",
            ["Event"] = "이벤트",
            ["Script"] = "스크립트",
            ["Inspection"] = "검사",
            ["Measurement"] = "측정",
            ["MediaPipe"] = "MediaPipe",
            ["YOLO"] = "YOLO",
            ["OCR"] = "OCR",
            ["LLM/VLM"] = "LLM/VLM",
            ["Function"] = "함수",
        };
    }
}
