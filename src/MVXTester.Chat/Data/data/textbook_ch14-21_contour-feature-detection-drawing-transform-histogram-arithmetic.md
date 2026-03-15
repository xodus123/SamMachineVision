## Chapter 14. Contour — 윤곽선 분석


Contour 카테고리는 이진 이미지에서 윤곽선을 검출하고, 면적, 둘레, 중심점, 외접 도형 등 다양한 형상 정보를 계산하는 13개 노드를 제공합니다.


#### FindContours

이진 이미지에서 윤곽선(컨투어)을 검출하는 기본 노드입니다. OpenCV의 FindContours 알고리즘으로 모든 객체의 경계를 추출합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Contours | Point[][] | 출력 Contours |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Mode | RetrievalModes | List | 윤곽선 검색 모드 (List, External, Tree 등) |
| Method | ContourApproxModes | ApproxSimple | 윤곽선 근사 방법 |


**기능 설명**

FindContours 노드는 이진 이미지에서 연결 영역의 경계를 Point[][] 형태로 추출합니다. 다채널 입력은 자동으로 그레이스케일 변환됩니다.


Mode 속성으로 검색 구조를 설정합니다. List는 모든 윤곽선을 평면 리스트로, External은 최외곽만, Tree는 계층 구조를 반환합니다. Method는 점 압축 방식을 제어합니다.


후속 Contour 분석 노드(ContourArea, BoundingRect, ApproxPoly 등)의 입력으로 직접 연결됩니다.


**응용 분야**


  - 제조 라인 부품 외형 검사

  - 이진화 후 객체 개수 파악

  - 형상 기반 분류 전처리

  - 결함 영역 경계 추출


#### DrawContours

검출된 윤곽선을 이미지 위에 시각적으로 그려주는 노드입니다. 색상, 두께, 인덱스를 지정할 수 있습니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| ColorR | int | 0 | 빨간색 성분 (0~255) |
| ColorG | int | 255 | 초록색 성분 (0~255) |
| ColorB | int | 0 | 파란색 성분 (0~255) |
| Thickness | int | 2 | 선 두께 (1~50) |
| Index | int | -1 | 윤곽선 인덱스 (-1=전체, -1~10000) |


**기능 설명**

DrawContours 노드는 FindContours 결과를 원본 이미지 위에 지정 색상/두께로 렌더링합니다. Index -1이면 전체, 특정 숫자면 해당 인덱스만 그립니다.


BGR 색상 체계 사용. 디버깅 및 결과 시각화에 필수적인 노드입니다.


**응용 분야**


  - 검출 결과 시각적 확인 및 디버깅

  - 검사 결과 리포트 이미지 생성

  - 특정 윤곽선만 강조 표시


#### ContourArea

각 윤곽선의 면적을 계산하여 배열로 출력하는 노드입니다.


    Areas double[]


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Areas | double[] | 출력 Areas |


**기능 설명**

ContourArea 노드는 모든 윤곽선에 대해 Cv2.ContourArea()를 호출하여 면적을 double 배열로 반환합니다.


ContourFilter와 연계하여 특정 크기 범위의 객체만 선별하거나, 면적 기반 품질 판정에 활용됩니다.


**응용 분야**


  - 객체 크기 측정 및 분류

  - 노이즈 제거 (작은 면적 필터링)

  - 면적 기반 양/불량 판정


#### BoundingRect

각 윤곽선의 축 정렬 외접 사각형(Bounding Rectangle)을 계산하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Rects | Rect[] | 출력 Rects |


**기능 설명**

BoundingRect 노드는 각 윤곽선을 완전히 포함하는 축 정렬(axis-aligned) 최소 사각형을 계산합니다. Rect에는 X, Y, Width, Height 정보가 포함됩니다.


DrawBoundingBoxes 노드와 연결하여 시각화하거나, ROI 크롭이나 객체 위치 추적에 활용됩니다.


**응용 분야**


  - 객체 위치 및 크기 추정

  - ROI 자동 설정

  - 바운딩 박스 기반 추적


#### ApproxPoly

윤곽선을 다각형으로 근사하는 노드입니다. Douglas-Peucker 알고리즘을 사용합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Approx | Point[][] | 출력 Approx |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Epsilon | double | 2.0 | 호 길이 대비 근사 정확도 % (0.01~50.0) |


**기능 설명**

ApproxPoly 노드는 각 윤곽선의 호 길이에 Epsilon 비율을 곱한 값을 허용 오차로 사용하여 다각형 근사를 수행합니다.


근사 결과의 꼭짓점 수로 도형을 분류할 수 있습니다 (3개=삼각형, 4개=사각형 등).


**응용 분야**


  - 도형 분류 (삼각형, 사각형 판별)

  - 윤곽선 단순화

  - 형상 특징 추출


#### ConvexHull

각 윤곽선의 볼록 껍질(Convex Hull)을 계산하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Hulls | Point[][] | 출력 Hulls |


**기능 설명**

ConvexHull 노드는 각 윤곽선을 감싸는 최소 볼록 다각형을 계산합니다.


원본 윤곽선과 볼록 껍질의 면적 비율(Solidity)로 객체의 볼록 정도를 측정하거나, 오목 결함 분석에 활용됩니다.


**응용 분야**


  - Solidity 계산을 통한 형상 분석

  - 제스처 인식 (볼록 결함 분석)

  - 객체 외형 단순화


#### MinEnclosingCircle

각 윤곽선을 포함하는 최소 외접원을 계산하는 노드입니다.


    Radii float[]


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Centers | Point2f[] | 출력 Centers |
| 출력 | Radii | float[] | 출력 Radii |


**기능 설명**

MinEnclosingCircle 노드는 각 윤곽선을 완전히 포함하는 최소 크기의 원을 계산합니다. 중심 좌표 배열과 반지름 배열을 출력합니다.


원형 객체의 크기 측정이나 원형도 분석에 유용합니다.


**응용 분야**


  - 원형 부품 크기 측정

  - 원형도(circularity) 분석

  - 볼, 링 등 원형 객체 검사


#### ContourCenters

모멘트를 이용하여 각 윤곽선의 무게 중심(centroid)을 계산하고 이미지에 표시하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Centers | Point[] | 출력 Centers |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MinArea | double | 0 | 포함할 최소 윤곽선 면적 (0~1000000) |
| DrawRadius | int | 5 | 중심점 표시 반지름 (1~50) |


**기능 설명**

ContourCenters 노드는 이미지 모멘트(M10/M00, M01/M00)를 사용하여 각 윤곽선의 질량 중심을 계산합니다. MinArea로 노이즈를 필터링합니다.


Image 입력이 연결되면 결과 이미지에 빨간색 원으로 중심점을 표시합니다.


**응용 분야**


  - 객체 위치 추적

  - 정렬 검사 (중심점 편차 측정)

  - 로봇 비전 좌표 산출


#### ContourFilter

면적 및 둘레 범위 조건으로 윤곽선을 필터링하는 노드입니다.


    Count int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Filtered | Point[][] | 출력 Filtered |
| 출력 | Count | int | 출력 Count |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MinArea | double | 100 | 최소 면적 (0~1000000) |
| MaxArea | double | 10000000 | 최대 면적 (0~10000000) |
| MinPerimeter | double | 0 | 최소 둘레 (0~100000) |
| MaxPerimeter | double | 1000000 | 최대 둘레 (0~1000000) |


**기능 설명**

ContourFilter 노드는 면적과 둘레의 최소/최대 범위를 기준으로 윤곽선을 선별합니다. 두 조건을 동시에 만족하는 윤곽선만 출력됩니다.


노이즈 윤곽선이나 이미지 경계의 과대 윤곽선을 제거하여 관심 객체만 추출합니다. Count 출력으로 객체 수를 확인합니다.


**응용 분야**


  - 노이즈 윤곽선 제거

  - 특정 크기 범위의 객체만 선택

  - 객체 수량 카운팅


#### Moments

각 윤곽선의 이미지 모멘트를 계산하여 면적과 중심 좌표를 출력하는 노드입니다.


    Areas double[]

    CenterX double[]

    CenterY double[]


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Areas | double[] | 출력 Areas |
| 출력 | CenterX | double[] | 출력 CenterX |
| 출력 | CenterY | double[] | 출력 CenterY |


**기능 설명**

Moments 노드는 Cv2.Moments()를 사용하여 각 윤곽선의 공간 모멘트를 계산합니다. M00은 면적, M10/M00과 M01/M00은 X, Y 중심입니다.


면적과 중심을 동시에 얻을 수 있어 효율적이며 노이즈에 강건합니다.


**응용 분야**


  - 객체 면적 및 위치 동시 측정

  - 모멘트 기반 형상 분석

  - 객체 방향 추정


#### FitEllipse

윤곽선에 최적 타원을 피팅하여 이미지에 그려주는 노드입니다. 최소 5개 점이 필요합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MinPoints | int | 5 | 타원 피팅 최소 점 수 (5~100) |
| DrawThickness | int | 2 | 타원 선 두께 (1~10) |


**기능 설명**

FitEllipse 노드는 최소 5개 이상의 점을 가진 윤곽선에 대해 최적 타원을 피팅하고 초록색으로 그립니다.


타원의 장축/단축 비율로 편심도를, 회전 각도로 방향을 측정할 수 있습니다.


**응용 분야**


  - 타원형 부품 검사

  - 객체 방향(회전각) 측정

  - 편심도 분석


#### MinAreaRect

각 윤곽선의 최소 면적 회전 사각형을 계산하여 이미지에 그려주는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours | Point[][] | 입력 Contours |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| DrawThickness | int | 2 | 사각형 선 두께 (1~10) |


**기능 설명**

MinAreaRect 노드는 각 윤곽선을 감싸는 최소 면적의 회전 사각형(RotatedRect)을 계산합니다. BoundingRect와 달리 최적 회전 각도를 가집니다.


회전된 객체의 실제 폭/높이를 정확하게 측정할 수 있어 경사진 부품의 치수 검사에 적합합니다.


**응용 분야**


  - 회전된 부품의 정밀 치수 측정

  - 객체 회전 각도 추정

  - 직사각형 객체 정합


#### MatchShapes

두 윤곽선 집합 간의 형상 유사도를 Hu 모멘트 기반으로 비교하는 노드입니다.


    Similarities double[]


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Contours1 | Point[][] | 입력 Contours1 |
| 입력 | Contours2 | Point[][] | 입력 Contours2 |
| 출력 | Similarities | double[] | 출력 Similarities |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Method | ShapeMatchModes | I1 | 형상 매칭 방법 |


**기능 설명**

MatchShapes 노드는 Contours2의 첫 번째 윤곽선을 참조로 사용하여 Contours1의 각 윤곽선과의 유사도를 계산합니다. 0에 가까울수록 유사합니다.


Hu 모멘트 기반이므로 크기, 위치, 회전에 불변하는 안정적 형상 비교가 가능합니다.


**응용 분야**


  - 형상 기반 부품 분류

  - 결함 유형 매칭

  - 참조 형상 대비 유사도 판정


## Chapter 15. Feature Detection — 특징점 검출


Feature 카테고리는 이미지에서 코너, 블롭 등의 특징점을 검출하고, 특징 기술자를 생성하여 매칭하는 8개 노드를 제공합니다.


#### ORB Features

ORB(Oriented FAST and Rotated BRIEF) 특징점 검출 및 기술자 추출 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Descriptors | Mat | 출력 Descriptors |
| 출력 | Keypoints Image | Mat | 출력 Keypoints Image |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| NFeatures | int | 500 | 최대 특징점 수 (1~10000) |
| ScaleFactor | float | 1.2 | 피라미드 축소 비율 (1.01~2.0) |
| NLevels | int | 8 | 피라미드 레벨 수 (1~20) |


**기능 설명**

ORB Features 노드는 FAST 기반 키포인트 검출과 BRIEF 기반 기술자 생성을 결합한 고속 특징 추출을 수행합니다.


Descriptors 출력은 MatchFeatures 노드에 연결하여 이미지 간 매칭에 사용합니다. Keypoints Image는 키포인트 시각화 결과입니다.


**응용 분야**


  - 실시간 특징점 매칭

  - 객체 인식 및 추적

  - 이미지 정합(Registration)


#### SIFT Features

SIFT(Scale-Invariant Feature Transform) 특징점 검출 및 기술자 추출 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Descriptors | Mat | 출력 Descriptors |
| 출력 | Keypoints Image | Mat | 출력 Keypoints Image |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| NFeatures | int | 0 | 최대 특징점 수 (0=무제한, 0~10000) |
| NOctaveLayers | int | 3 | 옥타브당 레이어 수 (1~10) |
| ContrastThreshold | double | 0.04 | 대비 임계값 (0.0~1.0) |


**기능 설명**

SIFT Features 노드는 스케일 불변 특징 변환을 수행합니다. 크기와 회전에 불변하는 128차원 기술자를 생성합니다.


ORB보다 정확하지만 속도가 느립니다. 정밀한 매칭이 필요한 경우에 적합합니다.


**응용 분야**


  - 정밀 이미지 매칭

  - 파노라마 스티칭

  - 3D 재구성


#### FAST Features

FAST(Features from Accelerated Segment Test) 코너 검출 노드입니다. 고속 코너 검출에 특화되어 있습니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Threshold | int | 10 | 검출 임계값 (0~255) |
| NonmaxSuppression | bool | true | 비최대 억제 적용 여부 |


**기능 설명**

FAST Features 노드는 원형 경계 픽셀을 비교하여 코너를 고속으로 검출합니다. NonmaxSuppression을 활성화하면 인접 중복 특징점을 억제합니다.


실시간 처리에 적합한 가장 빠른 코너 검출기 중 하나입니다. Result 출력에 키포인트가 시각화됩니다.


**응용 분야**


  - 실시간 비전 시스템

  - 모바일/임베디드 특징점 검출

  - 고속 추적 전처리


#### Harris Corner

Harris 코너 검출 노드입니다. 클래식한 코너 검출 알고리즘을 구현합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| BlockSize | int | 2 | 이웃 크기 (1~31) |
| KSize | int | 3 | Sobel 커널 크기 (1~31) |
| K | double | 0.04 | Harris 자유 매개변수 (0.0~1.0) |


**기능 설명**

Harris Corner 노드는 이미지의 그래디언트 공분산 행렬을 분석하여 코너를 검출합니다. 결과는 정규화된 코너 응답 맵(0-255)으로 출력됩니다.


K 값이 작을수록 더 많은 코너가 검출됩니다. BlockSize로 이웃 크기를 제어합니다.


**응용 분야**


  - 정밀 코너 검출

  - 캘리브레이션 패턴 검출

  - 구조적 특징 분석


#### Shi-Tomasi Corners

Shi-Tomasi 코너 검출 노드입니다. Harris의 개선 버전으로 더 안정적인 코너를 검출합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MaxCorners | int | 100 | 최대 코너 수 (1~10000) |
| QualityLevel | double | 0.01 | 최소 품질 수준 (0.0~1.0) |
| MinDistance | double | 10.0 | 코너 간 최소 거리 (0.0~1000.0) |


**기능 설명**

Shi-Tomasi Corners 노드는 GoodFeaturesToTrack 알고리즘을 사용합니다. 고유값 중 작은 값을 기준으로 코너 품질을 평가합니다.


빨간색 원으로 검출된 코너를 시각화합니다. MinDistance로 코너 간 최소 간격을 보장합니다.


**응용 분야**


  - 추적을 위한 특징점 선별

  - 안정적 코너 검출

  - 광학 흐름 전처리


#### Good Features To Track

Shi-Tomasi/Harris 기반 코너 검출 노드입니다. 코너 좌표를 Point[] 배열로 직접 출력합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |
| 출력 | Corners | Point[] | 출력 Corners |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MaxCorners | int | 100 | 최대 코너 수 (1~10000) |
| QualityLevel | double | 0.01 | 최소 품질 (0.001~1.0) |
| MinDistance | double | 10.0 | 최소 거리 (1.0~1000.0) |
| BlockSize | int | 3 | 블록 크기 (3~31) |
| UseHarris | bool | false | Harris 검출기 사용 여부 |


**기능 설명**

Good Features To Track 노드는 Corners 출력 포트로 코너 좌표를 Point 배열로 직접 제공합니다. UseHarris 속성으로 Shi-Tomasi와 Harris를 선택합니다.


검출된 코너는 초록색 원으로 시각화됩니다. BlockSize로 미분 공분산 행렬의 평균화 블록을 제어합니다.


**응용 분야**


  - 정밀 코너 좌표 추출

  - 이미지 정합 제어점

  - 객체 추적 초기화


#### Simple Blob Detector

SimpleBlobDetector를 사용하여 블롭(덩어리)을 검출하는 노드입니다. 면적, 원형도, 볼록도, 관성비 필터를 지원합니다.


    Sizes double[]


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |
| 출력 | Centers | Point[] | 출력 Centers |
| 출력 | Sizes | double[] | 출력 Sizes |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MinThreshold | int | 50 | 최소 이진화 임계값 (0~255) |
| MaxThreshold | int | 220 | 최대 이진화 임계값 (0~255) |
| FilterByArea | bool | true | 면적 필터 사용 |
| MinArea | double | 100.0 | 최소 블롭 면적 (0~100000) |
| MaxArea | double | 50000.0 | 최대 블롭 면적 (0~1000000) |
| FilterByCircularity | bool | false | 원형도 필터 사용 |
| MinCircularity | double | 0.1 | 최소 원형도 (0~1) |
| FilterByConvexity | bool | false | 볼록도 필터 사용 |
| MinConvexity | double | 0.5 | 최소 볼록도 (0~1) |
| FilterByInertia | bool | false | 관성비 필터 사용 |
| MinInertiaRatio | double | 0.1 | 최소 관성비 (0~1) |


**기능 설명**

Simple Blob Detector 노드는 여러 임계값으로 이진화한 후 연결 영역을 분석하여 블롭을 검출합니다. Centers와 Sizes 출력으로 각 블롭의 중심과 크기를 제공합니다.


4가지 필터(면적, 원형도, 볼록도, 관성비)를 조합하여 원하는 형태의 블롭만 선별할 수 있습니다.


**응용 분야**


  - 원형 객체 검출

  - 점/홀 검출

  - LED/마커 위치 추출

  - 결함 블롭 검출


#### Match Features

두 이미지의 특징 기술자를 매칭하는 노드입니다. BruteForce, BruteForceHamming, FlannBased 매처를 지원합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Desc1 | Mat | 입력 Desc1 |
| 입력 | Desc2 | Mat | 입력 Desc2 |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Matches Image | Mat | 출력 Matches Image |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| MatcherType | MatcherType | BruteForce | 매칭 알고리즘 (BruteForce/BruteForceHamming/FlannBased) |
| MaxMatches | int | 50 | 표시할 최대 매칭 수 (1~1000) |


**기능 설명**

Match Features 노드는 두 기술자 세트 간의 거리를 계산하여 유사한 특징점 쌍을 찾습니다. 결과를 거리순으로 정렬하여 상위 N개를 시각화합니다.


ORB 기술자에는 BruteForceHamming, SIFT 기술자에는 BruteForce 또는 FlannBased가 적합합니다.


**응용 분야**


  - 이미지 간 대응점 매칭

  - 파노라마 합성

  - 객체 인식 확인

  - 호모그래피 추정 전처리


## Chapter 16. Detection — 객체 탐지


Detection 카테고리는 허프 변환, 템플릿 매칭, Haar 캐스케이드 등 다양한 객체 탐지 알고리즘을 제공하는 9개 노드로 구성됩니다.


#### Hough Lines

허프 변환을 사용하여 이미지에서 직선을 검출하는 노드입니다. HoughLinesP(확률적 허프) 알고리즘을 사용합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Rho | double | 1.0 | 거리 해상도 (0.1~10.0 픽셀) |
| Theta | double | 1.0 | 각도 해상도 (0.1~90.0 도) |
| Threshold | int | 100 | 누산기 임계값 (1~1000) |
| MinLineLength | double | 50.0 | 최소 직선 길이 (0~10000) |
| MaxLineGap | double | 10.0 | 최대 직선 간격 (0~1000) |


**기능 설명**

Hough Lines 노드는 확률적 허프 변환(HoughLinesP)으로 선분의 시작/끝 좌표를 검출합니다. 다채널 입력은 자동 그레이스케일 변환됩니다.


검출된 선분은 빨간색으로 결과 이미지에 표시됩니다. Threshold를 높이면 더 확실한 직선만, MinLineLength를 높이면 긴 직선만 검출됩니다.


**응용 분야**


  - 직선 에지 검출

  - 도로 차선 인식

  - 기하학적 검사

  - PCB 패턴 검사


#### Hough Circles

허프 변환을 사용하여 이미지에서 원을 검출하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Dp | double | 1.0 | 해상도 역비율 (0.1~10.0) |
| MinDist | double | 50.0 | 원 중심 간 최소 거리 (1~10000) |
| Param1 | double | 200.0 | Canny 상위 임계값 (1~1000) |
| Param2 | double | 100.0 | 누산기 임계값 (1~1000) |
| MinRadius | int | 0 | 최소 반지름 (0~5000) |
| MaxRadius | int | 0 | 최대 반지름 (0=무제한, 0~5000) |


**기능 설명**

Hough Circles 노드는 허프 그래디언트 방법으로 원을 검출합니다. 초록색 원과 빨간색 중심점으로 결과를 시각화합니다.


Param2를 낮추면 더 많은 원이 검출되지만 오탐이 증가합니다. MinDist로 겹치는 원을 방지합니다.


**응용 분야**


  - 원형 부품 검출

  - 홀/핀 검사

  - 동전/원형 마커 검출

  - 눈동자/동공 검출


#### Template Match

템플릿 매칭으로 이미지에서 최적 일치 위치를 찾는 노드입니다. 단일 최고 매칭을 반환합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Template | Mat | 입력 Template |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Method | TemplateMatchModes | CCoeffNormed | 매칭 방법 |


**기능 설명**

Template Match 노드는 템플릿 이미지를 원본에서 슬라이딩하며 유사도를 계산합니다. 최고 매칭 위치에 초록색 사각형과 점수를 표시합니다.


CCoeffNormed 방법이 가장 안정적이며, 1.0에 가까울수록 완벽한 매칭입니다.


**응용 분야**


  - 패턴 위치 검출

  - 정밀 정렬 확인

  - 마크/로고 검색


#### Template Match Multi

NMS(비최대 억제)를 사용하여 이미지에서 여러 템플릿 매칭을 찾는 노드입니다.


    Count int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Template | Mat | 입력 Template |
| 출력 | Result | Mat | 출력 Result |
| 출력 | Matches | Point[] | 출력 Matches |
| 출력 | Count | int | 출력 Count |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Method | TemplateMatchModes | CCoeffNormed | 매칭 방법 |
| MatchThreshold | double | 0.8 | 최소 매칭 점수 (0.0~1.0) |
| MaxMatches | int | 100 | 최대 매칭 수 (1~1000) |


**기능 설명**

Template Match Multi 노드는 임계값 이상의 모든 매칭 위치를 NMS로 찾습니다. 영역 억제 방식으로 중복 검출을 방지합니다.


Matches 출력으로 모든 매칭 좌표를, Count로 총 매칭 수를 제공합니다. 반복 패턴 검출에 최적화되어 있습니다.


**응용 분야**


  - 반복 패턴 위치 검출

  - 다중 객체 검색

  - PCB 부품 위치 확인

  - 결함 패턴 탐지


#### Haar Cascade

Haar 캐스케이드 분류기를 사용한 객체 탐지 노드입니다. XML 분류기 파일이 필요합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| FilePath | string |  | Haar 캐스케이드 XML 파일 경로 |
| ScaleFactor | double | 1.1 | 이미지 스케일 팩터 (1.01~2.0) |
| MinNeighbors | int | 3 | 최소 이웃 수 (0~50) |
| MinSizeW | int | 30 | 최소 탐지 너비 (0~1000) |
| MinSizeH | int | 30 | 최소 탐지 높이 (0~1000) |


**기능 설명**

Haar Cascade 노드는 사전 학습된 XML 분류기를 로드하여 슬라이딩 윈도우 방식의 객체 탐지를 수행합니다. 분류기가 캐싱되어 반복 실행 시 효율적입니다.


검출 결과는 초록색 사각형으로 표시됩니다. ScaleFactor를 높이면 속도가 빨라지지만 정확도가 감소합니다. MinNeighbors를 높이면 오탐이 줄어듭니다.


**응용 분야**


  - 얼굴 탐지

  - 눈/코/입 검출

  - 사람/차량 검출

  - 사전 학습 모델 활용


#### Line Profile

지정된 직선을 따라 픽셀 밝기 프로파일을 측정하는 노드입니다.


    Profile double[]


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Profile | double[] | 출력 Profile |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| X1 | int | 0 | 시작 X 좌표 (0~10000) |
| Y1 | int | 0 | 시작 Y 좌표 (0~10000) |
| X2 | int | 100 | 끝 X 좌표 (0~10000) |
| Y2 | int | 100 | 끝 Y 좌표 (0~10000) |


**기능 설명**

Line Profile 노드는 두 점 사이의 직선을 따라 픽셀 밝기를 샘플링하여 1D 프로파일을 생성합니다. 결과 이미지에 측정 선과 하단 그래프를 표시합니다.


시작점은 빨간색, 끝점은 파란색으로 표시되며 프로파일 곡선이 하단에 오버레이됩니다. 에지 위치 측정에 유용합니다.


**응용 분야**


  - 에지 위치 정밀 측정

  - 표면 밝기 분포 분석

  - 간격/폭 측정

  - 라인 스캔 프로파일


#### Pixel Count

이미지에서 비제로(또는 임계값 이상) 픽셀 수와 비율을 계산하는 노드입니다.


    Count int

    Ratio double


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Count | int | 출력 Count |
| 출력 | Ratio | double | 출력 Ratio |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| UseThreshold | bool | false | 임계값 적용 여부 |
| ThresholdValue | int | 128 | 임계값 (0~255) |


**기능 설명**

Pixel Count 노드는 이미지의 비제로 픽셀을 카운트합니다. UseThreshold 활성화 시 먼저 이진화한 후 카운팅합니다.


Ratio 출력은 비제로 픽셀 비율(0.0~1.0)로, 영역 점유율이나 커버리지 판정에 활용됩니다.


**응용 분야**


  - 영역 점유율 측정

  - 이진 마스크 면적 계산

  - 충전율(fill ratio) 검사


#### Min Max Loc

이미지에서 최소/최대 밝기 값과 그 위치를 찾는 노드입니다.


    MinVal double

    MaxVal double


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | MinVal | double | 출력 MinVal |
| 출력 | MaxVal | double | 출력 MaxVal |
| 출력 | MinLoc | Point | 출력 MinLoc |
| 출력 | MaxLoc | Point | 출력 MaxLoc |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Min Max Loc 노드는 Cv2.MinMaxLoc()을 사용하여 이미지의 최소/최대 픽셀 값과 위치를 찾습니다. 다채널 이미지는 자동 그레이스케일 변환됩니다.


결과 이미지에 최소 위치는 파란색, 최대 위치는 빨간색 마커와 값으로 표시됩니다. 밝점/암점 탐지에 필수적입니다.


**응용 분야**


  - 밝점/암점 탐지

  - 핫스팟 검출

  - 최대 밝기 위치 추적

  - 품질 검사 기준점 설정


#### Connected Components

연결 요소 라벨링을 수행하여 각 영역에 고유 라벨을 부여하는 노드입니다.


    Count int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Labels | Mat | 출력 Labels |
| 출력 | Result | Mat | 출력 Result |
| 출력 | Count | int | 출력 Count |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Connectivity | ConnectivityType | Eight | 픽셀 연결 방식 (Four/Eight) |


**기능 설명**

Connected Components 노드는 이진 이미지에서 연결된 영역을 식별하고 각각에 고유한 정수 라벨을 부여합니다. 배경(라벨 0)을 제외한 영역 수를 Count로 출력합니다.


Result 출력은 각 영역을 고유 색상으로 칠한 시각화 이미지입니다. 4-연결과 8-연결 방식을 선택할 수 있습니다.


**응용 분야**


  - 개별 객체 식별 및 카운팅

  - 영역별 통계 분석

  - 라벨 기반 객체 분류

  - 입자 분석


## Chapter 17. Segmentation — 영역 분할


Segmentation 카테고리는 FloodFill, GrabCut, Watershed 등 고급 영역 분할 알고리즘을 제공하는 3개 노드로 구성됩니다.


#### Flood Fill

시드 포인트에서 시작하여 유사한 픽셀 영역을 채우는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| SeedX | int | 0 | 시드 X 좌표 (0~10000) |
| SeedY | int | 0 | 시드 Y 좌표 (0~10000) |
| NewValR | int | 255 | 새 색상 R (0~255) |
| NewValG | int | 0 | 새 색상 G (0~255) |
| NewValB | int | 0 | 새 색상 B (0~255) |
| LoDiff | int | 20 | 하한 밝기 차이 (0~255) |
| UpDiff | int | 20 | 상한 밝기 차이 (0~255) |


**기능 설명**

Flood Fill 노드는 시드 포인트의 픽셀 값을 기준으로 LoDiff~UpDiff 범위 내의 인접 픽셀을 새 색상으로 채웁니다.


영역 선택, 배경 제거, 마스크 생성에 활용됩니다. 시드 포인트가 이미지 범위를 벗어나면 오류를 반환합니다.


**응용 분야**


  - 영역 선택 및 색상 치환

  - 배경 제거 전처리

  - 마스크 자동 생성

  - 영역 기반 세그멘테이션


#### GrabCut

GrabCut 알고리즘으로 전경/배경을 분리하는 노드입니다. ROI 사각형으로 초기 영역을 지정합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |
| 출력 | Mask | Mat | 출력 Mask |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| RectX | int | 10 | ROI X 위치 (0~10000) |
| RectY | int | 10 | ROI Y 위치 (0~10000) |
| RectW | int | 200 | ROI 너비 (1~10000) |
| RectH | int | 200 | ROI 높이 (1~10000) |
| Iterations | int | 5 | GrabCut 반복 횟수 (1~50) |


**기능 설명**

GrabCut 노드는 3채널 BGR 이미지에서 지정된 ROI를 기반으로 전경/배경 분리를 수행합니다. GMM(가우시안 혼합 모델) 기반 반복 최적화를 통해 정밀한 분할을 달성합니다.


Result는 전경만 추출된 이미지, Mask는 전경 마스크(255)입니다. Iterations를 높이면 정확도가 향상되지만 처리 시간이 증가합니다.


**응용 분야**


  - 전경/배경 분리

  - 객체 추출

  - 배경 제거

  - 인터랙티브 세그멘테이션


#### Watershed

Watershed(분수령) 알고리즘으로 이미지를 영역 분할하는 노드입니다. 마커 입력 또는 자동 생성을 지원합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Markers | Mat | 입력 Markers |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| DistThreshold | double | 0.5 | 거리 변환 임계값 (0.0~1.0, 자동 마커 생성 시 사용) |


**기능 설명**

Watershed 노드는 마커 기반 분수령 알고리즘으로 이미지를 여러 영역으로 분할합니다. Markers 입력이 없으면 거리 변환과 연결 요소 분석으로 자동 마커를 생성합니다.


결과는 각 영역을 고유 색상으로, 경계를 흰색으로 표시한 이미지입니다. DistThreshold는 자동 마커 생성 시 전경 영역의 민감도를 조절합니다.


**응용 분야**


  - 겹친 객체 분리

  - 셀/입자 분할

  - 영역 기반 분석

  - 인스턴스 세그멘테이션


## Chapter 18. Drawing — 도형 그리기


Drawing 카테고리는 이미지 위에 선, 사각형, 원, 타원, 텍스트 등 다양한 시각 요소를 그리는 10개 노드를 제공합니다. 대부분 GetPortOrProperty 패턴을 사용하여 포트 입력과 속성 폴백을 지원합니다.


#### Draw Line

이미지 위에 직선을 그리는 노드입니다. 시작/끝 좌표를 포트 또는 속성으로 지정합니다.


    Pt1X int

    Pt1Y int

    Pt2X int

    Pt2Y int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Pt1X | int | 입력 Pt1X |
| 입력 | Pt1Y | int | 입력 Pt1Y |
| 입력 | Pt2X | int | 입력 Pt2X |
| 입력 | Pt2Y | int | 입력 Pt2Y |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Pt1X | int | 0 | 시작 X (0~10000) |
| Pt1Y | int | 0 | 시작 Y (0~10000) |
| Pt2X | int | 100 | 끝 X (0~10000) |
| Pt2Y | int | 100 | 끝 Y (0~10000) |
| ColorR | int | 255 | 빨강 (0~255) |
| ColorG | int | 0 | 초록 (0~255) |
| ColorB | int | 0 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (1~50) |


**기능 설명**

Draw Line 노드는 두 점 사이에 직선을 그립니다. 포트 입력이 연결되면 포트 값을, 없으면 속성 값을 사용합니다 (GetPortOrProperty 패턴).


BGR 색상 순서로 색상을 지정합니다. 측정 라인이나 가이드 표시에 활용됩니다.


**응용 분야**


  - 측정 라인 표시

  - 가이드라인 오버레이

  - 검사 결과 마킹


#### Draw Rectangle

이미지 위에 사각형을 그리는 노드입니다. 채우기도 가능합니다 (Thickness = -1).


    X int

    Y int

    Width int

    Height int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | X | int | 입력 X |
| 입력 | Y | int | 입력 Y |
| 입력 | Width | int | 입력 Width |
| 입력 | Height | int | 입력 Height |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| X | int | 10 | X 좌표 (0~10000) |
| Y | int | 10 | Y 좌표 (0~10000) |
| Width | int | 100 | 너비 (1~10000) |
| Height | int | 100 | 높이 (1~10000) |
| ColorR | int | 0 | 빨강 (0~255) |
| ColorG | int | 255 | 초록 (0~255) |
| ColorB | int | 0 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (-1=채우기, -1~50) |


**기능 설명**

Draw Rectangle 노드는 지정 위치에 사각형을 그립니다. Thickness를 -1로 설정하면 내부를 채웁니다.


ROI 영역 표시, 검출 결과 강조, 관심 영역 마킹에 활용됩니다.


**응용 분야**


  - ROI 영역 표시

  - 검출 결과 사각형 표시

  - 관심 영역 마킹


#### Draw Circle

이미지 위에 원을 그리는 노드입니다. 중심과 반지름을 포트 또는 속성으로 지정합니다.


    CenterX int

    CenterY int

    Radius int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | CenterX | int | 입력 CenterX |
| 입력 | CenterY | int | 입력 CenterY |
| 입력 | Radius | int | 입력 Radius |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| CenterX | int | 100 | 중심 X (0~10000) |
| CenterY | int | 100 | 중심 Y (0~10000) |
| Radius | int | 50 | 반지름 (1~5000) |
| ColorR | int | 0 | 빨강 (0~255) |
| ColorG | int | 0 | 초록 (0~255) |
| ColorB | int | 255 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (-1=채우기, -1~50) |


**기능 설명**

Draw Circle 노드는 지정된 중심과 반지름으로 원을 그립니다. Thickness -1이면 채운 원을 그립니다.


검출된 원형 객체 강조, 마커 표시, 관심 포인트 표시에 활용됩니다.


**응용 분야**


  - 원형 객체 강조

  - 마커/포인트 표시

  - 검사 결과 시각화


#### Draw Ellipse

이미지 위에 타원을 그리는 노드입니다. 중심, 축 크기, 회전 각도를 지정할 수 있습니다.


    CenterX int

    CenterY int

    AxisW int

    AxisH int

    Angle double


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | CenterX | int | 입력 CenterX |
| 입력 | CenterY | int | 입력 CenterY |
| 입력 | AxisW | int | 입력 AxisW |
| 입력 | AxisH | int | 입력 AxisH |
| 입력 | Angle | double | 입력 Angle |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| CenterX | int | 100 | 중심 X (0~10000) |
| CenterY | int | 100 | 중심 Y (0~10000) |
| AxisW | int | 80 | 축 너비 (1~5000) |
| AxisH | int | 50 | 축 높이 (1~5000) |
| Angle | double | 0.0 | 회전 각도 (0~360도) |
| ColorR | int | 255 | 빨강 (0~255) |
| ColorG | int | 255 | 초록 (0~255) |
| ColorB | int | 0 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (-1=채우기, -1~50) |


**기능 설명**

Draw Ellipse 노드는 중심, 장축/단축, 회전 각도를 지정하여 타원을 그립니다. 0~360도 전체 호를 그립니다.


FitEllipse 결과를 시각화하거나, 타원형 ROI를 표시하는 데 활용됩니다.


**응용 분야**


  - 타원형 객체 강조

  - FitEllipse 결과 시각화

  - 타원 ROI 표시


#### Draw Text

이미지 위에 텍스트를 그리는 노드입니다. 폰트, 크기, 색상을 지정할 수 있습니다.


    PosX int

    PosY int

    Text string


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | PosX | int | 입력 PosX |
| 입력 | PosY | int | 입력 PosY |
| 입력 | Text | string | 입력 Text |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Text | string | Hello | 표시할 텍스트 |
| PosX | int | 10 | X 위치 (0~10000) |
| PosY | int | 30 | Y 위치 (0~10000) |
| Font | HersheyFonts | HersheySimplex | 폰트 종류 |
| Scale | double | 1.0 | 폰트 크기 (0.1~20.0) |
| ColorR | int | 255 | 빨강 (0~255) |
| ColorG | int | 255 | 초록 (0~255) |
| ColorB | int | 255 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (1~50) |


**기능 설명**

Draw Text 노드는 OpenCV의 PutText로 텍스트를 렌더링합니다. Text 입력 포트가 연결되면 동적 텍스트를, 없으면 속성 값을 사용합니다.


측정값 표시, 라벨링, 타임스탬프 오버레이 등에 활용됩니다. HersheyFonts 열거형으로 다양한 폰트를 선택합니다.


**응용 분야**


  - 측정값 라벨 표시

  - 결과 텍스트 오버레이

  - 타임스탬프/메타 정보 표시


#### Draw Crosshair

이미지 위에 십자선(레티클) 오버레이를 그리는 노드입니다. 중심 좌표 -1이면 자동 센터입니다.


    CenterX int

    CenterY int

    Size int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | CenterX | int | 입력 CenterX |
| 입력 | CenterY | int | 입력 CenterY |
| 입력 | Size | int | 입력 Size |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| CenterX | int | -1 | 중심 X (-1=자동 센터, -1~10000) |
| CenterY | int | -1 | 중심 Y (-1=자동 센터, -1~10000) |
| Size | int | 50 | 팔 길이 (10~1000) |
| Thickness | int | 1 | 두께 (1~5) |
| ColorR | int | 0 | 빨강 (0~255) |
| ColorG | int | 255 | 초록 (0~255) |
| ColorB | int | 0 | 파랑 (0~255) |


**기능 설명**

Draw Crosshair 노드는 수평/수직 직선으로 구성된 십자선을 그립니다. CenterX/Y가 -1이면 이미지 중앙을 자동 사용합니다.


정렬 검사, 중심점 확인, 조준 오버레이에 활용됩니다.


**응용 분야**


  - 정렬 검사 가이드

  - 중심점 시각화

  - 카메라 조준 오버레이


#### Draw Grid

이미지 위에 격자 오버레이를 그리는 노드입니다.


    CellWidth int

    CellHeight int


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | CellWidth | int | 입력 CellWidth |
| 입력 | CellHeight | int | 입력 CellHeight |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| CellWidth | int | 50 | 셀 너비 (5~500 픽셀) |
| CellHeight | int | 50 | 셀 높이 (5~500 픽셀) |
| Thickness | int | 1 | 두께 (1~5) |
| ColorR | int | 128 | 빨강 (0~255) |
| ColorG | int | 128 | 초록 (0~255) |
| ColorB | int | 128 | 파랑 (0~255) |


**기능 설명**

Draw Grid 노드는 지정된 셀 크기로 수직/수평 격자선을 이미지 전체에 그립니다.


간격 측정, 영역 분할 시각화, 캘리브레이션 확인에 활용됩니다.


**응용 분야**


  - 간격 측정 가이드

  - 영역 분할 시각화

  - 캘리브레이션 확인


#### Draw Polylines

이미지 위에 다각선(폴리라인)을 그리는 노드입니다. Point[][] 배열을 입력받습니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Points | Point[][] | 입력 Points |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| IsClosed | bool | true | 폴리라인 닫힘 여부 |
| ColorR | int | 255 | 빨강 (0~255) |
| ColorG | int | 0 | 초록 (0~255) |
| ColorB | int | 0 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (1~50) |


**기능 설명**

Draw Polylines 노드는 Point[][] 배열의 점들을 연결하여 다각선을 그립니다. IsClosed가 true이면 마지막 점과 첫 점을 연결합니다.


ApproxPoly, ConvexHull 등의 결과를 시각화하는 데 적합합니다.


**응용 분야**


  - 윤곽 근사 결과 시각화

  - 볼록 껍질 그리기

  - 다각형 ROI 표시


#### Draw Bounding Boxes

Rect[] 배열을 입력받아 바운딩 박스를 그리는 노드입니다. 크기 라벨도 표시할 수 있습니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Rects | Rect[] | 입력 Rects |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| ColorR | int | 0 | 빨강 (0~255) |
| ColorG | int | 255 | 초록 (0~255) |
| ColorB | int | 0 | 파랑 (0~255) |
| Thickness | int | 2 | 두께 (1~10) |
| ShowLabel | bool | true | 크기 라벨 표시 여부 |


**기능 설명**

Draw Bounding Boxes 노드는 BoundingRect 노드의 Rect[] 출력을 직접 연결하여 바운딩 박스를 그립니다. ShowLabel이 활성화되면 각 박스 위에 WxH 치수를 표시합니다.


객체 검출 결과 시각화의 마지막 단계로 자주 사용됩니다.


**응용 분야**


  - 객체 검출 결과 시각화

  - BoundingRect 결과 표시

  - 치수 라벨 오버레이


#### Draw Contours Info

윤곽선을 그리면서 인덱스, 면적, 중심 좌표 등의 정보를 라벨로 표시하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Contours | Point[][] | 입력 Contours |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| ShowCenter | bool | true | 중심점 표시 |
| ShowArea | bool | true | 면적 텍스트 표시 |
| ShowIndex | bool | true | 인덱스 번호 표시 |
| MinArea | double | 0 | 최소 표시 면적 (0~1000000) |
| FontScale | double | 0.4 | 폰트 크기 (0.1~5.0) |
| Thickness | int | 1 | 선 두께 (1~10) |


**기능 설명**

Draw Contours Info 노드는 윤곽선을 색상별로 구분하여 그리고, 각 윤곽선에 인덱스 번호(#N), 면적(A:xxx), 중심 좌표를 라벨로 표시합니다.


8가지 색상이 순환 사용되며, MinArea 이하의 작은 윤곽선은 건너뜁니다. 디버깅과 분석에 매우 유용합니다.


**응용 분야**


  - 윤곽선 분석 결과 종합 시각화

  - 디버깅용 상세 정보 표시

  - 검사 리포트 이미지 생성


## Chapter 19. Transform — 기하학적 변환


Transform 카테고리는 크기 조정, 회전, 자르기, 뒤집기, 어파인/원근 변환 등 기하학적 이미지 변환을 수행하는 8개 노드를 제공합니다.


#### Resize

이미지 크기를 조정하는 노드입니다. 목표 크기 또는 스케일 팩터로 지정합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Width | int | 0 | 목표 너비 (0=스케일 사용, 0~10000) |
| Height | int | 0 | 목표 높이 (0=스케일 사용, 0~10000) |
| ScaleX | double | 1.0 | 수평 스케일 (0.01~100.0) |
| ScaleY | double | 1.0 | 수직 스케일 (0.01~100.0) |
| Interpolation | InterpolationFlags | Linear | 보간법 |


**기능 설명**

Resize 노드는 Width/Height가 지정되면 해당 크기로, 0이면 ScaleX/ScaleY 비율로 이미지를 조정합니다.


Interpolation 속성으로 보간법을 선택합니다. Linear(기본), Nearest(고속), Cubic(고품질) 등이 있습니다.


**응용 분야**


  - 이미지 크기 정규화

  - 성능 최적화를 위한 축소

  - 출력 해상도 조정


#### Rotate

이미지를 지정 각도로 회전하는 노드입니다. 자동 중심 또는 수동 중심을 선택합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Angle | double | 0.0 | 회전 각도 (-360~360도) |
| AutoCenter | bool | true | 이미지 중심 자동 사용 |
| CenterX | int | 0 | 수동 중심 X (0~10000) |
| CenterY | int | 0 | 수동 중심 Y (0~10000) |


**기능 설명**

Rotate 노드는 Cv2.GetRotationMatrix2D로 회전 행렬을 생성하고 WarpAffine으로 적용합니다. AutoCenter가 true이면 이미지 중심을 기준으로 회전합니다.


양수 각도는 반시계 방향 회전입니다. 회전 후 잘리는 영역이 있을 수 있습니다.


**응용 분야**


  - 기울어진 이미지 보정

  - 객체 방향 정규화

  - 회전 변환 테스트


#### Crop

이미지에서 사각형 영역을 잘라내는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| X | int | 0 | 시작 X (0~10000) |
| Y | int | 0 | 시작 Y (0~10000) |
| Width | int | 100 | 크롭 너비 (1~10000) |
| Height | int | 100 | 크롭 높이 (1~10000) |


**기능 설명**

Crop 노드는 지정된 ROI 영역을 잘라내어 새 이미지로 반환합니다. 좌표는 자동으로 이미지 경계에 클램프됩니다.


BoundingRect 결과를 활용하여 검출된 객체만 잘라내거나, 관심 영역을 추출하는 데 사용합니다.


**응용 분야**


  - ROI 영역 추출

  - 검출 객체 크롭

  - 부분 이미지 분석


#### Flip

이미지를 수평, 수직 또는 양방향으로 뒤집는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| FlipCode | FlipDirection | Horizontal | 뒤집기 방향 (Horizontal/Vertical/Both) |


**기능 설명**

Flip 노드는 세 가지 방향으로 이미지를 뒤집습니다. Horizontal은 좌우, Vertical은 상하, Both는 180도 회전과 동일합니다.


카메라 미러링 보정이나 데이터 증강에 활용됩니다.


**응용 분야**


  - 카메라 미러링 보정

  - 데이터 증강

  - 이미지 방향 보정


#### Warp Affine

3쌍의 대응점을 사용하여 어파인 변환을 적용하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| SrcX0 | int | 0 | 소스 점0 X |
| SrcY0 | int | 0 | 소스 점0 Y |
| SrcX1 | int | 100 | 소스 점1 X |
| SrcY1 | int | 0 | 소스 점1 Y |
| SrcX2 | int | 0 | 소스 점2 X |
| SrcY2 | int | 100 | 소스 점2 Y |
| DstX0 | int | 10 | 대상 점0 X |
| DstY0 | int | 10 | 대상 점0 Y |
| DstX1 | int | 90 | 대상 점1 X |
| DstY1 | int | 10 | 대상 점1 Y |
| DstX2 | int | 10 | 대상 점2 X |
| DstY2 | int | 90 | 대상 점2 Y |


**기능 설명**

Warp Affine 노드는 3쌍의 소스/대상 대응점으로 2x3 어파인 변환 행렬을 계산하고 적용합니다. 이동, 회전, 스케일링, 전단을 동시에 수행합니다.


평행선이 보존되는 변환입니다. 이미지 정합이나 왜곡 보정에 활용됩니다.


**응용 분야**


  - 이미지 정합(Registration)

  - 왜곡 보정

  - 기하 변환 테스트


#### Warp Perspective

4쌍의 대응점을 사용하여 원근 변환을 적용하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| SrcX0 | int | 0 | 소스 점0 X |
| SrcY0 | int | 0 | 소스 점0 Y |
| SrcX1 | int | 100 | 소스 점1 X |
| SrcY1 | int | 0 | 소스 점1 Y |
| SrcX2 | int | 100 | 소스 점2 X |
| SrcY2 | int | 100 | 소스 점2 Y |
| SrcX3 | int | 0 | 소스 점3 X |
| SrcY3 | int | 100 | 소스 점3 Y |
| DstX0 | int | 10 | 대상 점0 X |
| DstY0 | int | 10 | 대상 점0 Y |
| DstX1 | int | 90 | 대상 점1 X |
| DstY1 | int | 10 | 대상 점1 Y |
| DstX2 | int | 90 | 대상 점2 X |
| DstY2 | int | 90 | 대상 점2 Y |
| DstX3 | int | 10 | 대상 점3 X |
| DstY3 | int | 90 | 대상 점3 Y |


**기능 설명**

Warp Perspective 노드는 4쌍의 대응점으로 3x3 원근 변환 행렬을 계산하고 적용합니다. 어파인 변환과 달리 평행선이 보존되지 않습니다.


문서 스캔 보정, 탑뷰 변환, 경사 보정 등에 활용됩니다.


**응용 분야**


  - 문서/카드 스캔 보정

  - 탑뷰(Bird Eye) 변환

  - 경사 보정

  - AR 이미지 합성


#### Pyramid

이미지 피라미드 연산(업스케일/다운스케일)을 수행하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Direction | PyramidDirection | Down | 피라미드 방향 (Up=확대, Down=축소) |


**기능 설명**

Pyramid 노드는 PyrUp(2배 확대) 또는 PyrDown(2배 축소)을 수행합니다. 가우시안 블러를 적용한 후 스케일링하여 안티앨리어싱 효과를 제공합니다.


다중 해상도 분석이나 이미지 크기 조정에 활용됩니다.


**응용 분야**


  - 다중 해상도 분석

  - 안티앨리어싱 축소

  - 피라미드 기반 처리


#### Distance Transform

이진 이미지의 거리 변환을 수행하는 노드입니다. 각 픽셀에서 가장 가까운 배경까지의 거리를 계산합니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| DistanceType | DistanceType | L2 | 거리 타입 (L1/L2/C) |
| MaskSize | DistanceMaskSize | Five | 마스크 크기 (Three/Five/Precise) |


**기능 설명**

Distance Transform 노드는 전경 픽셀에서 가장 가까운 배경(0) 픽셀까지의 거리를 계산합니다. 결과는 0-255로 정규화됩니다.


L2(유클리드)가 가장 정확하며, Watershed 마커 생성이나 객체 분리에 활용됩니다.


**응용 분야**


  - Watershed 마커 생성

  - 겹친 객체 분리

  - 골격화 전처리

  - 형태학적 분석


## Chapter 20. Histogram — 히스토그램 분석


Histogram 카테고리는 히스토그램 계산, 평활화, CLAHE, 역투영 등 밝기 분포 분석 및 보정을 위한 4개 노드를 제공합니다.


#### Calc Histogram

이미지의 밝기 히스토그램을 계산하고 시각화하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Histogram | Mat | 출력 Histogram |


**기능 설명**

Calc Histogram 노드는 그레이스케일(또는 자동 변환) 이미지의 256-bin 히스토그램을 계산합니다. 출력 Histogram은 각 밝기 레벨의 빈도를 담은 Mat입니다.


512x400 크기의 히스토그램 시각화 이미지가 미리보기에 표시됩니다. 밝기 분포 확인 및 노출 평가에 활용됩니다.


**응용 분야**


  - 밝기 분포 분석

  - 노출 적정성 평가

  - 이미지 품질 검사


#### Equalize Histogram

히스토그램 평활화로 이미지의 대비를 개선하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Equalize Histogram 노드는 Cv2.EqualizeHist()로 히스토그램을 균일 분포로 변환하여 대비를 향상시킵니다. 다채널 입력은 자동 그레이스케일 변환됩니다.


전체 이미지에 동일한 변환을 적용하므로 조명이 균일한 경우에 적합합니다. 불균일 조명에는 CLAHE를 권장합니다.


**응용 분야**


  - 대비 향상

  - 저조도 이미지 보정

  - 히스토그램 분포 정규화


#### CLAHE

CLAHE(Contrast Limited Adaptive Histogram Equalization)로 적응적 대비 향상을 수행하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| ClipLimit | double | 2.0 | 대비 제한 임계값 (0.0~100.0) |
| TileGridSize | int | 8 | 타일 격자 크기 (1~64) |


**기능 설명**

CLAHE 노드는 이미지를 타일로 분할하여 각 영역별로 독립적인 히스토그램 평활화를 적용합니다. ClipLimit으로 과도한 대비 증가를 방지합니다.


불균일 조명 환경에서 EqualizeHistogram보다 우수한 결과를 제공합니다. TileGridSize가 작을수록 더 세밀한 적응이 이루어집니다.


**응용 분야**


  - 불균일 조명 보정

  - 의료 영상 대비 향상

  - 국소 대비 개선

  - 전처리 파이프라인


#### Calc Back Project

히스토그램 역투영으로 타겟 영역과 유사한 색상 분포를 찾는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | TargetRegion | Mat | 입력 TargetRegion |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| HistBins | int | 180 | 히스토그램 빈 수 (1~256) |
| RangeMin | int | 0 | 범위 최소 (0~255) |
| RangeMax | int | 180 | 범위 최대 (0~255) |


**기능 설명**

Calc Back Project 노드는 TargetRegion의 Hue 히스토그램을 계산한 후, Image 전체에서 해당 색상 분포와 일치하는 영역을 찾아 확률 맵을 생성합니다.


HSV 색공간의 Hue 채널을 사용하므로 조명 변화에 강건합니다. 특정 색상의 객체를 추적하는 데 효과적입니다.


**응용 분야**


  - 색상 기반 객체 추적

  - 특정 색상 영역 검출

  - CamShift/MeanShift 전처리

  - 색상 유사도 맵 생성


## Chapter 21. Arithmetic — 산술 연산


Arithmetic 카테고리는 이미지 간 덧셈, 뺄셈, 곱셈, 차이, 비트 연산, 블렌딩, 마스크 적용 등 11개 산술 연산 노드를 제공합니다.


#### Add

두 이미지를 픽셀 단위로 더하는 노드입니다. 포화 연산이 적용됩니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Add 노드는 Cv2.Add()로 두 이미지를 픽셀 단위로 더합니다. 8비트 이미지의 경우 255를 초과하면 255로 클리핑(포화)됩니다.


밝기 증가, 이미지 합성, 노이즈 마스크 적용 등에 활용됩니다.


**응용 분야**


  - 이미지 밝기 증가

  - 이미지 합성

  - 마스크 합산


#### Subtract

두 이미지를 픽셀 단위로 빼는 노드입니다. 포화 연산이 적용됩니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Subtract 노드는 Cv2.Subtract()로 Image1에서 Image2를 뺍니다. 음수 결과는 0으로 클리핑됩니다.


배경 제거(이전 프레임 차이), 마스크 기반 영역 제거 등에 활용됩니다.


**응용 분야**


  - 배경 차분

  - 이미지 간 변화 검출

  - 밝기 감소


#### Multiply

두 이미지를 픽셀 단위로 곱하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Multiply 노드는 Cv2.Multiply()로 두 이미지를 요소별 곱셈합니다. 마스크 기반 영역 강조나 밝기 스케일링에 사용됩니다.


결과가 타입 범위를 초과하면 포화됩니다. 정밀한 곱셈이 필요하면 부동소수점 변환 후 처리합니다.


**응용 분야**


  - 마스크 기반 영역 강조

  - 밝기 스케일링

  - 가중 합성


#### Abs Diff

두 이미지의 절대 차이를 계산하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Abs Diff 노드는 Cv2.Absdiff()로 |Image1 - Image2|를 계산합니다. Subtract와 달리 음수 결과가 절대값으로 변환되어 양방향 변화를 모두 감지합니다.


프레임 차이 검출, 변화 감지, 이미지 비교에 필수적인 노드입니다.


**응용 분야**


  - 프레임 간 변화 감지

  - 이미지 비교/차이 분석

  - 결함 검출 (참조 대비)


#### Bitwise AND

두 이미지의 비트 AND 연산을 수행하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Bitwise AND 노드는 Cv2.BitwiseAnd()로 두 이미지의 각 픽셀에 비트 AND를 적용합니다. 마스크 적용의 기본 연산입니다.


마스크가 255인 영역만 원본을 유지하고, 0인 영역은 제거됩니다.


**응용 분야**


  - 마스크 적용

  - 영역 선택적 추출

  - 이진 마스크 교집합


#### Bitwise OR

두 이미지의 비트 OR 연산을 수행하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Bitwise OR 노드는 Cv2.BitwiseOr()로 두 이미지의 각 픽셀에 비트 OR을 적용합니다.


두 마스크의 합집합을 구하거나, 이미지를 중첩하는 데 활용됩니다.


**응용 분야**


  - 마스크 합집합

  - 이미지 중첩

  - 영역 병합


#### Bitwise XOR

두 이미지의 비트 XOR 연산을 수행하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Bitwise XOR 노드는 Cv2.BitwiseXor()로 두 이미지의 각 픽셀에 비트 XOR을 적용합니다. 동일한 영역은 0, 다른 영역만 남깁니다.


두 이미지의 차이 영역만 추출하는 데 유용합니다.


**응용 분야**


  - 이미지 차이 영역 추출

  - 마스크 배타적 영역

  - 변경점 감지


#### Bitwise NOT

이미지의 비트 반전(NOT)을 수행하는 노드입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 출력 | Result | Mat | 출력 Result |


**기능 설명**

Bitwise NOT 노드는 Cv2.BitwiseNot()으로 각 픽셀을 비트 반전합니다. 255에서 해당 값을 뺀 것과 동일합니다.


마스크 반전, 네거티브 이미지 생성에 활용됩니다. 단일 입력 노드입니다.


**응용 분야**


  - 마스크 반전

  - 네거티브 이미지 생성

  - 이진 마스크 보수


#### Weighted Add

두 이미지의 가중 합(alpha blending)을 수행하는 노드입니다. Result = Alpha*Image1 + Beta*Image2 + Gamma.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Alpha | double | 0.5 | Image1 가중치 (0.0~10.0) |
| Beta | double | 0.5 | Image2 가중치 (0.0~10.0) |
| Gamma | double | 0.0 | 스칼라 보정 (-255~255) |


**기능 설명**

Weighted Add 노드는 Cv2.AddWeighted()로 두 이미지를 가중 합산합니다. Alpha+Beta=1.0으로 설정하면 자연스러운 블렌딩이 됩니다.


Gamma로 전체 밝기를 조정할 수 있습니다. 반투명 오버레이, 이미지 합성에 활용됩니다.


**응용 분야**


  - 반투명 오버레이 합성

  - 이미지 블렌딩

  - 밝기 가중 조합


#### Image Blend

두 이미지를 Alpha 비율로 블렌딩하는 간편 노드입니다. Image2 가중치는 자동으로 (1-Alpha)가 됩니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image1 | Mat | 입력 Image1 |
| 입력 | Image2 | Mat | 입력 Image2 |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Alpha | double | 0.5 | Image1 블렌드 비율 (0.0~1.0) |


**기능 설명**

Image Blend 노드는 Alpha 하나만으로 블렌딩을 제어합니다. Image2의 크기/타입이 다르면 자동으로 Image1에 맞춰 리사이즈합니다.


간편한 투명도 합성에 최적화되어 있으며, Weighted Add의 단순화 버전입니다.


**응용 분야**


  - 간편 이미지 합성

  - 크로스페이드 효과

  - 오버레이 투명도 조절


#### Mask Apply

마스크를 이미지에 적용하여 관심 영역만 추출하는 노드입니다. BitwiseAnd 기반입니다.


**포트 상세**


| 방향 | 이름 | 타입 | 설명 |
| --- | --- | --- | --- |
| 입력 | Image | Mat | 입력 Image |
| 입력 | Mask | Mat | 입력 Mask |
| 출력 | Result | Mat | 출력 Result |


**속성**


| 이름 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Invert | bool | false | 마스크 반전 여부 |


**기능 설명**

Mask Apply 노드는 Cv2.BitwiseAnd(image, image, mask)로 마스크의 비제로 영역만 원본에서 추출합니다. 다채널 마스크는 자동 그레이스케일 변환됩니다.


Invert 속성을 활성화하면 마스크를 반전하여 마스크 외부 영역을 추출합니다.


**응용 분야**


  - ROI 마스킹

  - 배경 제거

  - 관심 영역만 추출

  - 마스크 기반 분석