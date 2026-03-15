## MVXTester 튜토리얼

## 튜토리얼 목록 요약

MVXTester에서 제공하는 학습 예제:
- Tutorial 1: 기본 이미지 처리 (Image Read → Grayscale → Blur → Canny Edge)
- Tutorial 2: 컨투어를 이용한 객체 검출 (이진화 → FindContours → DrawContours)
- Tutorial 3: 라이브 카메라 처리 (Camera → 실시간 에지 검출)
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
- Tutorial 18: Tesseract OCR 상세
- Tutorial 19~30: LLM/VLM, 시리얼 통신, TCP, 이벤트, 스크립트, 산업 응용 등

### Tutorial 1: 기본 이미지 처리
**목표**: 이미지를 로드하여 그레이스케일 변환, 블러, 엣지 검출을 수행합니다.

1. Input/Output 카테고리에서 "Image Read" 노드를 추가합니다
2. 노드를 더블클릭하여 이미지 파일을 선택합니다
3. Color 카테고리에서 "Convert Color" 노드를 추가하고 Code = BGR2GRAY로 설정합니다
4. Image Read → Convert Color를 연결합니다
5. Filter 카테고리에서 "Gaussian Blur" 노드를 추가하고 KernelW/H = 5로 설정합니다
6. Edge Detection 카테고리에서 "Canny" 노드를 추가하고 Threshold1=100, Threshold2=200으로 설정합니다
7. 모든 노드를 좌에서 우로 연결하고 F5를 눌러 실행합니다

### Tutorial 2: 컨투어를 이용한 객체 검출
**목표**: 이진 이미지에서 객체를 찾고 개수를 셉니다.

1. Image Read → Convert Color (BGR2GRAY) → Threshold (Thresh=128)을 연결합니다
2. Contour 카테고리에서 "Find Contours" 노드를 추가합니다
3. "Filter Contours"를 추가하고 MinArea=500으로 설정하여 노이즈를 제거합니다
4. "Draw Contours"를 추가하고 원본 이미지 + 필터링된 컨투어를 연결합니다
5. Count 출력으로 검출된 객체 수를 확인할 수 있습니다

### Tutorial 3: 라이브 카메라 처리
**목표**: 실시간 카메라 피드를 처리합니다.

1. Input 카테고리에서 "Camera" 노드를 추가합니다
2. Refresh를 클릭하여 연결된 모든 카메라를 검색합니다 (USB, HIK, Cognex)
3. 카메라를 선택하면 속성이 자동으로 조정됩니다
4. 처리 노드(blur, threshold 등)를 연결합니다
5. F6(Stream)을 눌러 연속 캡처를 시작합니다
6. 실시간으로 파라미터를 조정합니다 — 결과가 즉시 업데이트됩니다
7. F6을 다시 눌러 스트리밍을 중지합니다

### Tutorial 4: 템플릿 매칭
**목표**: 큰 이미지에서 템플릿 패턴을 찾습니다.

1. 두 개의 "Image Read" 노드를 추가합니다 — 하나는 장면용, 하나는 템플릿용
2. Detection 카테고리에서 "Template Match" 노드를 추가합니다
3. 장면 → Image, 템플릿 → Template 입력을 연결합니다
4. Method = CCoeffNormed로 설정하면 최상의 결과를 얻습니다
5. 출력: Location (최적 매칭 위치)과 Score (0~1)
6. 다중 매칭을 위해 "Template Match Multi"를 사용하고 Threshold=0.8로 설정합니다

### Tutorial 5: 제어 흐름 사용 (루프)
**목표**: For 루프를 사용하여 같은 파이프라인으로 여러 이미지를 처리합니다.

1. Control 카테고리에서 "For" 노드를 추가하고 Start=0, End=5, Step=1로 설정합니다
2. 루프 본문에 처리 노드를 추가합니다
3. Index 출력을 사용하여 반복마다 파라미터를 변경합니다
4. 끝에 "Collect" 노드를 추가하여 결과를 리스트로 수집합니다
5. "Break If" 노드로 조건에 따라 조기 종료할 수 있습니다

### Tutorial 6: 재사용 가능한 함수 만들기
**목표**: 처리 파이프라인을 재사용 가능한 함수 노드로 패키징합니다.

1. 새 그래프에서 파이프라인을 구성합니다
2. .mvxp 프로젝트 파일로 저장합니다
3. 다른 그래프에서 툴바의 "Import Function"을 클릭합니다
4. 소스 노드(입력 없음)가 함수 파라미터가 됩니다
5. 싱크 노드(출력 없음)가 함수 반환값이 됩니다
6. 함수 노드를 더블클릭하여 내부 구조를 볼 수 있습니다

### Tutorial 7: 산업용 검사
**목표**: 레퍼런스 이미지와 비교하여 결함을 검출합니다.

1. 두 개의 "Image Read" 노드를 추가합니다 — 레퍼런스와 테스트 이미지
2. Inspection 카테고리에서 "Defect Detector" 노드를 추가합니다
3. 레퍼런스와 테스트 이미지를 연결합니다
4. Threshold와 MinDefectArea를 조정하여 노이즈를 필터링합니다
5. ShowOverlay를 활성화하여 원본 이미지에 결함을 시각화합니다

### Tutorial 8: 얼굴 검출 (MediaPipe)
**목표**: MediaPipe BlazeFace를 사용하여 실시간 얼굴을 검출합니다.

1. Camera → MP Face Detection을 연결합니다 (Result 출력에 바운딩 박스 표시)
2. Result → Image Show를 연결하고 F5로 실행합니다
3. Faces 출력(Rect[])은 ROI 크롭에 사용할 수 있습니다

### Tutorial 9: 손 추적
**목표**: 실시간 손 랜드마크를 추적합니다.

1. Camera → MP Hand Landmark → Image Show를 연결합니다
2. F6(Stream)으로 30fps 연속 추적을 시작합니다
3. 손 하나당 21개 랜드마크와 스켈레톤 연결이 표시됩니다

### Tutorial 10: 포즈 추정
33개 신체 키포인트와 스켈레톤 시각화. Visibility 출력으로 검출된 관절을 확인할 수 있습니다.

### Tutorial 11: 페이스 메시 (468 포인트)
468개 얼굴 랜드마크 포인트와 컨투어 그리기. DrawContour/DrawPoints 속성으로 시각화를 제어합니다.

### Tutorial 12: 배경 제거
Effect 옵션: Blur(배경 블러), Remove(투명), GreenScreen(초록색 배경).

### Tutorial 13: 객체 검출 (COCO 80 클래스)
SSD MobileNet V2로 COCO 80개 클래스 검출: person, car, bicycle, dog, cat 등.

### Tutorial 14: YOLOv8 객체 검출

1. yolov8n.onnx를 Models/YOLO/ 폴더에 배치합니다
2. Image Read → YOLOv8 Detection → Image Show를 연결합니다
3. 모델 메타데이터에서 입력 크기와 클래스 수를 자동 감지합니다

### Tutorial 15: 실시간 YOLOv8 카메라
F6(Stream 모드)로 연속 검출합니다. 속도 우선은 yolov8n, 정확도 우선은 yolov8m/l을 사용합니다.

### Tutorial 16: 텍스트 인식 (PaddleOCR)

1. ppocr_det.onnx, ppocr_rec.onnx, ppocr_keys.txt를 Models/OCR/에 배치합니다
2. 출력: Texts[](각 텍스트 영역), FullText(전체 결합), Boxes[](위치)

### Tutorial 17: 한국어/영어 OCR
RecModel=ppocr_rec.onnx, DictFile=ppocr_keys.txt로 한국어/중국어를 인식합니다. 영어 전용은 ppocr_rec_en.onnx를 사용합니다.

### Tutorial 18: Tesseract OCR
eng.traineddata/kor.traineddata를 Models/Tesseract/에 배치합니다. Language="eng+kor"로 혼합 텍스트를 인식합니다.

### Tutorial 19: LLM/VLM 이미지 분석 (OpenAI GPT-4o)
API 키는 Models/API/api_config.json에서 자동 로드됩니다. Prompt 입력 포트로 커스텀 질문을 지정합니다.

### Tutorial 20: LLM/VLM 이미지 분석 (Gemini)
Google Gemini API를 사용한 이미지 분석. api_config.json에서 API 키를 설정합니다.

### Tutorial 21: LLM/VLM 이미지 분석 (Claude)
API 키를 Models/API/api_config.json에 설정하거나 Properties 패널에서 수동 입력합니다.

### Tutorial 22: 멀티 LLM/VLM 비교
**목표**: 같은 이미지에 대한 GPT-4o, Gemini, Claude의 응답을 비교합니다.

하나의 Image Read를 3개의 LLM/VLM 노드에 연결합니다. Response 출력을 나란히 비교합니다.

### Tutorial 23: OCR + LLM/VLM 검증
OCR FullText → LLM/VLM Prompt 입력. LLM/VLM을 사용하여 OCR 결과를 검증, 번역 또는 요약합니다.

### Tutorial 24: 히스토그램 균등화
먼저 그레이스케일로 변환(BGR2GRAY)한 후 균등화하여 대비를 개선합니다.

### Tutorial 25: 색상 기반 객체 검출
Color Object Detector(Inspection): 대상 색상의 HSV 범위를 설정합니다. 검출된 영역의 컨투어를 출력합니다.

### Tutorial 26: 모폴로지 파이프라인
Erode로 노이즈를 제거하고 Dilate로 빈 공간을 채웁니다. 깨끗한 이진 마스크를 위한 클래식 모폴로지 처리입니다.

### Tutorial 27: 특징점 매칭 (ORB)
두 이미지에서 ORB 특징점을 추출하고 BFMatcher로 대응점을 찾아 시각화합니다.

### Tutorial 28: 워터셰드 세그멘테이션
마커 기반 세그멘테이션. 각 연결 영역은 출력에서 고유한 색상을 받습니다.

### Tutorial 29: TCP 통신
TCP Server가 명령을 수신(백그라운드 스레드)하고 처리를 트리거합니다. TCP Client가 결과를 다시 전송합니다. F5 Runtime 모드를 사용합니다.

### Tutorial 30: 완전한 검사 파이프라인
**목표**: Pass/Fail 판정과 네트워크 보고를 포함한 완전한 산업 검사 파이프라인입니다.

Camera → Blur → Threshold → Contours → Filter → Measurement → If (Pass/Fail 조건) → TCP Client (결과를 PLC/MES로 전송)
트리거 기반 검사를 위해 TCP Server와 함께 F5 Runtime을 사용합니다.
