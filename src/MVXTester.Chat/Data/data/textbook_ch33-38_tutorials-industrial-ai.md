## Chapter 33: 기본 이미지 처리 예제


    이 장에서는 MVXTester의 기본 노드들을 활용하여 실무에서 자주 사용되는 이미지 처리 파이프라인을 구축하는 방법을 학습합니다.
    엣지 검출, 색상 분류, 영역 검출, 전처리 파이프라인, ROI 비교 등 5가지 기본 예제를 통해 노드 기반 비전 시스템의 핵심 패턴을 익힐 수 있습니다.


    예제 1: 엣지 기반 부품 검사


#### 목적


      제조 라인에서 금속 부품의 외곽 형상이 올바른지 검사하는 시스템을 구축합니다. Canny 엣지 검출을 적용하여 부품의 윤곽선을 추출하고,
      검출된 엣지의 픽셀 수를 기준으로 양품/불량을 판정하는 기본적인 비전 검사 파이프라인입니다.


      이 예제를 통해 Image Read에서 시작하여 전처리(블러) - 처리(엣지 검출) - 분석(컨투어) - 판정의 전형적인 비전 검사 흐름을 이해할 수 있습니다.
      GaussianBlur로 노이즈를 제거한 후 Canny Edge를 적용하면 안정적인 엣지 검출이 가능합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 검사 대상 이미지 로드 |
| Gaussian Blur | Filter | 노이즈 제거를 위한 가우시안 블러 |
| Canny Edge | Edge | 엣지 검출 |
| Find Contours | Contour | 엣지 영역에서 컨투어 추출 |
| Contour Filter | Contour | 면적 기준으로 유효 컨투어 필터링 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


Filter

Edge

Contour

Contour


#### 구축 단계


      - **이미지 로드:** Image Read 노드를 배치하고 FilePath 속성에 검사 대상 부품 이미지 경로를 설정합니다.

      - **노이즈 제거:** Gaussian Blur 노드를 추가하고 Image Read의 Image 출력 포트를 Gaussian Blur의 Image 입력 포트에 연결합니다. Kernel Width/Height를 5로 설정합니다.

      - **엣지 검출:** Canny Edge 노드를 추가하고 Gaussian Blur의 Result 출력을 연결합니다. Threshold 1을 100, Threshold 2를 200으로 설정합니다.

      - **컨투어 추출:** Find Contours 노드를 연결하여 엣지 이미지에서 컨투어를 검출합니다. Retrieval Mode를 External로 설정합니다.

      - **필터링:** Contour Filter 노드로 Min Area를 500, Max Area를 10000000으로 설정하여 노이즈성 작은 컨투어를 제거합니다.

      - **결과 확인:** Image Show 노드를 연결하고 F5를 눌러 실행합니다. Contour Filter의 Count 출력으로 검출된 유효 컨투어 수를 확인합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Gaussian Blur | Kernel Width / Height | 5 | 홀수만 허용, 값이 클수록 블러 강도 증가 |
| Gaussian Blur | Sigma X | 0.0 | 0이면 커널 크기에서 자동 계산 |
| Canny Edge | Threshold 1 | 100 | 하한 임계값 (약한 엣지 기준) |
| Canny Edge | Threshold 2 | 200 | 상한 임계값 (강한 엣지 기준) |
| Canny Edge | Aperture Size | 3 | Sobel 연산 커널 크기 (3, 5, 7) |
| Contour Filter | Min Area | 500 | 500px 미만의 작은 노이즈 제거 |


#### 결과 해석


      실행 결과 Image Show 창에 검사 이미지가 표시되며, Contour Filter의 Count 출력에서 검출된 유효 컨투어 수를 확인할 수 있습니다.
      정상 부품의 경우 일정한 수의 컨투어가 검출되며, 형상 이상이 있는 불량 부품은 컨투어 수나 면적이 달라집니다.


      Canny의 두 임계값 비율은 일반적으로 1:2 또는 1:3을 권장합니다. 부품의 명암 대비가 낮은 경우 임계값을 낮추고,
      배경 노이즈가 많은 경우에는 Gaussian Blur의 커널 크기를 7이나 9로 높여 전처리를 강화합니다.


      Contour Filter의 Count 출력을 Compare 노드에 연결하면 자동 OK/NG 판정 시스템으로 확장할 수 있습니다.


> **참고:** **팁:** Canny Edge의 Threshold 1과 2를 조정할 때는 먼저 높은 값(예: 150/300)에서 시작하여 점차 낮추면서 필요한 엣지만 검출되는 최적값을 찾으세요. Aperture Size를 5로 높이면 더 부드러운 엣지를 검출할 수 있습니다.


    예제 2: 색상 분류 시스템


#### 목적


      컨베이어 벨트 위의 부품을 색상별로 분류하는 시스템을 구축합니다. BGR 이미지를 HSV 색공간으로 변환한 후
      InRange 노드로 특정 색상 범위를 마스킹하여 해당 색상의 객체를 검출합니다. Color Object Detector 노드를 활용하면
      이 과정을 단일 노드로 간소화할 수 있습니다.


      HSV 색공간은 조명 변화에 강인한 색상 검출을 가능하게 합니다. H(Hue) 채널은 색상, S(Saturation)는 채도,
      V(Value)는 밝기를 나타내므로 밝기 변화와 독립적으로 색상을 검출할 수 있습니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 입력 이미지 로드 |
| Color Object Detector | Inspection | HSV 기반 색상 객체 검출 |
| Image Show | Input/Output | 검출 결과 시각화 |


#### 노드 파이프라인


Inspection


#### 구축 단계


      - **이미지 로드:** Image Read 노드로 컬러 부품 이미지를 로드합니다.

      - **색상 검출 설정:** Color Object Detector 노드를 추가하고 Image 입력을 연결합니다. 빨간색 부품을 검출하려면 Hue Low=0, Hue High=10 (또는 170~179)으로 설정합니다.

      - **채도/밝기 필터:** Saturation Low=50, Value Low=50으로 설정하여 너무 어둡거나 채도가 낮은 영역을 제외합니다.

      - **최소 면적 설정:** Min Area를 500으로 설정하여 노이즈를 제거합니다.

      - **결과 확인:** Result 출력을 Image Show에 연결합니다. Count 출력에서 검출된 객체 수, BoundingBoxes에서 위치 정보를 확인합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Color Object Detector | Hue Low / High | 0 / 10 | 빨간색: 0~10 또는 170~179 |
| Color Object Detector | Saturation Low | 50 | 채도 하한 (높을수록 선명한 색만) |
| Color Object Detector | Value Low | 50 | 밝기 하한 (어두운 영역 제외) |
| Color Object Detector | Min Area | 500 | 최소 객체 면적 (px) |
| Color Object Detector | Show Mask | false | true 시 바이너리 마스크 출력 |


#### 결과 해석


      실행 결과 검출된 색상 객체 주위에 녹색 바운딩 박스가 표시되고, 중심점에 빨간 점이 표시됩니다.
      각 객체에는 인덱스 번호와 면적(px)이 라벨로 표시됩니다. Count 출력에서 전체 검출 수를 확인할 수 있으며,
      Centers 출력에서 각 객체의 중심 좌표를 얻을 수 있습니다.


      파란색을 검출하려면 Hue Low=100, Hue High=130으로 변경하고, 녹색은 Hue Low=35, Hue High=85를 사용합니다.
      여러 Color Object Detector 노드를 병렬로 배치하면 다중 색상 동시 분류가 가능합니다.


      Show Mask를 true로 설정하면 바이너리 마스크 이미지를 확인하여 색상 범위가 적절한지 디버깅할 수 있습니다.


> **참고:** **팁:** OpenCV의 HSV에서 H 범위는 0~179 (일반적인 0~360의 절반)입니다. 빨간색은 H=0 부근과 H=170 부근에 걸쳐 있으므로 두 개의 Color Object Detector를 사용하거나, 두 범위를 합치는 방식을 고려하세요.


    예제 3: 바코드/QR 영역 검출


#### 목적


      제품 라벨에서 바코드나 QR 코드가 인쇄된 영역을 검출하는 시스템을 구축합니다.
      바코드 영역은 특유의 고밀도 수직 엣지 패턴을 가지므로 Morphology Ex의 Close 연산으로 인접 엣지들을
      하나의 영역으로 병합한 후 컨투어를 찾아 바운딩 박스를 추출합니다.


      이 예제는 엣지 검출 - 모폴로지 - 컨투어 분석의 조합 패턴을 보여주며,
      실무에서 특정 텍스처 영역을 검출하는 기본 기법으로 널리 활용됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 라벨 이미지 로드 |
| Convert Color | Color | BGR → Grayscale 변환 |
| Gaussian Blur | Filter | 노이즈 제거 |
| Canny Edge | Edge | 엣지 검출 |
| Morphology Ex | Morphology | Close 연산으로 엣지 영역 병합 |
| Find Contours | Contour | 병합된 영역에서 컨투어 추출 |
| Bounding Rect | Contour | 바운딩 박스 계산 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


Color

Filter

Edge


      &darr;


Morphology

Contour

Contour


#### 구축 단계


      - **이미지 로드:** Image Read 노드로 바코드가 포함된 라벨 이미지를 로드합니다.

      - **그레이스케일 변환:** Convert Color 노드를 추가하고 Conversion을 BGR2GRAY로 설정합니다.

      - **블러 처리:** Gaussian Blur로 Kernel Width/Height=3으로 가벼운 노이즈 제거를 합니다.

      - **엣지 검출:** Canny Edge에서 Threshold 1=50, Threshold 2=150으로 바코드의 세밀한 엣지를 검출합니다.

      - **모폴로지 Close:** Morphology Ex 노드에서 Operation=Close, Kernel Size=21, Kernel Shape=Rect로 설정합니다. 이 연산으로 인접한 바코드 라인들이 하나의 연결 영역으로 병합됩니다.

      - **컨투어 및 바운딩 박스:** Find Contours로 병합 영역을 찾고, Bounding Rect로 바운딩 박스를 계산합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Convert Color | Conversion | BGR2GRAY | 그레이스케일 변환 |
| Canny Edge | Threshold 1 / 2 | 50 / 150 | 바코드 라인 검출을 위한 낮은 임계값 |
| Morphology Ex | Operation | Close | 엣지 간 간격을 메워 영역을 병합 |
| Morphology Ex | Kernel Size | 21 | 바코드 라인 간격에 맞는 커널 크기 |
| Morphology Ex | Kernel Shape | Rect | 직사각형 커널이 바코드에 적합 |


#### 결과 해석


      Morphology Ex의 Close 연산 결과 바코드 영역이 하나의 흰색 덩어리로 병합됩니다.
      Find Contours에서 이 영역의 컨투어를 찾고, Bounding Rect에서 직사각형 바운딩 박스가 계산됩니다.
      가장 큰 바운딩 박스가 바코드/QR 코드 영역에 해당합니다.


      Kernel Size가 너무 작으면 바코드 라인들이 완전히 병합되지 않고, 너무 크면 주변 텍스트까지 포함됩니다.
      바코드 라인 간격의 2~3배 정도의 커널 크기가 적절합니다.


      Contour Filter를 추가하여 면적 기준으로 노이즈 영역을 제거하면 더 정확한 결과를 얻을 수 있습니다.


> **참고:** **팁:** Morphology Ex의 Iterations 속성을 2~3으로 높이면 Close 연산을 반복 적용하여 더 넓은 간격의 엣지도 병합할 수 있습니다. QR 코드 검출 시에는 Kernel Shape를 Rect로 유지하세요.


    예제 4: 이미지 전처리 파이프라인


#### 목적


      산업용 카메라에서 취득한 원시 이미지를 비전 검사에 적합한 상태로 정규화하는 전처리 파이프라인을 구축합니다.
      그레이스케일 변환, 히스토그램 평활화(CLAHE), 노이즈 제거, 이진화의 순서로 처리하여
      조명 변화에 강인한 안정적인 검사 입력을 생성합니다.


      전처리는 모든 비전 검사의 기반이 됩니다. 적절한 전처리 없이는 후단의 검출/분석 노드가 불안정한 결과를 생성합니다.
      이 파이프라인을 기본 템플릿으로 저장해두면 다양한 검사 프로젝트에 재활용할 수 있습니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 원시 이미지 로드 |
| Convert Color | Color | BGR → Grayscale 변환 |
| CLAHE | Histogram | 적응형 히스토그램 평활화 |
| Median Blur | Filter | 솔트&페퍼 노이즈 제거 |
| Adaptive Threshold | Threshold | 조명 불균일에 강인한 이진화 |
| Image Show | Input/Output | 각 단계 결과 확인 |


#### 노드 파이프라인


Color

Histogram

Filter

Threshold


#### 구축 단계


      - **이미지 로드:** Image Read로 조명 불균일이 있는 검사 이미지를 로드합니다.

      - **그레이스케일 변환:** Convert Color에서 BGR2GRAY를 선택합니다.

      - **콘트라스트 향상:** CLAHE 노드를 연결합니다. 조명이 고르지 않은 이미지의 국소 대비를 향상시킵니다.

      - **노이즈 제거:** Median Blur 노드에서 Kernel Size=5로 설정합니다. 미디언 블러는 엣지를 보존하면서 솔트&페퍼 노이즈를 효과적으로 제거합니다.

      - **이진화:** Adaptive Threshold 노드에서 Method=MeanC, Block Size=11, C=2로 설정합니다.

      - **다중 미리보기:** 각 노드의 미리보기를 통해 전처리 각 단계의 효과를 확인합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Convert Color | Conversion | BGR2GRAY | 단채널 변환 |
| Median Blur | Kernel Size | 5 | 홀수, 엣지 보존형 노이즈 제거 |
| Adaptive Threshold | Method | MeanC | 주변 평균 기반 적응 임계값 |
| Adaptive Threshold | Block Size | 11 | 로컬 영역 크기 (홀수) |
| Adaptive Threshold | C | 2.0 | 평균에서 차감할 상수 |
| Adaptive Threshold | Type | Binary | 이진 임계값 타입 |


#### 결과 해석


      CLAHE 처리 후 이미지는 조명 불균일이 보정되어 전체적으로 균일한 밝기 분포를 보입니다.
      Median Blur는 Gaussian Blur와 달리 엣지를 보존하므로 후속 이진화에 유리합니다.
      Adaptive Threshold는 고정 임계값과 달리 로컬 영역의 밝기를 기준으로 이진화하므로
      조명 그라디언트가 있는 이미지에서도 안정적인 결과를 생성합니다.


      Block Size를 크게 하면 더 넓은 영역의 평균을 참조하여 부드러운 이진화가 되고,
      작게 하면 세밀한 디테일을 보존합니다. C 값을 높이면 배경이 더 많이 흰색이 됩니다.


      이 파이프라인의 출력은 FindContours, TemplateMatch 등 후속 분석 노드의 입력으로 직접 사용할 수 있습니다.


> **참고:** **팁:** 전처리 파이프라인은 프로젝트 파일(.mvx)로 저장해두면 다른 검사에서 복사하여 재사용할 수 있습니다. 각 노드의 미리보기를 활용하여 단계별로 처리 효과를 확인하세요.


    예제 5: ROI 기반 영역 비교


#### 목적


      두 이미지의 특정 관심 영역(ROI)을 크롭하여 절대 차이(Abs Diff)를 계산하고,
      변화가 있는 부분을 검출하는 시스템을 구축합니다. 조립 전후 비교, 불량 검출 등에 활용됩니다.


      Crop 노드로 동일한 위치의 ROI를 추출한 뒤 Abs Diff로 차이를 계산하면
      픽셀 단위의 변화를 감지할 수 있습니다. 임계값을 적용하면 의미 있는 변화만 추출됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read (x2) | Input/Output | 비교할 두 이미지 로드 |
| Crop (x2) | Transform | 동일 ROI 영역 추출 |
| Abs Diff | Arithmetic | 두 ROI의 절대 차이 계산 |
| Threshold | Threshold | 차이 이진화 |
| Image Show | Input/Output | 차이 영역 시각화 |


#### 노드 파이프라인


        Image Read 1

        Crop 1
Transform


        &searr;


Arithmetic

Threshold


        Image Read 2

        Crop 2
Transform


        &nearr;


#### 구축 단계


      - **기준 이미지 로드:** 첫 번째 Image Read에 정상(기준) 이미지를, 두 번째에 검사 대상 이미지를 설정합니다.

      - **ROI 설정:** 두 Crop 노드에 동일한 X, Y, Width, Height 값을 설정합니다. 예: X=100, Y=80, Width=300, Height=200.

      - **차이 계산:** Abs Diff 노드의 Image1 입력에 Crop 1의 Result를, Image2 입력에 Crop 2의 Result를 연결합니다.

      - **이진화:** Threshold 노드에서 Threshold=30, Max Value=255, Type=Binary로 설정합니다.

      - **결과 확인:** Image Show로 차이가 있는 영역을 흰색으로 확인합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Crop 1, 2 | X / Y | 100 / 80 | 관심 영역 좌상단 좌표 (동일하게) |
| Crop 1, 2 | Width / Height | 300 / 200 | 관심 영역 크기 (동일하게) |
| Threshold | Threshold | 30 | 차이 임계값 (낮을수록 민감) |
| Threshold | Max Value | 255 | 이진화 최대값 |
| Threshold | Type | Binary | 이진 모드 |


#### 결과 해석


      Abs Diff의 출력은 두 이미지 간 픽셀 값의 절대 차이를 나타내는 그레이스케일 이미지입니다.
      동일한 영역은 검정(0), 차이가 큰 영역은 밝은 값으로 나타납니다.
      Threshold를 적용하면 의미 있는 변화 영역만 흰색으로 추출됩니다.


      Threshold 값이 너무 낮으면 미세한 노이즈까지 검출되고, 너무 높으면 실제 변화를 놓칩니다.
      일반적으로 20~50 범위에서 시작하여 조정합니다.


      이진화 결과에 Find Contours를 연결하면 변화 영역의 위치와 크기를 정량적으로 분석할 수 있습니다.


> **참고:** **팁:** 조명 변화에 의한 오검출을 줄이려면 두 이미지에 동일한 전처리(CLAHE, GaussianBlur)를 적용한 후 비교하세요. Defect Detector 노드를 사용하면 이 과정을 단일 노드로 수행할 수 있습니다.


## Chapter 34: 형상 분석 예제


    이 장에서는 윤곽선 분석, 형상 매칭, 치수 측정 등 형상 기반 검사 기법을 실습합니다.
    PCB 결함 검사, 나사 유무 확인, 자동 치수 측정, 라벨 정렬 검사, 표면 스크래치 검출 등
    제조 현장에서 자주 요구되는 5가지 응용 예제를 다룹니다.


    예제 6: PCB 결함 검사


#### 목적


      PCB 기판의 패턴을 기준 이미지(Golden Sample)와 비교하여 단선, 단락, 결손 등의 결함을 자동으로 검출합니다.
      Defect Detector 노드를 사용하면 기준 이미지와의 차이를 분석하여 결함 위치와 개수를 출력합니다.


      PCB 검사는 정밀한 위치 정렬이 전제됩니다. 실제 라인에서는 Template Match로 위치를 보정한 후
      비교하는 것이 일반적이지만, 이 예제에서는 정렬된 이미지를 전제로 결함 검출에 집중합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read (x2) | Input/Output | 검사 이미지 + 기준 이미지 로드 |
| Defect Detector | Inspection | 기준 대비 결함 검출 |
| Image Show | Input/Output | 결함 오버레이 시각화 |


#### 노드 파이프라인


        Image Read (검사)

        &searr;


Inspection


        Image Read (기준)

        &nearr;


#### 구축 단계


      - **이미지 로드:** 첫 번째 Image Read에 검사 대상 PCB 이미지를, 두 번째에 정상 기준 이미지를 설정합니다.

      - **결함 검출기 연결:** Defect Detector의 Image 입력에 검사 이미지를, Reference 입력에 기준 이미지를 연결합니다.

      - **파라미터 조정:** Blur Size=5, Threshold=30, Min Defect Area=50으로 설정합니다.

      - **결과 확인:** Result 출력을 Image Show에 연결합니다. Show Overlay=true로 결함 위치를 빨간색으로 표시합니다.

      - **판정:** Count 출력이 0이면 양품, 1 이상이면 불량으로 판정합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Defect Detector | Blur Size | 5 | 비교 전 노이즈 제거 블러 |
| Defect Detector | Threshold | 30 | 차이 임계값 (민감도 조절) |
| Defect Detector | Min Defect Area | 50 | 최소 결함 면적 (px) |
| Defect Detector | Morph Kernel | 3 | 모폴로지 노이즈 제거 크기 |
| Defect Detector | Show Overlay | true | 원본에 결함 오버레이 표시 |


#### 결과 해석


      실행 결과 검사 이미지 위에 결함 영역이 반투명 빨간색으로 오버레이되고, 각 결함에 빨간 테두리가 표시됩니다.
      좌측 상단에 "Defects: N" 형식으로 결함 수가 표시됩니다. Defects 출력 포트에서 각 결함의 바운딩 박스(Rect[])를 얻을 수 있으며,
      DiffMask 출력에서 차이 마스크 이미지를 확인할 수 있습니다.


      Threshold 값을 낮추면 미세한 차이도 결함으로 검출하지만 오검출이 증가하고,
      높이면 큰 결함만 검출합니다. Min Defect Area로 면적이 작은 노이즈를 필터링합니다.


      DiffMask를 Image Write로 저장하면 결함 분석 리포트에 활용할 수 있습니다.


> **참고:** **팁:** 두 이미지의 크기가 다르면 에러가 발생합니다. 크기가 다른 경우 Resize 노드로 동일 크기로 맞추거나, 촬영 조건을 통일하세요. Morph Kernel을 5로 높이면 작은 노이즈성 결함을 더 효과적으로 제거할 수 있습니다.


    예제 7: 나사 유무 확인


#### 목적


      조립 공정에서 나사가 올바르게 체결되었는지 확인하는 시스템을 구축합니다.
      Presence Checker 노드를 사용하여 나사 위치의 ROI에서 특정 패턴(나사 머리)의 존재 여부를 판정합니다.


      나사 머리는 주변보다 어둡거나 밝은 원형 패턴을 형성하므로, ROI 내에서 이진화 후
      흰색 픽셀의 비율(Fill Ratio)로 나사 존재 여부를 판별할 수 있습니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 조립품 이미지 로드 |
| Presence Checker | Inspection | 나사 위치 ROI에서 존재 여부 판정 |
| Image Show | Input/Output | 판정 결과 시각화 |


#### 노드 파이프라인


Inspection


#### 구축 단계


      - **이미지 로드:** Image Read로 나사가 있는/없는 조립품 이미지를 로드합니다.

      - **ROI 설정:** Presence Checker 노드에서 나사 위치에 맞게 ROI X, ROI Y, ROI Width, ROI Height를 설정합니다.

      - **임계값 설정:** Threshold를 128로 설정하고, 나사가 배경보다 어두우면 Invert Binary=true로 합니다.

      - **판정 기준:** Min Fill Ratio=0.3으로 설정하여 ROI 내 30% 이상이 나사 패턴이면 PASS (나사 있음)로 판정합니다.

      - **결과 확인:** Result 출력에 ROI 영역이 녹색(PASS) 또는 빨간색(FAIL)으로 표시됩니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Presence Checker | ROI X / Y | 나사 위치 | 나사 중심 좌표에 맞게 설정 |
| Presence Checker | ROI Width / Height | 50 / 50 | 나사 머리 크기보다 약간 크게 |
| Presence Checker | Threshold | 128 | 이진화 임계값 |
| Presence Checker | Invert Binary | 상황에 따라 | 나사가 어두우면 true |
| Presence Checker | Min Fill Ratio | 0.3 | 최소 채움 비율 (PASS 기준) |


#### 결과 해석


      ROI 영역이 반투명 녹색(PASS) 또는 빨간색(FAIL)으로 표시되고, "Fill: XX.X% [PASS/FAIL]" 라벨이 표시됩니다.
      FillRatio 출력에서 정확한 채움 비율을, Pass 출력에서 bool 판정 결과를 얻을 수 있습니다.


      여러 나사 위치를 검사하려면 Presence Checker 노드를 나사 수만큼 복제하여 각각 다른 ROI를 설정하고,
      모든 Pass 출력을 BooleanLogic 노드(AND)로 결합하면 전체 판정이 가능합니다.


      PixelCount 출력을 활용하면 나사의 체결 상태(깊이)에 따른 세밀한 판정도 가능합니다.


> **참고:** **팁:** ROI 크기를 나사 머리보다 10~20% 크게 설정하면 위치 오차에 대한 여유가 생깁니다. 복수 나사 검사 시 ForLoop + Crop 조합으로 자동화할 수도 있습니다.


    예제 8: 치수 자동 측정


#### 목적


      부품의 폭, 높이 등 치수를 자동으로 측정하는 시스템을 구축합니다.
      Object Measure 노드는 이미지에서 객체를 검출하고 최소 외접 회전 사각형(MinAreaRect)을 이용하여
      실제 치수를 계산합니다. Pixels Per Unit 설정으로 실측 단위(mm 등)로 변환할 수 있습니다.


      산업용 비전에서 치수 측정은 가장 기본적이면서도 중요한 기능입니다.
      카메라 캘리브레이션을 통해 Pixels Per Unit 값을 정확히 설정하면 서브픽셀 수준의 정밀 측정이 가능합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 부품 이미지 로드 |
| Object Measure | Measurement | 객체 검출 및 치수 측정 |
| Image Show | Input/Output | 측정 결과 시각화 |


#### 노드 파이프라인


Measurement


#### 구축 단계


      - **이미지 로드:** Image Read로 배경이 단색인 부품 이미지를 로드합니다.

      - **측정 노드 설정:** Object Measure 노드에서 Min Area=500으로 노이즈를 제거합니다.

      - **캘리브레이션:** 기준 길이가 알려진 객체로 Pixels Per Unit 값을 계산합니다. 예: 10mm 객체가 200px이면 Pixels Per Unit=20.0으로 설정합니다.

      - **단위 설정:** Unit Name을 "mm"으로 설정합니다.

      - **바이너리 조정:** 부품이 배경보다 어두우면 Invert Binary=true로 설정합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Object Measure | Blur Size | 5 | 전처리 블러 크기 |
| Object Measure | Min Area | 500 | 최소 객체 면적 |
| Object Measure | Pixels Per Unit | 20.0 | 픽셀/실측단위 비율 |
| Object Measure | Unit Name | mm | 표시 단위명 |
| Object Measure | Invert Binary | false | 어두운 객체 시 true |


#### 결과 해석


      결과 이미지에 각 객체의 최소 외접 회전 사각형이 녹색으로 표시되고, 중심에 "W x H mm" 형식의 치수 라벨이 표시됩니다.
      Widths와 Heights 출력 포트에서 각 객체의 측정값 배열을 얻을 수 있으며, Count에서 검출된 객체 수를 확인합니다.


      MinAreaRect를 사용하므로 기울어진 객체도 정확한 폭/높이를 측정할 수 있습니다.
      Width는 항상 Height 이상의 값(긴 변)으로 정규화됩니다.


      측정값을 Compare 노드에 연결하면 공차 범위 내 여부를 자동 판정할 수 있습니다.


> **참고:** **팁:** Pixels Per Unit을 정확히 설정하는 것이 측정 정밀도의 핵심입니다. 캘리브레이션 타겟(체커보드 등)을 촬영하여 알려진 치수로부터 정확한 비율을 계산하세요.


    예제 9: 라벨 정렬 검사


#### 목적


      제품에 부착된 라벨이 올바른 각도로 정렬되어 있는지 검사합니다.
      Alignment Checker 노드는 가장 큰 객체의 방향 각도를 측정하고 기대 각도와의 차이를 계산하여
      허용 오차 내인지 자동으로 판정합니다.


      라벨 정렬은 제품 외관 품질의 핵심 요소입니다. FitEllipse 또는 MinAreaRect를 사용하여
      객체의 주축 방향을 정밀하게 측정하며, 0.1도 단위의 각도 오차를 검출할 수 있습니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 라벨 이미지 로드 |
| Alignment Checker | Inspection | 정렬 각도 측정 및 판정 |
| Image Show | Input/Output | 정렬 결과 시각화 |


#### 노드 파이프라인


Inspection


#### 구축 단계


      - **이미지 로드:** Image Read로 라벨이 부착된 제품 이미지를 로드합니다.

      - **기대 각도 설정:** Alignment Checker의 Expected Angle을 0.0 (수평)으로 설정합니다.

      - **허용 오차:** Angle Tolerance를 5.0으로 설정하여 +/-5도 범위 내를 PASS로 판정합니다.

      - **최소 면적:** Min Area=1000으로 설정하여 라벨이 아닌 작은 객체를 무시합니다.

      - **바이너리 조정:** 라벨이 배경보다 어두우면 Invert Binary=true로 설정합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Alignment Checker | Expected Angle | 0.0 | 기대 방향 (-180 ~ 180) |
| Alignment Checker | Angle Tolerance | 5.0 | 허용 각도 편차 |
| Alignment Checker | Min Area | 1000 | 최소 객체 면적 |
| Alignment Checker | Blur Size | 5 | 전처리 블러 |
| Alignment Checker | Invert Binary | false | 밝은 라벨은 false |


#### 결과 해석


      결과 이미지에 검출된 객체의 윤곽선이 녹색(PASS) 또는 빨간색(FAIL)으로 표시되고,
      타원 또는 회전 사각형이 노란색으로 피팅됩니다. 중심에서 방향을 나타내는 주황색 선이 그려집니다.
      상단에 "Angle: XX.X deg (Error: XX.X deg) PASS/FAIL" 라벨이 표시됩니다.


      Angle 출력에서 검출된 각도, AngleError에서 기대 각도와의 편차, Pass에서 bool 판정을 얻습니다.


      여러 제품을 연속 검사할 때 AngleError의 추이를 모니터링하면 공정 드리프트를 조기에 감지할 수 있습니다.


> **참고:** **팁:** FitEllipse는 5개 이상의 컨투어 포인트가 필요합니다. 작은 객체나 단순한 형상은 MinAreaRect로 폴백됩니다. Angle Tolerance를 공정 관리 기준(Cpk)에 맞춰 설정하세요.


    예제 10: 표면 스크래치 검출


#### 목적


      금속이나 플라스틱 부품 표면의 스크래치(긁힘)를 자동으로 검출하는 시스템을 구축합니다.
      Scratch Detector 노드는 모폴로지 TopHat/BlackHat 연산으로 표면의 선형 결함을 강조하고,
      elongation(길이/폭 비율) 필터로 스크래치 형태의 결함만 선별합니다.


      표면 검사는 조명 설정이 핵심입니다. 측면 조명(Dark Field)을 사용하면 스크래치가
      밝게 드러나므로 검출이 용이합니다. 이 예제는 DarkOnLight/LightOnDark 두 모드를 모두 지원합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 표면 이미지 로드 |
| Scratch Detector | Inspection | 스크래치 검출 및 측정 |
| Image Show | Input/Output | 검출 결과 시각화 |


#### 노드 파이프라인


Inspection


#### 구축 단계


      - **이미지 로드:** Image Read로 표면 이미지를 로드합니다.

      - **검출 모드 선택:** Scratch Detector의 Detect Mode를 DarkOnLight (밝은 표면 위의 어두운 스크래치) 또는 LightOnDark (어두운 표면 위의 밝은 스크래치)로 설정합니다.

      - **민감도 조정:** Morph Kernel Size=15 (스크래치 폭보다 큰 값), Threshold=30으로 설정합니다.

      - **필터 설정:** Min Length=30으로 짧은 노이즈를 제거하고, Min Elongation=3.0으로 선형 형태만 선별합니다.

      - **결과 확인:** Count 출력에서 스크래치 수, TotalLength에서 총 스크래치 길이를 확인합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Scratch Detector | Detect Mode | DarkOnLight | 밝은 배경 위 어두운 스크래치 |
| Scratch Detector | Morph Kernel Size | 15 | 스크래치보다 큰 커널 (홀수) |
| Scratch Detector | Threshold | 30 | 모폴로지 결과 이진화 임계값 |
| Scratch Detector | Min Length | 30 | 최소 스크래치 길이 (px) |
| Scratch Detector | Min Elongation | 3.0 | 최소 길이/폭 비율 |
| Scratch Detector | Blur Size | 3 | 전처리 블러 크기 |


#### 결과 해석


      검출된 스크래치 영역이 반투명 빨간색으로 오버레이되고, 윤곽선이 빨간 테두리로 표시됩니다.
      상단에 "Scratches: N Total Length: XXXpx" 형식의 요약 정보가 표시됩니다.
      ScratchMask 출력에서 바이너리 마스크를 얻어 추가 분석에 활용할 수 있습니다.


      BlackHat 연산(DarkOnLight 모드)은 closing - original을 계산하여 밝은 배경에서 어두운 선형 구조를 강조합니다.
      TopHat 연산(LightOnDark 모드)은 original - opening으로 어두운 배경의 밝은 구조를 강조합니다.


      TotalLength를 기준으로 허용 한도를 설정하면 경미한 스크래치는 허용하고 심각한 결함만 불량 처리할 수 있습니다.


> **참고:** **팁:** Morph Kernel Size는 검출하려는 스크래치의 폭보다 커야 합니다. 너무 작으면 스크래치가 모폴로지 연산에 의해 제거되지 않아 강조되지 않고, 너무 크면 넓은 결함까지 강조됩니다.


## Chapter 35: 제조 검사 예제

  이 장에서는 실제 제조 라인에서 활용되는 고급 검사 시나리오를 구축합니다. 원형 부품 외경 측정, 패턴 매칭 기반 조립 확인, 컨베이어 벨트 카운팅, 색상 일관성 검사, 복합 결함 검사 등 산업 현장의 다양한 요구사항에 대응하는 파이프라인을 설계합니다.


    예제 11: 원형 부품 외경 측정


#### 목적

    원형 부품(베어링, 와셔, O-링 등)의 외경을 자동으로 측정합니다. Hough Circles 노드로 원을 검출하고, 검출된 반지름에서 외경을 계산합니다. Pixels Per Unit 환산을 통해 실측 치수를 출력합니다.


    허프 원 변환은 이미지 내의 원형 객체를 강건하게 검출할 수 있으며, 부분적으로 가려진 원이나 노이즈가 있는 환경에서도 안정적인 결과를 제공합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 원형 부품 이미지 로드 |
| Convert Color | Color | BGR → Grayscale 변환 |
| Gaussian Blur | Filter | 노이즈 제거 |
| Hough Circles | Detection | 원 검출 (중심, 반지름) |
| Image Show | Input/Output | 검출 결과 시각화 |


#### 노드 파이프라인


Color

Filter

Detection


#### 구축 단계


      - **이미지 로드:** Image Read로 원형 부품 이미지를 로드합니다.

      - **전처리:** Convert Color로 그레이스케일 변환 후 Gaussian Blur (Kernel=5)로 노이즈를 제거합니다.

      - **원 검출:** Hough Circles에서 Dp=1.0, Min Distance=50, Param1=200, Param2=100으로 설정합니다.

      - **반지름 범위:** 부품 크기에 맞게 Min Radius=30, Max Radius=150으로 설정합니다.

      - **외경 계산:** 검출된 원의 반지름 x 2 / PixelsPerUnit으로 실제 외경을 계산합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Hough Circles | Dp | 1.0 | 해상도 역비율 (1=원본) |
| Hough Circles | Min Distance | 50 | 원 중심 간 최소 거리 |
| Hough Circles | Param 1 | 200 | Canny 상한 임계값 |
| Hough Circles | Param 2 | 100 | 누적기 임계값 |
| Hough Circles | Min/Max Radius | 30/150 | 검출 반지름 범위 |


#### 결과 해석

    결과 이미지에 검출된 원이 녹색으로 표시되고 중심에 빨간 점이 찍힙니다. Param2를 낮추면 더 많은 원을 검출하지만 오검출이 증가합니다. Min/Max Radius로 대상 크기를 제한하면 정확도가 향상됩니다.


    여러 부품의 외경을 동시에 측정하려면 검출된 모든 원의 반지름 배열을 분석합니다. Min Distance를 부품 직경 이상으로 설정하면 중복 검출을 방지합니다.


    Gaussian Blur를 전처리로 적용하면 노이즈에 의한 오검출을 크게 줄일 수 있습니다.


> **참고:** **팁:** Hough Circles 내부에서 Canny를 사용하므로 별도의 Canny Edge 노드는 불필요합니다. Max Radius=0으로 설정하면 반지름 상한 제한 없이 검출합니다.


    예제 12: 패턴 매칭 기반 조립 확인


#### 목적

    조립 공정에서 특정 부품이 올바른 위치에 올바른 수량으로 조립되었는지 확인합니다. Pattern Matcher 노드는 템플릿 이미지를 사용하여 검사 이미지 내 모든 일치 위치를 찾고, NMS로 중복을 제거한 후 기대 수량과 비교하여 PASS/FAIL을 판정합니다.


    Template Match와 달리 Pattern Matcher는 다중 매칭과 자동 판정 기능을 내장하여 조립 검사에 적합합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read (검사) | Input/Output | 조립품 이미지 로드 |
| Image Read (템플릿) | Input/Output | 부품 템플릿 이미지 로드 |
| Pattern Matcher | Inspection | 다중 패턴 매칭 및 판정 |
| Image Show | Input/Output | 매칭 결과 시각화 |


#### 노드 파이프라인


        Image Read (검사)

        &searr;


Inspection


        Image Read (템플릿)

        &nearr;


#### 구축 단계


      - **이미지 로드:** 조립품 전체 이미지와 찾을 부품의 템플릿 이미지를 설정합니다.

      - **매칭 설정:** Pattern Matcher의 Image에 조립품, Template에 부품 템플릿을 연결합니다.

      - **매칭 임계값:** Match Threshold=0.8 (80% 이상 유사도).

      - **기대 수량:** Expected Min=4, Expected Max=4로 설정하면 정확히 4개 시 PASS.

      - **NMS 설정:** NMS Overlap=0.3으로 중복 검출을 제거합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Pattern Matcher | Match Threshold | 0.8 | 최소 매칭 유사도 |
| Pattern Matcher | Expected Min/Max | 4/4 | 기대 매칭 수 |
| Pattern Matcher | NMS Overlap | 0.3 | 중복 제거 IoU 임계값 |


#### 결과 해석

    매칭 위치에 녹색 사각형이 표시되고 이미지에 PASS/FAIL 테두리가 그려집니다. Matches 출력에서 좌표 배열, Count에서 매칭 수, Pass에서 판정 결과를 얻습니다.


    Match Threshold 0.7~0.9 범위가 일반적입니다. CCoeffNormed 방식으로 밝기 변화에 강인하지만 크기/회전 변화에는 대응하지 못합니다.


    템플릿은 검사 이미지에서 크롭하여 만드는 것이 가장 정확합니다.


> **참고:** **팁:** 템플릿 이미지 크기가 검사 이미지보다 작아야 합니다. 배경이 포함되지 않도록 부품 영역만 정확히 크롭하세요.


    예제 13: 컨베이어 벨트 개수 카운팅


#### 목적

    컨베이어 벨트 위의 부품 개수를 실시간으로 카운팅합니다. Object Counter 노드는 이미지 내 객체를 자동으로 검출하고 번호를 매기며, 각 객체의 중심 좌표와 면적을 출력합니다.


    Camera 노드와 Stream 모드(F6)를 결합하면 실시간 카운팅이 가능합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 테스트 이미지 (실시간: Camera 대체) |
| Object Counter | Inspection | 객체 검출 및 카운팅 |
| Image Show | Input/Output | 카운팅 결과 시각화 |


#### 노드 파이프라인


Inspection


#### 구축 단계


      - **이미지 소스:** 테스트 시 Image Read, 실시간에는 Camera 노드를 사용합니다.

      - **카운터 설정:** Object Counter에서 Invert Binary=true (밝은 배경 + 어두운 부품).

      - **면적 필터:** Min Area=200, Max Area=10000000으로 노이즈를 제거합니다.

      - **모폴로지:** Morph Kernel Size=3으로 작은 끊김을 보정합니다.

      - **결과 확인:** Count, Centers, Areas 출력으로 결과를 확인합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Object Counter | Blur Size | 5 | 전처리 블러 |
| Object Counter | Invert Binary | true | 밝은 배경 + 어두운 객체 |
| Object Counter | Min Area | 200 | 최소 면적 |
| Object Counter | Morph Kernel Size | 3 | Close 연산 커널 |


#### 결과 해석

    각 객체에 녹색 윤곽선, 빨간 중심점, "#N" 번호가 표시되고 좌측 상단에 "Count: N"이 표시됩니다. Areas 출력으로 비정상 크기 객체(겹침/파손)를 추가 검출할 수 있습니다.


    접촉/겹침 부품은 하나로 카운팅되므로, 이 경우 Watershed 노드를 전처리에 추가합니다.


    Use Adaptive Threshold=true를 설정하면 조명 불균일 환경에서 더 안정적입니다.


> **참고:** **팁:** 겹친 부품 분리에는 Watershed 노드(예제 18)를 참조하세요. ForLoop로 프레임별 카운트를 누적하면 총 통과량을 추적할 수 있습니다.


    예제 14: 색상 일관성 검사


#### 목적

    제품의 색상이 기준 범위 내에 있는지 검사합니다. Convert Color로 HSV 변환 후 InRange로 허용 색상 범위를 마스킹하고, 마스크 내 픽셀 비율로 색상 일관성을 판정합니다.


    도장, 인쇄, 식품 등에서 색상 품질 관리는 핵심입니다. HSV 색공간에서 Hue 범위를 정밀하게 설정하면 미세한 색상 차이도 검출합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 제품 이미지 로드 |
| Convert Color | Color | BGR → HSV 변환 |
| In Range | Color | 허용 색상 범위 마스킹 |
| Find Contours | Contour | 마스크 영역 분석 |
| Contour Filter | Contour | 노이즈 영역 제거 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


Color

Color


      &darr;


Contour

Contour


#### 구축 단계


      - **이미지 로드:** Image Read로 컬러 제품 이미지를 로드합니다.

      - **HSV 변환:** Convert Color에서 BGR2HSV를 선택합니다.

      - **색상 범위:** In Range에서 파란 제품은 Lower H=100, S=50, V=50 / Upper H=130, S=255, V=255.

      - **마스크 분석:** Find Contours + Contour Filter로 허용 범위 내 영역의 면적을 계산합니다.

      - **판정:** 전체 대비 허용 색상 영역 비율이 95% 이상이면 PASS입니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Convert Color | Conversion | BGR2HSV | HSV 색공간 변환 |
| In Range | Lower/Upper H | 100/130 | 파란색 Hue 범위 |
| In Range | Lower/Upper S | 50/255 | 채도 범위 |
| In Range | Lower/Upper V | 50/255 | 밝기 범위 |


#### 결과 해석

    In Range의 Mask 출력은 허용 범위 내 픽셀이 흰색인 바이너리 이미지입니다. 흰색 비율이 높을수록 색상 일관성이 좋습니다. 색상 이상 영역은 검정으로 나타납니다.


    Color Object Detector를 사용하면 허용 범위 외의 이상 색상 영역을 직접 검출하고 시각화할 수 있습니다.


    양품 샘플로부터 HSV 분포를 측정하여 평균 +/- 3*표준편차 범위를 기준으로 설정하면 통계적으로 안정적입니다.


> **참고:** **팁:** OpenCV HSV에서 H 범위는 0~179입니다. 빨간색은 H=0 부근과 H=170 부근에 걸쳐 있으므로 두 개의 In Range를 사용하거나 BitwiseOr로 합치는 방식을 고려하세요.


    예제 15: 복합 결함 검사


#### 목적

    하나의 파이프라인에서 여러 유형의 결함(스크래치, 결손, 색상 이상)을 동시에 검사하는 복합 검사 시스템을 구축합니다. Defect Detector와 Scratch Detector를 병렬로 실행하고, 두 결과를 종합하여 최종 판정합니다.


    실무에서는 단일 검사로는 불충분하며, MVXTester의 노드 기반 아키텍처는 병렬 검사를 자연스럽게 구현합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read (x2) | Input/Output | 검사 + 기준 이미지 |
| Defect Detector | Inspection | 영역 결함 검출 |
| Scratch Detector | Inspection | 선형 결함 검출 |
| Compare (x2) | Control | 각 검사 판정 |
| Boolean Logic | Control | AND 종합 판정 |


#### 노드 파이프라인


Inspection

        Compare (==0)
Control


        &searr;


        Boolean Logic (AND)
Control


Inspection

        Compare (==0)
Control


        &nearr;


#### 구축 단계


      - **이미지 준비:** 검사 이미지와 기준 이미지를 각각 Image Read로 로드합니다.

      - **결함 검출 분기 1:** Defect Detector로 기준 대비 영역 결함을 검출합니다.

      - **결함 검출 분기 2:** 동일 이미지를 Scratch Detector에 연결합니다.

      - **개별 판정:** 각 Count 출력을 Compare (Operator=Equal, B=0)에 연결하여 결함 수 0일 때 true.

      - **종합 판정:** 두 Compare를 Boolean Logic (AND)로 결합합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Defect Detector | Threshold | 30 | 차이 감지 민감도 |
| Defect Detector | Min Defect Area | 50 | 최소 결함 크기 |
| Scratch Detector | Min Length | 30 | 최소 스크래치 길이 |
| Compare | Operator | Equal | Count == 0 |
| Boolean Logic | Operation | AND | 모든 검사 통과 |


#### 결과 해석

    각 검출기의 Result에서 해당 결함 유형의 시각화를 확인합니다. Boolean Logic 출력이 true이면 양품, false이면 불량입니다.


    검사 항목 추가 시 새로운 검출기 + Compare를 추가하여 Boolean Logic에 연결합니다. 각 Count 값을 기록하면 결함 유형별 통계 분석이 가능합니다.


    무거운 검사보다 가벼운 검사를 먼저 실행하여 빠른 불량 배제를 고려합니다.


> **참고:** **팁:** 복합 검사에서 각 분기의 실행 시간을 모니터링하여 병목을 파악하세요. 가장 느린 검사가 전체 사이클 타임을 결정합니다.


## Chapter 36: 품질 관리 예제

  이 장에서는 품질 관리(QC) 시스템에 특화된 고급 예제를 다룹니다. 히스토그램 모니터링, 적응형 임계값 자동 조정, 워터셰드 기반 객체 분리, 다중 ROI 동시 검사, 자동 OK/NG 판정 시스템 등 실시간 품질 관리 핵심 기법을 학습합니다.


    예제 16: 실시간 히스토그램 모니터링


#### 목적

    카메라 입력의 밝기 분포를 실시간으로 모니터링하여 조명 변화나 이상을 감지합니다. Calc Histogram으로 히스토그램을 계산하고, Brightness Uniformity로 밝기 균일성을 판정합니다.


    히스토그램 모니터링은 비전 시스템의 안정성을 보장하는 기본 QC 기법입니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 이미지 로드 |
| Calc Histogram | Histogram | 히스토그램 계산 |
| Brightness Uniformity | Inspection | 밝기 균일성 분석 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


Histogram


      &darr; (Image Read 분기)


Inspection


#### 구축 단계


      - **이미지 소스:** Image Read 또는 Camera 노드를 배치합니다.

      - **히스토그램:** Calc Histogram 노드를 연결합니다. 미리보기에서 밝기 분포를 확인합니다.

      - **균일성 분석:** Brightness Uniformity에서 Grid Columns=4, Grid Rows=4로 분할합니다.

      - **판정 기준:** Max Std Dev=30으로 설정합니다.

      - **결과 확인:** Result에 그리드 오버레이와 각 셀의 평균 밝기가 표시됩니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Brightness Uniformity | Grid Columns | 4 | 가로 분할 수 |
| Brightness Uniformity | Grid Rows | 4 | 세로 분할 수 |
| Brightness Uniformity | Max Std Dev | 30 | 허용 최대 표준편차 |


#### 결과 해석

    그리드 오버레이에 각 셀 밝기가 표시되고, 균일 셀은 녹색, 불균일 셀은 빨간색입니다. 상단 배너에 "PASS - UNIFORM" 또는 "FAIL - NON-UNIFORM"이 표시됩니다.


    MeanBrightness, StdDev, MinBrightness, MaxBrightness, IsUniform 출력을 통해 상세 통계를 얻습니다. Calc Histogram 미리보기로 과노출/저노출/낮은 대비를 직관적으로 파악할 수 있습니다.


    Max Std Dev 기준값은 초기 양품 이미지로부터 실측하여 설정하는 것이 좋습니다.


> **참고:** **팁:** 그리드를 3x3이나 5x5로 변경하여 분석 해상도를 조정할 수 있습니다. Stream 모드에서 지속적으로 모니터링하면 조명 열화를 조기에 감지할 수 있습니다.


    예제 17: 어댑티브 임계값 자동 조정


#### 목적

    조명이 불균일한 환경에서 안정적인 이진화를 수행합니다. Otsu Threshold와 Adaptive Threshold를 비교하여 각각의 장단점을 이해하고 최적의 이진화 전략을 선택합니다.


    Otsu는 전체 히스토그램 기반 자동 임계값을, Adaptive는 로컬 영역별 임계값을 계산합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 조명 불균일 이미지 |
| Convert Color | Color | 그레이스케일 변환 |
| Otsu Threshold | Threshold | 전역 자동 임계값 |
| Adaptive Threshold | Threshold | 로컬 적응 임계값 |
| Image Show (x2) | Input/Output | 비교 시각화 |


#### 노드 파이프라인


Color


      &darr; (분기)


Threshold


        |
Threshold


#### 구축 단계


      - **이미지 로드:** 조명 그라디언트가 있는 이미지를 로드합니다.

      - **그레이스케일:** Convert Color로 BGR2GRAY 변환합니다.

      - **Otsu 분기:** Otsu Threshold에 연결합니다. Max Value=255.

      - **Adaptive 분기:** Adaptive Threshold에 연결합니다. Method=GaussianC, Block Size=15, C=3.

      - **비교:** 두 Image Show에서 결과를 비교합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Otsu Threshold | Max Value | 255 | 이진화 최대값 |
| Adaptive Threshold | Method | GaussianC | 가우시안 가중 평균 |
| Adaptive Threshold | Block Size | 15 | 로컬 영역 크기 (홀수) |
| Adaptive Threshold | C | 3.0 | 평균 차감 상수 |


#### 결과 해석

    조명 균일 이미지에서는 Otsu와 Adaptive가 유사하지만, 조명 그라디언트 이미지에서는 Otsu가 한쪽에서 실패하는 반면 Adaptive는 전체에서 안정적입니다.


    Block Size를 크게 하면 넓은 밝기 변화에 적응하고, 작게 하면 세밀한 디테일을 보존합니다. C 값이 클수록 배경이 확장됩니다.


    IfSelect로 조건에 따라 두 방법을 동적 전환하는 전략도 사용됩니다.


> **참고:** **팁:** Adaptive Threshold는 그레이스케일 입력이 필요합니다. 컬러 이미지는 내부에서 자동 변환하지만 명시적으로 Convert Color를 사용하는 것이 권장됩니다.


    예제 18: 워터셰드 기반 겹침 객체 분리


#### 목적

    접촉하거나 겹쳐 있는 객체들을 워터셰드 알고리즘으로 분리합니다. Watershed 노드는 Distance Transform 기반 자동 마커를 생성하여 분할합니다.


    약, 부품, 식품 등의 카운팅에서 겹침 객체 분리는 흔한 문제이며, 워터셰드로 정확한 경계 분할이 가능합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 겹침 객체 이미지 |
| Watershed | Segmentation | 워터셰드 분할 |
| Image Show | Input/Output | 분할 결과 시각화 |


#### 노드 파이프라인


Segmentation


#### 구축 단계


      - **이미지 로드:** 접촉/겹침 객체 이미지를 로드합니다.

      - **워터셰드:** Watershed 노드에 연결합니다. Markers 입력을 비워두면 자동 마커가 생성됩니다.

      - **임계값:** Distance Threshold=0.5로 설정합니다.

      - **결과 확인:** 각 분할 영역이 다른 색상으로 표시되고 경계선이 흰색입니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Watershed | Distance Threshold | 0.5 | 거리 변환 임계값 비율 (0~1) |


#### 결과 해석

    각 분할 영역이 고유 색상으로 칠해지고 경계선은 흰색입니다. 분할 영역 수를 세면 겹침을 고려한 정확한 객체 수를 얻습니다.


    Distance Threshold가 높으면(0.7~0.9) 분리가 강해지지만 작은 객체 누락 가능성이 있고, 낮으면(0.2~0.4) 과분할이 발생할 수 있습니다.


    Markers 입력에 직접 마커 이미지를 제공하면 사용자 정의 분할이 가능합니다.


> **참고:** **팁:** Watershed는 3채널 BGR 이미지를 필요로 합니다. 그레이스케일은 자동 변환됩니다. 이진화된 이미지 입력 시 Distance Transform이 올바르게 작동합니다.


    예제 19: 다중 ROI 동시 검사


#### 목적

    하나의 이미지에서 여러 ROI를 동시에 검사합니다. 각 ROI에 Presence Checker를 배치하여 조립 상태를 확인하고, 모든 ROI가 PASS여야 최종 양품으로 판정합니다.


    PCB 검사, 커넥터 검사, 다점 조립 확인 등에서 필수적인 패턴입니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 검사 이미지 |
| Presence Checker (x3) | Inspection | 각 ROI 존재 여부 검사 |
| Boolean Logic (x2) | Control | AND 체인 종합 판정 |


#### 노드 파이프라인


        Presence Checker 1
Inspection

Control


        Presence Checker 2
Inspection


        &nearr;


        Presence Checker 3
Inspection

Control


#### 구축 단계


      - **이미지 로드:** Image Read로 다중 검사 위치 이미지를 로드합니다.

      - **ROI 설정:** 각 Presence Checker에 서로 다른 ROI 좌표를 설정합니다.

      - **판정 기준:** 각 Min Fill Ratio=0.3으로 설정합니다.

      - **종합 판정:** 3개의 Pass를 Boolean Logic (AND) 체인으로 연결합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Presence Checker 1~3 | ROI X/Y/W/H | 각 위치별 | 검사 위치 좌표 |
| Presence Checker 1~3 | Min Fill Ratio | 0.3 | 존재 판정 기준 |
| Boolean Logic | Operation | AND | 모든 검사 통과 |


#### 결과 해석

    각 Presence Checker Result에 해당 ROI가 녹색(PASS) 또는 빨간색(FAIL)으로 표시됩니다. Boolean Logic 최종 출력이 true이면 전체 양품, false이면 누락 불량입니다.


    검사 위치가 많은 경우 ForLoop + 좌표 배열을 활용하여 동적 ROI 생성이 가능합니다.


    ROI 크기를 대상보다 10~20% 크게 설정하면 위치 오차에 대한 여유가 생깁니다.


> **참고:** **팁:** Mouse Event 노드로 ROI 위치를 대화형으로 설정한 뒤, 해당 좌표를 Presence Checker 속성에 입력하면 편리합니다.


    예제 20: 자동 OK/NG 판정


#### 목적

    비전 검사 결과를 자동으로 OK/NG로 판정하고 결과를 시각적으로 표시하는 종합 판정 시스템을 구축합니다. Object Measure로 치수를 측정하고, 공차 범위 내인지 Compare + Boolean Logic으로 판정합니다.


    측정-비교-판정-출력의 완전한 자동 검사 흐름을 보여줍니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 부품 이미지 |
| Object Measure | Measurement | 치수 자동 측정 |
| Compare (x2) | Control | 상한/하한 비교 |
| Boolean Logic | Control | AND 범위 판정 |
| If Select | Control | OK/NG 선택 출력 |


#### 노드 파이프라인


Measurement


      &darr;


        Compare (≥하한)
Control

        Boolean Logic (AND)
Control

Control


        Compare (≤상한)
Control


        &nearr;


#### 구축 단계


      - **측정:** Image Read + Object Measure로 부품 치수를 측정합니다.

      - **하한 비교:** Compare 1: A=측정값, B=9.5, Operator=GreaterOrEqual.

      - **상한 비교:** Compare 2: A=측정값, B=10.5, Operator=LessOrEqual.

      - **범위 판정:** Boolean Logic (AND)로 두 비교를 결합합니다.

      - **결과 출력:** If Select의 Condition에 AND 결과를 연결합니다. True Value="OK", False Value="NG".


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Object Measure | Pixels Per Unit | 20.0 | 캘리브레이션 비율 |
| Compare 1 | Operator | GreaterOrEqual | 측정값 ≥ 하한 |
| Compare 2 | Operator | LessOrEqual | 측정값 ≤ 상한 |
| Boolean Logic | Operation | AND | 범위 내 판정 |


#### 결과 해석

    Object Measure Result에 측정 치수가 표시됩니다. Boolean Logic이 true이면 공차 내 양품, false이면 공차 초과 불량입니다. If Select에서 "OK" 또는 "NG" 문자열을 출력합니다.


    여러 객체의 치수를 모두 검사하려면 ForEach 루프를 활용합니다. 측정값을 CsvReader로 기록하거나 시리얼 통신으로 PLC에 전송하여 자동 라인을 구축할 수 있습니다.


    공차 범위는 도면 사양의 +/- 허용치를 기준으로 설정합니다.


> **참고:** **팁:** 기준 10.0mm, 공차 +/-0.5mm이면 하한 9.5, 상한 10.5로 설정합니다. Pixels Per Unit 캘리브레이션이 측정 정밀도의 핵심입니다.


## Chapter 37: MediaPipe 예제

  이 장에서는 MediaPipe 기반 AI 노드를 활용한 응용 예제를 다룹니다. 얼굴 검출, 손동작 인식, 포즈 추정, 얼굴 메시, 배경 제거 등 딥러닝 기반 비전 기능을 노드 파이프라인으로 간편하게 구축합니다. 모든 MediaPipe 노드는 ONNX 모델 기반으로 동작하며, 별도의 Python 환경 없이 C# 내에서 실행됩니다.


    예제 21: 실시간 얼굴 검출


#### 목적

    MediaPipe BlazeFace 모델을 사용하여 이미지 또는 카메라 영상에서 실시간으로 얼굴을 검출합니다. MP Face Detection 노드는 얼굴의 바운딩 박스, 신뢰도 점수, 검출 수를 출력합니다.


    출입 관리, 안전 모니터링, 사용자 인터페이스 등에서 얼굴 검출은 기본이 되는 기능입니다. BlazeFace 모델은 경량화되어 실시간 처리가 가능합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 테스트 이미지 (실시간: Camera) |
| MP Face Detection | MediaPipe | BlazeFace 얼굴 검출 |
| Image Show | Input/Output | 검출 결과 시각화 |


#### 노드 파이프라인


MediaPipe


#### 구축 단계


      - **이미지 소스:** Image Read로 사람 얼굴이 포함된 이미지를 로드합니다. 실시간에는 Camera 노드를 사용합니다.

      - **검출 설정:** MP Face Detection 노드를 연결합니다. Confidence=0.5로 설정합니다.

      - **최대 검출 수:** Max Detections=10으로 설정합니다.

      - **결과 확인:** Result를 Image Show에 연결합니다. 검출된 얼굴에 "Face" 라벨과 바운딩 박스가 표시됩니다.

      - **출력 활용:** Faces(Rect[]), Scores(double[]), Count(int) 출력을 후속 처리에 활용합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| MP Face Detection | Confidence | 0.5 | 최소 검출 신뢰도 (0~1) |
| MP Face Detection | Max Detections | 10 | 최대 검출 수 |


#### 결과 해석

    검출된 얼굴마다 녹색 바운딩 박스와 "Face" 라벨이 표시됩니다. Confidence를 낮추면 더 많은 얼굴을 검출하지만 오검출 가능성이 높아지고, 높이면 확실한 얼굴만 검출합니다.


    Faces 출력의 Rect 배열에서 각 얼굴의 위치(X, Y, Width, Height)를 얻어 Crop 노드로 얼굴 영역만 추출할 수 있습니다.


    ONNX 모델 파일(face_detection_short_range.onnx)이 Models/MediaPipe/ 폴더에 있어야 합니다.


> **참고:** **팁:** 측면이나 기울어진 얼굴의 검출률을 높이려면 Confidence를 0.3~0.4로 낮추세요. 모델 파일이 없으면 에러 메시지에 다운로드 경로가 안내됩니다.


    예제 22: 손동작 인식


#### 목적

    MediaPipe Hand Landmark 모델로 손의 21개 랜드마크를 검출하여 손동작을 인식합니다. 손바닥 검출 후 개별 손가락 관절 위치를 추출하며, 스켈레톤 시각화를 제공합니다.


    제스처 인터페이스, 수화 인식, HRI(Human-Robot Interaction) 등에 활용됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 손 이미지 로드 |
| MP Hand Landmark | MediaPipe | 21개 손 랜드마크 검출 |
| Image Show | Input/Output | 스켈레톤 시각화 |


#### 노드 파이프라인


MediaPipe


#### 구축 단계


      - **이미지 소스:** Image Read 또는 Camera 노드로 손이 보이는 이미지를 로드합니다.

      - **랜드마크 검출:** MP Hand Landmark 노드를 연결합니다. Confidence=0.5, Max Hands=2.

      - **스켈레톤 표시:** Draw Skeleton=true로 설정하면 손가락 관절 연결선이 표시됩니다.

      - **결과 확인:** Result에 손 스켈레톤이 표시되고, Landmarks에서 21개 좌표를 얻습니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| MP Hand Landmark | Confidence | 0.5 | 최소 검출 신뢰도 |
| MP Hand Landmark | Max Hands | 2 | 최대 손 수 |
| MP Hand Landmark | Draw Skeleton | true | 스켈레톤 연결선 표시 |


#### 결과 해석

    검출된 각 손에 21개 랜드마크가 녹색 점과 연결선으로 표시됩니다. 상단에 "Hands: N" 또는 "No hands detected"가 표시됩니다. Landmarks 출력에서 모든 손의 랜드마크 좌표를 얻습니다.


    랜드마크 인덱스: 0=손목, 1-4=엄지, 5-8=검지, 9-12=중지, 13-16=약지, 17-20=소지. 손가락 끝(4,8,12,16,20)의 위치로 제스처를 판별할 수 있습니다.


    두 단계 파이프라인(손바닥 검출 + 랜드마크 추출)으로 동작하므로 모델 파일 2개(palm_detection.onnx, hand_landmark.onnx)가 필요합니다.


> **참고:** **팁:** 손가락 펼침/접힘을 판별하려면 각 손가락 끝 랜드마크의 Y 좌표와 해당 손가락 PIP 관절 Y 좌표를 비교합니다. 끝이 PIP보다 위(작은 Y)이면 펼침, 아래면 접힘입니다.


    예제 23: 포즈 추정 자세 분석


#### 목적

    MediaPipe BlazePose 모델로 전신 33개 포즈 랜드마크를 검출하여 자세를 분석합니다. 어깨, 팔꿈치, 손목, 엉덩이, 무릎, 발목 등의 관절 위치를 추출하여 자세 교정, 운동 분석, 안전 모니터링에 활용합니다.


    작업자 자세 모니터링, 스포츠 과학, 재활 의료 등 다양한 분야에서 활용됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 전신 이미지 로드 |
| MP Pose Landmark | MediaPipe | 33개 포즈 랜드마크 검출 |
| Image Show | Input/Output | 포즈 스켈레톤 시각화 |


#### 노드 파이프라인


MediaPipe


#### 구축 단계


      - **이미지 소스:** 전신이 보이는 이미지를 로드합니다.

      - **포즈 검출:** MP Pose Landmark 노드를 연결합니다. Confidence=0.5.

      - **시각화:** Draw Skeleton=true, Draw Labels=true로 설정합니다.

      - **결과 확인:** 33개 관절점과 연결 스켈레톤이 표시됩니다.

      - **자세 분석:** Landmarks 출력에서 관절 좌표를 추출하여 각도를 계산합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| MP Pose Landmark | Confidence | 0.5 | 최소 검출 신뢰도 |
| MP Pose Landmark | Draw Skeleton | true | 스켈레톤 표시 |
| MP Pose Landmark | Draw Labels | true | 관절명 라벨 표시 |


#### 결과 해석

    33개 랜드마크가 스켈레톤으로 연결되어 표시됩니다. 주요 인덱스: 0=코, 11/12=왼/오른 어깨, 13/14=팔꿈치, 15/16=손목, 23/24=엉덩이, 25/26=무릎, 27/28=발목.


    Visibility 출력에서 각 랜드마크의 가시성을 확인할 수 있어, 가려진 관절을 필터링할 수 있습니다.


    관절 간 각도를 계산하여 "바른 자세" 판정을 구현할 수 있습니다. 예: 어깨-팔꿈치-손목 각도로 팔 굽힘을 분석합니다.


> **참고:** **팁:** 전신이 프레임에 완전히 들어와야 정확한 검출이 가능합니다. 상반신만 보이면 하반신 랜드마크의 정확도가 떨어집니다. 모델 파일: pose_detection.onnx, pose_landmark_full.onnx가 필요합니다.


    예제 24: 얼굴 메시 표정 분석


#### 목적

    MediaPipe Face Mesh 모델로 468개의 얼굴 랜드마크를 검출하여 얼굴의 세밀한 형태를 분석합니다. 눈, 입, 눈썹, 턱선 등의 정밀한 위치를 추출하여 표정 분석, AR 필터, 뷰티 앱 등에 활용합니다.


    얼굴 메시는 3D 얼굴 모델링, 감정 인식, 시선 추적 등의 기반이 됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 얼굴 이미지 로드 |
| MP Face Mesh | MediaPipe | 468개 얼굴 랜드마크 검출 |
| Image Show | Input/Output | 메시 시각화 |


#### 노드 파이프라인


MediaPipe


#### 구축 단계


      - **이미지 소스:** 정면 얼굴 이미지를 로드합니다.

      - **메시 검출:** MP Face Mesh 노드를 연결합니다. Confidence=0.5.

      - **시각화 설정:** Draw Contours=true로 얼굴 윤곽선을, Draw Points=false로 개별 점 표시를 조절합니다.

      - **결과 확인:** 468개 랜드마크가 메시 형태로 표시됩니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| MP Face Mesh | Confidence | 0.5 | 최소 검출 신뢰도 |
| MP Face Mesh | Draw Contours | true | 메시 윤곽선 표시 |
| MP Face Mesh | Draw Points | false | 개별 랜드마크 점 표시 |


#### 결과 해석

    468개 랜드마크가 얼굴 표면에 메시 형태로 표시됩니다. Landmarks 출력에서 모든 점의 2D 좌표를 얻습니다. Count 출력에서 검출된 얼굴 수를 확인합니다.


    표정 분석 예시: 눈 종횡비(Eye Aspect Ratio)를 계산하면 눈 감김/뜨임을 판별할 수 있고, 입 종횡비로 입 벌림을 감지할 수 있습니다.


    두 단계 파이프라인(얼굴 검출 + 메시 추출)으로 동작하며, face_detection_short_range.onnx와 face_landmark.onnx 모델이 필요합니다.


> **참고:** **팁:** Draw Points=true로 설정하면 468개 점이 모두 표시되어 디버깅에 유용하지만, 시각적으로 복잡해집니다. 분석에 필요한 특정 랜드마크만 인덱스로 추출하여 사용하세요.


    예제 25: 배경 제거 합성


#### 목적

    MediaPipe Selfie Segmentation 모델로 사람과 배경을 분리합니다. 배경을 블러, 제거(투명), 또는 녹색(그린 스크린) 효과로 대체합니다.


    화상 회의 배경, 영상 편집, AR 합성 등에 활용됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 사람 이미지 로드 |
| MP Selfie Segmentation | MediaPipe | 사람/배경 분리 |
| Image Show | Input/Output | 합성 결과 시각화 |


#### 노드 파이프라인


MediaPipe


#### 구축 단계


      - **이미지 소스:** 사람이 포함된 이미지를 로드합니다.

      - **세그먼테이션:** MP Selfie Segmentation 노드를 연결합니다.

      - **배경 모드:** Background Mode를 Blur(배경 블러), Remove(배경 제거), Green(그린 스크린) 중 선택합니다.

      - **임계값:** Threshold=0.5로 설정합니다.

      - **결과 확인:** Result에 합성 결과, Mask에 세그먼테이션 마스크가 출력됩니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| MP Selfie Segmentation | Threshold | 0.5 | 세그먼테이션 임계값 |
| MP Selfie Segmentation | Background Mode | Blur | Blur/Remove/Green 선택 |
| MP Selfie Segmentation | Blur Strength | (기본값) | Blur 모드 시 블러 강도 |


#### 결과 해석

    Blur 모드에서는 사람 영역은 선명하고 배경만 블러 처리됩니다. Remove 모드에서는 배경이 검정으로 제거됩니다. Green 모드에서는 배경이 녹색(크로마키)으로 대체됩니다.


    Mask 출력은 사람 영역이 흰색, 배경이 검정인 바이너리 마스크입니다. 이 마스크를 MaskApply 노드에 활용하면 커스텀 배경 합성이 가능합니다.


    Threshold를 낮추면(0.3) 사람 영역이 넓어져 머리카락 등 경계가 더 포함되고, 높이면(0.7) 확실한 영역만 사람으로 분류됩니다.


> **참고:** **팁:** Mask 출력을 Bitwise AND(MaskApply)로 다른 배경 이미지와 합성하면 가상 배경 기능을 구현할 수 있습니다. 모델 파일: selfie_segmentation.onnx가 필요합니다.


## Chapter 38: YOLO / OCR / 스크립트 예제

  이 장에서는 YOLOv8 객체 검출, PaddleOCR 텍스트 인식, Python 스크립트 연동, 다중 카메라 처리 등 고급 AI 및 확장 기능을 활용한 예제를 다룹니다. 이 노드들은 산업용 비전을 넘어 범용 AI 비전 시스템을 구축할 수 있는 강력한 도구입니다.


    예제 26: YOLO 다중 객체 검출


#### 목적

    YOLOv8 모델을 사용하여 이미지 내의 다양한 객체를 동시에 검출합니다. YOLOv8 Detection 노드는 COCO 80개 클래스 또는 커스텀 학습 모델의 객체를 검출하고, 각 객체의 바운딩 박스, 클래스명, 신뢰도를 출력합니다.


    YOLO는 실시간 객체 검출의 대표적인 모델로, 산업 현장의 제품 분류, 안전 모니터링, 자율 주행 등에 널리 활용됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 검출 대상 이미지 로드 |
| YOLOv8 Detection | YOLO | 다중 객체 검출 |
| Image Show | Input/Output | 검출 결과 시각화 |


#### 노드 파이프라인


YOLO


#### 구축 단계


      - **모델 준비:** yolov8n.onnx (nano) 모델을 Models/YOLO/ 폴더에 배치합니다. PyTorch에서 변환: yolo export model=yolov8n.pt format=onnx.

      - **이미지 소스:** Image Read로 다양한 객체가 포함된 이미지를 로드합니다.

      - **검출 설정:** YOLOv8 Detection 노드를 연결합니다. Confidence=0.25, IoU Threshold=0.45.

      - **모델 파일:** Model File 속성에 "yolov8n.onnx"를 입력합니다.

      - **결과 확인:** Result에 각 객체의 바운딩 박스, 클래스명, 신뢰도가 표시됩니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| YOLOv8 Detection | Confidence | 0.25 | 최소 검출 신뢰도 |
| YOLOv8 Detection | IoU Threshold | 0.45 | NMS IoU 임계값 |
| YOLOv8 Detection | Max Detections | 100 | 최대 검출 수 |
| YOLOv8 Detection | Model File | yolov8n.onnx | ONNX 모델 파일명 |


#### 결과 해석

    각 검출 객체에 클래스 컬러의 바운딩 박스와 "class XX%" 형식의 라벨이 표시됩니다. 상단에 "Objects: N"으로 총 검출 수가 표시됩니다.


    BoundingBoxes(Rect[]), Labels(string[]), Scores(double[]), Count(int) 출력을 통해 후속 처리가 가능합니다. 특정 클래스만 필터링하려면 Labels 배열을 검사합니다.


    모델 크기에 따라 속도/정확도 트레이드오프: nano(n) > small(s) > medium(m) > large(l) > xlarge(x). 모델 메타데이터에서 클래스명을 자동 로드합니다.


> **참고:** **팁:** 커스텀 데이터셋으로 학습한 YOLOv8 모델도 ONNX로 내보내면 동일하게 사용할 수 있습니다. 모델의 클래스명은 ONNX 메타데이터에서 자동으로 읽어옵니다.


    예제 27: OCR 문서 텍스트 추출


#### 목적

    PaddleOCR 노드를 사용하여 이미지에서 텍스트를 검출하고 인식합니다. 다국어(한국어, 영어, 중국어, 일본어 등) 텍스트를 지원하며, 검출된 각 텍스트의 위치, 내용, 신뢰도를 출력합니다.


    문서 디지털화, 라벨 읽기, 일련번호 확인 등 텍스트가 포함된 모든 검사에 활용됩니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 텍스트 이미지 로드 |
| PaddleOCR | OCR | 텍스트 검출 및 인식 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


OCR


#### 구축 단계


      - **모델 준비:** ppocr_det.onnx (검출), ppocr_rec.onnx (인식), ppocr_keys.txt (사전) 파일을 Models/OCR/ 폴더에 배치합니다.

      - **이미지 로드:** 텍스트가 포함된 이미지를 로드합니다.

      - **OCR 설정:** PaddleOCR 노드를 연결합니다. Det Threshold=0.3, Rec Threshold=0.5.

      - **결과 확인:** Result에 텍스트 영역이 바운딩 박스로 표시되고, FullText에서 전체 텍스트를 얻습니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| PaddleOCR | Det Threshold | 0.3 | 텍스트 영역 검출 임계값 |
| PaddleOCR | Rec Threshold | 0.5 | 텍스트 인식 신뢰도 임계값 |
| PaddleOCR | Max Side Length | 960 | 검출용 최대 이미지 크기 |
| PaddleOCR | Det Model | ppocr_det.onnx | 검출 모델 파일 |
| PaddleOCR | Rec Model | ppocr_rec.onnx | 인식 모델 파일 |
| PaddleOCR | Dictionary | ppocr_keys.txt | 문자 사전 파일 |


#### 결과 해석

    검출된 텍스트 영역에 주황색 바운딩 박스가 표시되고 라벨에 인식된 텍스트가 표시됩니다. 상단에 "Texts: N"으로 검출 수가 표시됩니다. OpenCV putText의 ASCII 제한으로 비영어 문자는 '?'로 표시되지만, Texts/FullText 출력에는 원본 유니코드 텍스트가 보존됩니다.


    Texts(string[]), Boxes(Rect[]), Scores(double[]), Count(int), FullText(string) 출력을 제공합니다. FullText는 모든 인식 텍스트를 줄바꿈으로 연결한 문자열입니다.


    한국어 인식을 위해서는 한국어 인식 모델과 사전 파일이 필요합니다. HuggingFace의 paddleocr-onnx 리포지토리에서 다운로드할 수 있습니다.


> **참고:** **팁:** 텍스트 미리보기(Text Preview)에서 유니코드 텍스트를 정확하게 확인할 수 있습니다. Max Side Length를 줄이면 처리 속도가 빨라지지만 작은 텍스트를 놓칠 수 있습니다.


    예제 28: AI 비전 검사 리포트


#### 목적

    YOLOv8 객체 검출과 PaddleOCR 텍스트 인식을 결합하여 제품 이미지에서 구성 요소를 식별하고 라벨을 읽어 종합 검사 리포트를 생성합니다.


    제품 라벨 검증, 부품 목록 확인, 자동 재고 관리 등 복합적인 AI 비전 시나리오를 구현합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 제품 이미지 로드 |
| YOLOv8 Detection | YOLO | 제품 구성 요소 검출 |
| PaddleOCR | OCR | 라벨 텍스트 인식 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


YOLO

        Image Show 1

      &darr; (Image Read 분기)


OCR

        Image Show 2

#### 구축 단계


      - **이미지 로드:** 라벨이 있는 제품 이미지를 로드합니다.

      - **객체 검출:** YOLOv8 Detection으로 제품 내 구성 요소를 검출합니다.

      - **텍스트 인식:** 동일 이미지를 PaddleOCR에도 연결하여 라벨 텍스트를 읽습니다.

      - **결과 통합:** YOLO의 Labels/Count와 OCR의 Texts/FullText를 종합하여 검사 리포트를 구성합니다.

      - **판정:** 검출된 객체 수와 텍스트 내용을 기대값과 비교하여 판정합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| YOLOv8 Detection | Confidence | 0.3 | 객체 검출 신뢰도 |
| PaddleOCR | Det Threshold | 0.3 | 텍스트 검출 임계값 |
| PaddleOCR | Rec Threshold | 0.5 | 텍스트 인식 신뢰도 |


#### 결과 해석

    YOLO Result에서 검출된 구성 요소(예: bottle, cup, laptop 등)의 위치와 클래스를 확인합니다. OCR Result에서 라벨 텍스트(제품명, 일련번호 등)를 확인합니다.


    두 결과를 조합하면 "올바른 제품에 올바른 라벨이 부착되었는가"를 자동으로 검증할 수 있습니다. 예: YOLO로 "bottle" 검출 + OCR로 "제품A" 라벨 확인.


    결과를 Image Write + CsvReader로 저장하면 검사 이력 관리가 가능합니다.


> **참고:** **팁:** YOLO와 OCR을 병렬로 실행하므로 처리 시간은 더 느린 쪽에 의해 결정됩니다. 고해상도 이미지는 Resize 노드로 축소한 후 YOLO에 입력하면 속도가 향상됩니다.


    예제 29: Python 스크립트 연동


#### 목적

    Python Script 노드를 사용하여 MVXTester에서 제공하지 않는 커스텀 이미지 처리를 수행합니다. 시스템 Python을 호출하여 OpenCV, NumPy, scikit-image 등의 Python 라이브러리를 활용합니다.


    특수한 알고리즘, 학술 논문의 구현, 프로토타이핑 등 기존 노드로 해결하기 어려운 처리를 Python으로 확장합니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Image Read | Input/Output | 입력 이미지 로드 |
| Python Script | Script | 커스텀 Python 처리 |
| Image Show | Input/Output | 결과 시각화 |


#### 노드 파이프라인


Script


#### 구축 단계


      - **Python 환경:** Python이 시스템에 설치되어 있고 PATH에 등록되어 있는지 확인합니다.

      - **이미지 입력:** Image Read 출력을 Python Script의 Image 입력에 연결합니다.

      - **스크립트 작성:** Python Script 속성에 코드를 입력합니다. sys.argv[1]로 입력 이미지 경로, sys.argv[2]로 출력 이미지 경로를 받습니다.

      - **Python 경로:** Python Path 속성에 python 실행 경로를 설정합니다 (기본: "python").

      - **타임아웃:** Timeout을 30000ms (30초)로 설정합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Python Script | Python Script | (코드) | 실행할 Python 코드 |
| Python Script | Python Path | python | Python 실행 파일 경로 |
| Python Script | Timeout (ms) | 30000 | 스크립트 실행 타임아웃 |


#### 결과 해석

    Python 스크립트의 실행 결과 이미지(sys.argv[2]에 저장)가 Result 출력으로 전달됩니다. 스크립트 내에서 cv2.imread(sys.argv[1])로 입력을 읽고, 처리 후 cv2.imwrite(sys.argv[2], result)로 결과를 저장합니다.


    Python 스크립트에서 에러가 발생하면 노드의 Error에 stderr 내용이 표시됩니다. 타임아웃 초과 시에도 에러가 발생합니다.


    임시 파일을 통해 이미지를 교환하므로 대용량 이미지의 경우 디스크 I/O 오버헤드가 있습니다.


> **참고:** **팁:** Python 스크립트 내에서 pip install로 필요한 패키지를 미리 설치해두세요. 복잡한 처리는 별도의 .py 파일로 작성하고 Python Script 노드에서 호출하는 것이 유지보수에 유리합니다.


    예제 30: 다중 카메라 동시 처리


#### 목적

    여러 대의 카메라를 동시에 운용하여 다각도 또는 다위치 검사를 수행하는 시스템을 구축합니다. 각 카메라에 독립적인 검사 파이프라인을 연결하고, 모든 검사 결과를 종합하여 최종 판정합니다.


    다면 검사, 전후면 동시 검사, 멀티스테이션 검사 등 실제 생산 라인에서 필수적인 구성입니다.


#### 사용 노드


| 노드 | 카테고리 | 역할 |
| --- | --- | --- |
| Camera (x2) | Input/Output | 다중 카메라 영상 취득 |
| Object Measure | Measurement | 카메라 1: 치수 측정 |
| Defect Detector | Inspection | 카메라 2: 결함 검사 |
| Compare (x2) | Control | 각 검사 판정 |
| Boolean Logic | Control | 종합 판정 |
| Image Show (x2) | Input/Output | 각 카메라 결과 시각화 |


#### 노드 파이프라인


        Camera 1

Measurement

Control


        &searr;


        Boolean Logic (AND)
Control


        Camera 2

Inspection

Control


        &nearr;


#### 구축 단계


      - **카메라 연결:** 두 Camera 노드를 배치하고 각각 다른 카메라 장치를 선택합니다.

      - **검사 파이프라인 1:** Camera 1에 Object Measure를 연결하여 치수를 측정합니다.

      - **검사 파이프라인 2:** Camera 2에 Defect Detector를 연결하여 결함을 검출합니다.

      - **개별 판정:** 각 검사 결과를 Compare로 판정합니다 (치수 공차 내, 결함 수 0).

      - **종합 판정:** Boolean Logic (AND)로 두 판정을 결합합니다.

      - **실행:** Stream 모드(F6)로 실행하면 두 카메라가 동시에 영상을 취득하고 병렬 검사합니다.


#### 핵심 파라미터 설정


| 노드 | 속성 | 권장값 | 설명 |
| --- | --- | --- | --- |
| Camera 1, 2 | Device | 각각 다른 장치 | 카메라 장치 선택 |
| Camera 1, 2 | Width/Height | 640/480 | 캡처 해상도 |
| Object Measure | Pixels Per Unit | 캘리브레이션 값 | 카메라 1 캘리브레이션 |
| Defect Detector | Threshold | 30 | 카메라 2 결함 감도 |
| Boolean Logic | Operation | AND | 종합 판정 |


#### 결과 해석

    각 Camera의 Image Show에서 개별 카메라 영상과 검사 결과를 확인합니다. Boolean Logic 출력이 true이면 두 카메라 검사 모두 통과한 양품입니다.


    MVXTester의 실행 엔진은 독립적인 브랜치를 병렬로 처리하므로, 두 카메라 파이프라인은 자동으로 병렬 실행됩니다. 전체 사이클 타임은 더 느린 쪽에 의해 결정됩니다.


    카메라 수를 확장하려면 Camera 노드와 검사 파이프라인을 추가하고 Boolean Logic 체인을 확장합니다.


> **참고:** **팁:** 각 카메라의 해상도와 FPS를 실제 검사 요구사항에 맞게 설정하세요. 고해상도는 정밀도를 높이지만 처리 시간이 증가합니다. Camera 노드의 IStreamingSource 인터페이스에 의해 Stream 모드에서 연속 촬영이 가능합니다.