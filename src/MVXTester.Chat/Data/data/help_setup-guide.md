## MVXTester 설치 및 설정 가이드

### 설치 요약

MVXTester 실행에 필요한 항목:
- .NET 8.0 SDK (필수)
- Windows 10/11 x64 (필수)
- Visual Studio 2022 (개발용)
- ONNX 모델: MediaPipe(얼굴/손/포즈), YOLOv8, PaddleOCR, Tesseract → Models/ 폴더에 배치
- API 키 (선택): OpenAI, Gemini, Claude → 챗봇 설정에서 입력
- 카메라 SDK (선택): HIKROBOT MVS, Cognex GigE Vision

### 1. 필수 SDK

- **.NET 8.0 SDK**: https://dotnet.microsoft.com/download
- **Windows 10/11**: WPF는 Windows 필수
- **Visual Studio 2022** 또는 **dotnet CLI**

빌드:
```
dotnet build MVXTester.sln
```

실행:
```
dotnet run --project src/MVXTester.App/MVXTester.App.csproj
```

### 2. ONNX 모델

모델 파일을 해당 Models/ 하위 폴더에 배치합니다. 빌드 시 출력 디렉토리로 자동 복사됩니다.

#### MediaPipe (Models/MediaPipe/)

| 모델 파일 | 용도 |
| --- | --- |
| blazeface_short.onnx | 얼굴 검출 |
| palm_detection.onnx | 손 검출 (1단계) |
| hand_landmark.onnx | 손 랜드마크 (2단계) |
| pose_detection.onnx | 포즈 검출 (1단계) |
| pose_landmark_lite.onnx | 포즈 랜드마크 (2단계) |
| face_landmark.onnx | 페이스 메시 (468 포인트) |
| selfie_segmentation.onnx | 인물 세그멘테이션 |
| ssd_mobilenet_v2.onnx | 객체 검출 (COCO 80) |

다운로드: google/mediapipe ONNX exports 또는 HuggingFace

#### YOLOv8 (Models/YOLO/)

| 모델 파일 | 크기 | 특징 |
| --- | --- | --- |
| yolov8n.onnx | 6MB | 가장 빠름 (Nano) |
| yolov8s.onnx | 22MB | Small |
| yolov8m.onnx | 52MB | Medium |
| yolov8l.onnx | 87MB | Large |
| yolov8x.onnx | 131MB | 가장 정확 (XLarge) |

다운로드: https://github.com/ultralytics/assets
내보내기: `yolo export model=yolov8n.pt format=onnx`

#### PaddleOCR (Models/OCR/)

| 파일 | 용도 |
| --- | --- |
| ppocr_det.onnx | 텍스트 검출 (DB 알고리즘) |
| ppocr_rec.onnx | 텍스트 인식 (한국어/중국어) |
| ppocr_keys.txt | 문자 사전 (9000+ 글자) |
| ppocr_rec_en.onnx | 영어 인식 (선택사항) |
| ppocr_keys_en.txt | 영어 사전 (선택사항) |

다운로드: HuggingFace monkt/paddleocr-onnx (PP-OCRv5)

#### Tesseract OCR (Models/Tesseract/)

| 파일 | 크기 | 용도 |
| --- | --- | --- |
| eng.traineddata | 23MB | 영어 |
| kor.traineddata | 15MB | 한국어 |
| jpn.traineddata | - | 일본어 (선택) |
| chi_sim.traineddata | - | 중국어 간체 (선택) |

다운로드: https://github.com/tesseract-ocr/tessdata

### 3. API 키 설정

LLM/VLM 노드는 Models/API/api_config.json에서 API 키를 로드합니다:

```json
{
  "openai": {
    "api_key": "sk-proj-...",
    "model": "gpt-4o-mini"
  },
  "gemini": {
    "api_key": "AIza...",
    "model": "gemini-2.5-flash"
  },
  "claude": {
    "api_key": "sk-ant-...",
    "model": "claude-sonnet-4-20250514"
  }
}
```

#### API 키 발급 방법

**OpenAI**
1. https://platform.openai.com 방문
2. API Keys 메뉴 → Create new secret key
3. sk-proj-... 형식의 키 복사

**Google Gemini**
1. https://aistudio.google.com 방문
2. Get API key 클릭
3. AIza... 형식의 키 복사

**Anthropic Claude**
1. https://console.anthropic.com 방문
2. API Keys 메뉴 → Create Key
3. sk-ant-... 형식의 키 복사

api_config.json 파일이 없어도 노드 Properties 패널에서 수동 입력이 가능합니다.

### 4. 카메라 SDK (선택사항)

#### USB Camera
추가 SDK 불필요 (OpenCvSharp VideoCapture 사용)

#### HIKROBOT MVS SDK
- https://www.hikrobotics.com 에서 MVS SDK 다운로드 및 설치
- MvCameraControl.Net.dll 자동 검색
- GigE Vision / USB3 Vision 카메라 지원

#### Cognex VisionPro 9.x
- VisionPro SDK 설치 후 자동 검색
- GigE Vision 카메라 (CIC 시리즈)

SDK 미설치 환경에서도 카메라 외 모든 기능은 정상 작동합니다.

### 5. 출력 폴더 구조

```
bin/Debug/net8.0-windows/
├── Models/
│   ├── MediaPipe/    *.onnx (자동 복사)
│   ├── YOLO/         yolov8n.onnx
│   ├── OCR/          ppocr_*.onnx, ppocr_keys.txt
│   ├── Tesseract/    *.traineddata
│   ├── API/          api_config.json (자동 복사)
│   └── Chat/         chat_config.json (자동 복사)
├── MVXTester.App.exe
└── *.dll
```
