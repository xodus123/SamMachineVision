## Chapter 22: Control 노드

Control 카테고리는 노드 그래프의 실행 흐름을 제어하는 노드들을 제공합니다. 조건 분기, 반복(루프), 비교 연산, 논리 연산 등을 통해 복잡한 처리 로직을 구성할 수 있습니다.


#### 22.1 Boolean

Boolean 노드는 참(true) 또는 거짓(false) 값을 생성하는 상수 노드입니다. 제어 흐름에서 조건값을 직접 지정할 때 사용합니다.


    Value bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Value | bool | false | 출력할 불리언 값 |


**기능 설명**

Boolean 노드는 입력 포트 없이 속성에서 지정한 불리언 값을 그대로 출력합니다. IfSelect, BooleanLogic 등의 조건 노드에 고정 조건을 제공하거나, 테스트 목적으로 특정 분기를 강제 활성화할 때 유용합니다.


런타임 중에도 속성 패널에서 값을 변경할 수 있어, 디버깅 시 특정 경로를 빠르게 전환하는 스위치 역할을 합니다.


**응용 분야**


- 조건 분기 노드의 고정 입력
- 디버깅 시 경로 전환 스위치
- 기능 활성화/비활성화 플래그


#### 22.2 Compare

Compare 노드는 두 개의 숫자 값을 비교하여 불리언 결과를 출력합니다. 6가지 비교 연산자를 지원합니다.


    A double

    B double

  Compare
Control

    Result bool


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| A | 입력 | double | 비교할 첫 번째 값 |
| B | 입력 | double | 비교할 두 번째 값 |
| Result | 출력 | bool | 비교 결과 |


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Operator | CompareOperator | Equal | 비교 연산자 (GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, Equal, NotEqual) |


**기능 설명**

Compare 노드는 두 실수 값 A와 B를 선택된 연산자로 비교합니다. Equal/NotEqual의 경우 부동소수점 오차를 고려하여 double.Epsilon 기반으로 비교합니다. 결과는 IfSelect, BreakIf 등의 조건 노드에 연결할 수 있습니다.


측정 결과가 허용 범위 내인지 판정하거나, 카운터 값이 목표에 도달했는지 확인하는 등 다양한 조건 판단에 활용됩니다.


**응용 분야**


- 측정값 합격/불합격 판정
- 임계값 기반 조건 분기
- 루프 종료 조건 생성


#### 22.3 Boolean Logic

Boolean Logic 노드는 두 불리언 입력에 대해 논리 연산을 수행합니다. AND, OR, XOR, NAND, NOR, NOT_A 게이트를 지원합니다.


    A bool

    B bool

  Boolean Logic
Control

    Result bool


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| A | 입력 | bool | 첫 번째 불리언 입력 |
| B | 입력 | bool | 두 번째 불리언 입력 |
| Result | 출력 | bool | 논리 연산 결과 |


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Gate | BoolLogicGate | AND | 논리 게이트 타입 (AND, OR, XOR, NAND, NOR, NOT_A) |


**기능 설명**

AND는 두 입력이 모두 참일 때만 참, OR는 하나라도 참이면 참, XOR는 둘 중 하나만 참일 때 참을 출력합니다. NAND/NOR는 각각 AND/OR의 부정이며, NOT_A는 입력 A의 부정값을 출력합니다.


여러 Compare 노드의 결과를 결합하여 복합 조건을 만들거나, 다중 검사 항목의 종합 합격 판정을 구성할 때 사용합니다.


**응용 분야**


- 복합 조건 결합 (예: 크기 OK AND 색상 OK)
- 다중 검사 항목 종합 판정
- 조건 부정 또는 배타적 분기


#### 22.4 If Select

If Select 노드는 조건에 따라 두 입력 중 하나를 선택하여 출력합니다. 프로그래밍의 삼항 연산자(condition ? a : b)에 해당합니다.


    Condition bool

    True Value object

    False Value object

  If Select
Control

    Result object


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| Condition | 입력 | bool | 선택 조건 |
| True Value | 입력 | object | 조건이 참일 때 출력할 값 |
| False Value | 입력 | object | 조건이 거짓일 때 출력할 값 |
| Result | 출력 | object | 선택된 값 |


**기능 설명**

Condition이 true이면 True Value를, false이면 False Value를 Result로 출력합니다. 한쪽만 연결된 경우 조건이 맞을 때만 해당 값을 출력하고 그렇지 않으면 null을 출력합니다.


이미지 처리 파이프라인에서 조건에 따라 다른 처리 경로의 결과를 선택하거나, 검사 결과에 따라 다른 메시지를 출력하는 분기 로직에 핵심적으로 사용됩니다.


**응용 분야**


- 조건별 이미지 처리 결과 선택
- 합격/불합격에 따른 메시지 분기
- 동적 파라미터 전환


#### 22.5 Switch

Switch 노드는 인덱스 값에 따라 최대 8개의 입력 중 하나를 선택하여 출력합니다. 다중 분기가 필요한 경우에 사용합니다.


    Index int

    Value 0~7 object

  Switch
Control

    Result object


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| Index | 입력 | int | 선택할 입력의 인덱스 (0~7) |
| Value 0 ~ Value 7 | 입력 | object | 선택 대상 값 (8개) |
| Result | 출력 | object | 선택된 값 |


**기능 설명**

Switch 노드는 Index 포트로 받은 정수 값(0~7)에 해당하는 입력 포트의 값을 Result로 출력합니다. 인덱스가 범위를 벗어나면 에러를 발생시킵니다. IfSelect가 2개 분기인 반면, Switch는 최대 8개 경로를 지원합니다.


모드 선택, 레시피 전환, 다중 카메라 입력 중 하나를 선택하는 등의 시나리오에서 활용됩니다.


**응용 분야**


- 처리 모드별 파이프라인 선택
- 다중 레시피/설정 전환
- 복수 카메라 입력 선택


#### 22.6 For

For 노드는 지정된 범위(Start~End)에서 Step 간격으로 반복하며, 하류 노드(body)를 반복 실행하는 ILoopNode 구현체입니다.


    Start int

    End int

    Step int

  For
Control

    Index int

    Count int

    IsRunning bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Start | int | 0 | 루프 시작 값 (포트 미연결 시 사용) |
| End | int | 10 | 루프 종료 값 (포트 미연결 시 사용) |
| Step | int | 1 | 증가 간격 (포트 미연결 시 사용) |
| Max Iterations | int | 10000 | 안전 제한 최대 반복 횟수 |


**기능 설명**

For 노드는 GetPortOrProperty 패턴으로 입력 포트가 연결되면 포트 값을, 미연결이면 속성 값을 사용합니다. 각 반복마다 하류 처리 노드들이 실행되며, Collect 노드를 통해 결과가 배열로 수집됩니다.


무한 루프 방지를 위해 MaxIterations 안전 제한이 있으며 Step은 최소 1로 강제됩니다. For(0,10,1) → ImageProcess → Collect 구성으로 10개 이미지를 순차 처리하고 결과를 배열로 모읍니다.


**응용 분야**


- 다중 이미지 순차 처리
- 파라미터 스윕 실험
- 배치 검사 루프


#### 22.7 ForEach

ForEach 노드는 배열이나 컬렉션의 각 요소를 순회하며 하류 노드를 반복 실행합니다.


    Collection object

  ForEach
Control

    Element object

    Index int

    Count int

    IsRunning bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Max Iterations | int | 10000 | 안전 제한 최대 반복 횟수 |


**기능 설명**

ForEach 노드는 입력 컬렉션을 object[] 배열로 변환 후 각 요소를 Element 포트로 순차 출력합니다. Array, IEnumerable, 단일 객체(1-element 배열 변환) 등 다양한 입력 타입을 지원합니다.


FindContours → ForEach → ContourArea → Collect 패턴으로 각 컨투어를 개별 분석하고 결과를 수집할 수 있습니다.


**응용 분야**


- 검출된 컨투어 개별 분석
- 복수 ROI 순차 검사
- 배열 데이터 요소별 변환


#### 22.8 For Loop

For Loop 노드는 내부 상태를 유지하며 매 실행마다 인덱스를 증가시키는 카운터형 노드입니다.

  For Loop
Control

    Index int

    IsRunning bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Start | int | 0 | 루프 시작 값 |
| End | int | 10 | 루프 종료 값 (미포함) |
| Step | int | 1 | 증가 간격 (최소 1) |


**기능 설명**

For Loop 노드는 내부 상태를 유지하며 매 Process() 호출마다 인덱스를 Step만큼 증가시킵니다. End에 도달하면 IsRunning을 false로 설정하고 시작값으로 리셋합니다.


런타임 모드에서 반복 실행되는 파이프라인에서 프레임 카운터나 순차 인덱스 생성기로 활용됩니다.


**응용 분야**


- 프레임 기반 순차 인덱스 생성
- 런타임 반복 카운터
- 순환 버퍼 인덱스


#### 22.9 While

While 노드는 최대 반복 횟수까지 하류 노드를 반복 실행합니다. BreakIf 노드와 함께 조건부 조기 종료가 가능합니다.

  While
Control

    Index int

    IsRunning bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Max Iterations | int | 100 | 최대 반복 횟수 (BreakIf로 조기 종료 가능) |


**기능 설명**

While 노드는 ILoopNode 인터페이스를 구현하며 MaxIterations 횟수만큼 반복하거나 BreakIf가 트리거될 때까지 반복합니다. 반복 횟수가 사전에 정해지지 않는 수렴 루프에 적합합니다.


예를 들어 이미지 처리 결과의 오차가 임계값 이하가 될 때까지 반복하는 최적화 루프를 구성할 수 있습니다.


**응용 분야**


- 수렴 조건 기반 반복 처리
- 재시도 로직 구현
- 조건부 반복 검사


#### 22.10 BreakIf

BreakIf 노드는 루프 내부에서 조건이 참이 되면 조기 종료를 신호합니다. IBreakSignal 인터페이스를 구현합니다.


    Condition bool

    Value object

  BreakIf
Control

    Value object


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| Condition | 입력 | bool | 중단 조건 (true이면 루프 종료) |
| Value (입력) | 입력 | object | 패스스루 값 |
| Value (출력) | 출력 | object | 패스스루 값 (그대로 전달) |


**기능 설명**

Condition이 true가 되면 ShouldBreak 플래그를 설정합니다. 루프 실행기는 바디 실행 후 이 플래그를 확인하여 루프를 종료합니다. Value 포트는 조건에 관계없이 값을 그대로 통과시킵니다.


While(Max=1000) → Process → BreakIf(error < threshold) 패턴으로 오차 수렴 시 루프를 종료하는 최적화 로직을 구성할 수 있습니다.


**응용 분야**


- 오차 수렴 시 루프 종료
- 목표 달성 감지 후 중단
- 이상 감지 시 처리 중단


#### 22.11 Collect

Collect 노드는 루프 반복 결과를 배열로 수집합니다. 루프 바디의 끝에 배치하여 경계 역할을 합니다. ILoopCollector 인터페이스를 구현합니다.


    Value object

  Collect
Control

    Result object[]

    Count int


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| Value | 입력 | object | 각 반복에서 수집할 값 |
| Result | 출력 | object[] | 수집된 결과 배열 |
| Count | 출력 | int | 수집된 요소 수 |


**기능 설명**

루프 시작 시 ClearCollection()으로 초기화, 매 반복마다 CollectIteration()으로 값 추가, 루프 종료 시 FinalizeCollection()으로 배열 출력합니다. Collect 하류 노드들은 루프 완료 후 한 번만 실행됩니다.


비루프 컨텍스트에서는 단일 값을 1-element 배열로 감싸서 출력합니다.


**응용 분야**


- 루프 결과 배열 수집
- 개별 분석 결과 통합
- 통계 처리를 위한 데이터 집계


#### 22.12 Delay

Delay 노드는 지정된 시간(밀리초) 동안 실행을 지연시킵니다. 런타임 모드에서만 실제 대기가 발생합니다.


    Milliseconds int

  Delay
Control

    Elapsed double


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Delay (ms) | int | 100 | 대기 시간 (1~60000 ms) |


**기능 설명**

런타임 모드(IsRuntimeMode)에서만 Thread.Sleep()을 통해 실제 대기가 발생하며, Stopwatch로 정확한 경과 시간을 측정하여 Elapsed로 출력합니다. 비런타임 모드에서는 대기 없이 0.0을 출력합니다.


장비 통신 후 응답 대기, 처리 간 타이밍 조절, 프레임 레이트 제한 등에 사용됩니다.


**응용 분야**


- 장비 응답 대기
- 처리 타이밍 동기화
- 런타임 루프 속도 제한


## Chapter 23: Data Processing 노드

Data 카테고리는 문자열 변환, 숫자 포맷팅, CSV 파일 처리 등 데이터 변환과 가공을 위한 노드들을 제공합니다. 측정 결과의 포맷팅, 외부 데이터 로드, 문자열 조작 등에 활용됩니다.


#### 23.1 String to Number

String to Number 노드는 문자열을 숫자(double)로 변환합니다.


    Text string

  String to Number
Data

    Value double


| 포트 | 방향 | 타입 | 설명 |
| --- | --- | --- | --- |
| Text | 입력 | string | 변환할 문자열 |
| Value | 출력 | double | 변환된 숫자 값 |


**기능 설명**

입력 문자열을 InvariantCulture 기반으로 double 값으로 파싱합니다. 빈 문자열이면 0.0을 출력하고, 파싱 불가능한 경우 에러를 발생시킵니다. 시리얼 통신이나 TCP로 수신한 숫자 문자열을 연산에 사용할 수 있도록 변환합니다.


**응용 분야**


- 시리얼/TCP 수신 데이터의 숫자 변환
- CSV 데이터의 숫자 필드 파싱
- 사용자 입력 문자열의 수치 변환


#### 23.2 Number to String

Number to String 노드는 숫자를 지정된 포맷의 문자열로 변환합니다.


    Value double

  Number to String
Data

    Text string


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Format | string | F2 | 숫자 포맷 문자열 (예: F2, N0, E3) |


**기능 설명**

입력 숫자를 지정된 포맷 문자열로 변환합니다. F2는 소수점 2자리 고정, N0은 천 단위 구분 정수, E3은 지수 표기 등 .NET 표준 포맷을 지원합니다. 측정 결과를 표시용 문자열로 변환하거나 통신 전송을 위한 포맷팅에 사용합니다.


**응용 분야**


- 측정 결과 포맷팅 출력
- 통신 전송용 문자열 생성
- 로그 기록용 숫자 포맷팅


#### 23.3 CSV Reader

CSV Reader 노드는 디스크에서 CSV 파일을 읽어 2차원 배열과 헤더를 출력합니다.

  CSV Reader
Data

    Data string[][]

    Headers string[]


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| File Path | FilePath | (빈값) | CSV 파일 경로 |
| Delimiter | string | , | 열 구분자 |
| Has Header | bool | true | 첫 행이 헤더인지 여부 |


**기능 설명**

지정된 경로의 CSV 파일을 읽어 모든 행을 2차원 문자열 배열로 출력합니다. Has Header가 true이면 첫 행을 Headers로 분리하고 나머지를 Data로 출력합니다. 파일이 존재하지 않거나 비어있으면 에러를 발생시킵니다.


**응용 분야**


- 검사 기준 데이터 로드
- 외부 설정 파일 읽기
- 측정 결과 이력 데이터 로드


#### 23.4 CSV Parser

CSV Parser 노드는 CSV 형식의 텍스트 문자열을 파싱하여 2차원 배열로 출력합니다.


    CsvText string

  CSV Parser
Data

    Data string[][]

    Headers string[]


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Delimiter | string | , | 열 구분자 |
| Has Header | bool | true | 첫 행이 헤더인지 여부 |


**기능 설명**

CSV Reader가 파일을 읽는 반면, CSV Parser는 입력 포트로 받은 문자열을 직접 파싱합니다. TCP/시리얼 통신으로 수신한 CSV 형식 데이터를 즉시 구조화된 배열로 변환할 수 있습니다.


**응용 분야**


- 통신 수신 CSV 데이터 파싱
- 동적 생성 CSV 텍스트 처리
- 메모리 내 CSV 데이터 구조화


#### 23.5 String Split

String Split 노드는 문자열을 구분자로 분할하여 배열로 출력합니다.


    Text string

  String Split
Data

    Parts string[]


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Delimiter | string | , | 분할 구분자 |


**기능 설명**

입력 문자열을 지정된 구분자로 분할하여 문자열 배열로 출력합니다. 통신으로 수신한 데이터를 개별 필드로 분리하거나, 경로 문자열을 분해하는 등의 용도로 사용됩니다.


**응용 분야**


- 수신 데이터 필드 분리
- 구분자 기반 텍스트 파싱
- ForEach와 연결한 개별 처리


#### 23.6 String Join

String Join 노드는 문자열 배열을 구분자로 결합하여 하나의 문자열로 출력합니다.


    Parts string[]

  String Join
Data

    Text string


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Delimiter | string | , | 결합 구분자 |


**기능 설명**

String Split의 역연산으로, 문자열 배열의 모든 요소를 지정된 구분자로 결합합니다. 측정 결과 배열을 CSV 형식 문자열로 변환하거나, 통신 전송용 메시지를 조합하는 데 사용됩니다.


**응용 분야**


- 측정 결과 CSV 문자열 생성
- 통신 전송 메시지 조합
- 로그 기록 문자열 생성


#### 23.7 String Replace

String Replace 노드는 문자열 내에서 특정 텍스트를 찾아 다른 텍스트로 치환합니다.


    Text string

  String Replace
Data

    Result string


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Find | string | (빈값) | 찾을 텍스트 |
| Replace | string | (빈값) | 치환할 텍스트 |


**기능 설명**

입력 문자열에서 Find에 해당하는 모든 부분을 Replace로 치환합니다. Find가 비어있으면 원본 문자열을 그대로 출력합니다. 통신 프로토콜의 특수 문자 처리, 파일 경로 변환, 텍스트 정리 등에 활용됩니다.


**응용 분야**


- 통신 프로토콜 특수 문자 처리
- 파일 경로 문자열 변환
- 출력 텍스트 서식 정리


## Chapter 24: Communication 노드

Communication 카테고리는 외부 장비 및 시스템과의 통신을 위한 노드를 제공합니다. 시리얼 포트(RS-232), TCP 서버/클라이언트를 통해 PLC, 센서, 외부 소프트웨어와 데이터를 교환할 수 있습니다. 모든 통신 노드는 IBackgroundNode 인터페이스를 구현하여 백그라운드 스레드에서 비동기 수신을 처리합니다.


#### 24.1 Serial Port

Serial Port 노드는 RS-232 시리얼 통신을 통해 데이터를 송수신합니다. ASCII 및 Hex 모드를 지원합니다.


    Send Data string

  Serial Port
Communication

    Received Data string

    IsOpen bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Port Name | string | COM1 | 시리얼 포트 이름 |
| Baud Rate | BaudRateOption | 9600 | 통신 속도 (9600~115200) |
| Data Bits | int | 8 | 데이터 비트 (5~8) |
| Stop Bits | StopBits | One | 정지 비트 |
| Parity | Parity | None | 패리티 설정 |
| Data Mode | DataModeOption | ASCII | 데이터 모드 (ASCII 또는 Hex) |


**기능 설명**

Serial Port 노드는 백그라운드 스레드에서 10ms 간격으로 수신 데이터를 폴링하여 버퍼에 저장합니다. Process() 호출 시 버퍼의 데이터를 Received Data로 출력하고, Send Data 포트에 연결된 값을 전송합니다. 포트 설정이 변경되면 자동으로 재연결합니다.


Hex 모드에서는 바이트 데이터를 공백 구분 16진수 문자열("FF A0 01")로 변환하여 송수신합니다. ASCII 모드에서는 일반 텍스트로 통신합니다.


**응용 분야**


- PLC와의 시리얼 통신
- 바코드 리더기 데이터 수신
- 센서 데이터 실시간 수집


#### 24.2 TCP Server

TCP Server 노드는 지정된 포트에서 TCP 연결을 대기하고 클라이언트와 데이터를 송수신합니다.


    Send Data string

  TCP Server
Communication

    Received Data string

    IsRunning bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Port | int | 5000 | 리슨 포트 번호 (1~65535) |


**기능 설명**

TcpListener를 사용하여 지정된 포트에서 연결을 대기합니다. 클라이언트 연결 시 백그라운드 스레드에서 데이터를 수신하고, UTF-8 인코딩으로 문자열 변환하여 버퍼에 저장합니다. 포트 변경 시 자동으로 서버를 재시작합니다.


외부 시스템에서 MVXTester로 명령이나 데이터를 전송하는 서버 역할을 합니다. IsDirty 플래그를 통해 새 데이터 수신 시 파이프라인을 자동 재실행합니다.


**응용 분야**


- 외부 시스템으로부터 트리거 수신
- 검사 명령 수신 서버
- 결과 데이터 양방향 교환


#### 24.3 TCP Client

TCP Client 노드는 원격 TCP 서버에 연결하여 데이터를 송수신합니다.


    Send Data string

  TCP Client
Communication

    Received Data string

    IsConnected bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Host | string | 127.0.0.1 | 서버 호스트명 또는 IP |
| Port | int | 5000 | 서버 포트 번호 (1~65535) |


**기능 설명**

지정된 호스트와 포트로 TCP 연결을 수행합니다. 연결 후 백그라운드 스레드에서 수신 데이터를 폴링하며, UTF-8 문자열로 변환하여 버퍼에 저장합니다. 호스트나 포트 변경 시 자동 재연결합니다.


MVXTester에서 외부 서버로 검사 결과를 전송하거나, 원격 시스템의 데이터를 수신하는 클라이언트 역할을 합니다.


**응용 분야**


- 검사 결과 외부 전송
- MES/SCADA 시스템 연동
- 원격 서버 데이터 수신


## Chapter 25: Event 노드

Event 카테고리는 사용자 인터랙션(키보드, 마우스)을 노드 그래프에서 활용할 수 있게 합니다. ImageShow 창에서 발생하는 입력 이벤트를 수신하여 실시간 상호작용이 가능한 비전 애플리케이션을 구성할 수 있습니다.


#### 25.1 Keyboard Event

Keyboard Event 노드는 ImageShow 창에서 발생하는 키보드 이벤트를 수신합니다.

  Keyboard Event
Event

    KeyCode int

    KeyName string

    IsPressed bool


**기능 설명**

IKeyboardEventReceiver 인터페이스와 RuntimeEventBus.KeyEvent를 통해 키보드 이벤트를 수신합니다. KeyDown 시 IsPressed가 true, KeyUp 시 false로 설정됩니다. 새 이벤트 수신 시 IsDirty를 설정하여 파이프라인 재실행을 트리거합니다.


키 입력에 따른 모드 전환, 파라미터 조정, 처리 시작/중지 등의 인터랙티브 제어에 활용됩니다.


**응용 분야**


- 키 입력 기반 모드 전환
- 인터랙티브 파라미터 조정
- 수동 트리거 입력


#### 25.2 Mouse Event

Mouse Event 노드는 ImageShow 창에서 발생하는 마우스 이벤트(클릭, 이동)를 수신합니다.

  Mouse Event
Event

    X int

    Y int

    EventType string

    Button int

    IsPressed bool


**기능 설명**

IMouseEventReceiver 인터페이스와 RuntimeEventBus.MouseEvent를 통해 마우스 이벤트를 수신합니다. LeftDown/RightDown/MiddleDown 시 IsPressed가 true, Up 이벤트 시 false가 됩니다. X, Y는 이미지 좌표계 기준 마우스 위치입니다.


이미지 위의 특정 위치 선택, 클릭 기반 ROI 지정, 마우스 추적 등에 활용됩니다.


**응용 분야**


- 이미지 좌표 기반 위치 선택
- 클릭 기반 객체 선택
- 실시간 마우스 추적


#### 25.3 Mouse ROI

Mouse ROI 노드는 마우스 드래그로 ImageShow 창에서 ROI(관심 영역) 사각형을 그립니다.

  Mouse ROI
Event


    IsDrawing bool


**기능 설명**

마우스 왼쪽 버튼 누름(LeftDown)에서 시작점을 기록하고, 드래그 중 Move 이벤트로 끝점을 업데이트하며, 버튼 놓음(LeftUp) 시 최종 사각형을 확정합니다. 시작점과 끝점으로부터 정규화된 Rect(x, y, width, height)를 계산하여 출력합니다.


사용자가 실시간으로 관심 영역을 지정하고, 해당 영역만 검사하거나 크롭하는 인터랙티브 워크플로우를 구성할 수 있습니다.


**응용 분야**


- 인터랙티브 ROI 지정
- 관심 영역 기반 부분 검사
- 실시간 크롭 영역 선택


#### 25.4 WaitKey

WaitKey 노드는 지정된 시간 동안 키 입력을 대기합니다. OpenCV의 cv2.waitKey()와 유사하며, IStreamingSource를 구현하여 런타임 루프를 유지합니다.


    Delay int

  WaitKey
Event

    KeyCode int

    KeyName string

    IsTimeout bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Delay (ms) | int | 1 | 대기 시간 (1~30000 ms) |


**기능 설명**

런타임 모드에서 지정된 시간 동안 1ms 간격으로 키 이벤트를 폴링합니다. 키 입력이 감지되면 즉시 반환하고, 타임아웃 시 IsTimeout을 true로 설정합니다. IStreamingSource 마커 인터페이스를 구현하여 이 노드가 포함된 그래프는 런타임 모드에서 지속적으로 실행됩니다.


이미지 표시 후 사용자 키 입력을 대기하거나, 연속 프레임 처리에서 최소 대기를 보장하는 용도로 사용됩니다.


**응용 분야**


- 이미지 표시 후 키 입력 대기
- 스트리밍 루프 프레임 간 대기
- 사용자 확인 대기


## Chapter 26: Script 노드

Script 카테고리는 Python과 C# 스크립트를 노드 그래프 내에서 실행할 수 있게 합니다. 기본 제공 노드로 구현하기 어려운 커스텀 처리를 스크립트로 작성하여 유연하게 확장할 수 있습니다.


#### 26.1 Python Script

Python Script 노드는 시스템에 설치된 Python을 사용하여 이미지 처리 스크립트를 실행합니다.


  Python Script
Script


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Python Script | MultilineString | (기본 스크립트) | 실행할 Python 스크립트. sys.argv[1]=입력경로, sys.argv[2]=출력경로 |
| Python Path | string | python | Python 실행 파일 경로 |
| Timeout (ms) | int | 30000 | 스크립트 실행 타임아웃 |


**기능 설명**

입력 이미지를 임시 PNG 파일로 저장하고, Python 프로세스를 실행하여 스크립트를 수행합니다. 스크립트는 sys.argv[1]로 입력 이미지 경로를, sys.argv[2]로 출력 이미지 경로를 받습니다. 실행 완료 후 출력 파일을 읽어 Result로 출력합니다.


OpenCV(cv2), NumPy, scikit-image 등 Python 생태계의 풍부한 라이브러리를 활용할 수 있어, 딥러닝 추론이나 고급 이미지 처리 알고리즘을 통합하는 데 적합합니다. 타임아웃을 초과하면 프로세스를 강제 종료합니다.


**응용 분야**


- 딥러닝 모델 추론 통합
- 고급 Python 라이브러리 활용
- 커스텀 이미지 처리 알고리즘


#### 26.2 C# Script

C# Script 노드는 .NET SDK를 사용하여 OpenCvSharp 기반 C# 스크립트를 실행합니다.


  C# Script
Script


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| C# Script | MultilineString | (기본 스크립트) | 실행할 C# 스크립트. args[0]=입력경로, args[1]=출력경로 |
| Timeout (ms) | int | 60000 | 스크립트 실행 타임아웃 (첫 실행은 빌드로 인해 느림) |


**기능 설명**

임시 디렉토리에 .csproj 프로젝트를 자동 생성하고, OpenCvSharp4 NuGet 패키지를 참조하는 C# 프로젝트로 스크립트를 실행합니다. 현재 로드된 OpenCvSharp 버전과 런타임 패키지(win/linux/osx)를 자동 감지하여 호환성을 보장합니다.


dotnet run 명령으로 실행되므로 첫 실행 시 빌드 시간이 소요됩니다. MVXTester와 동일한 .NET 환경에서 실행되어 타입 호환성이 우수하며, 복잡한 알고리즘이나 커스텀 로직 구현에 적합합니다.


**응용 분야**


- 커스텀 OpenCvSharp 처리
- 복잡한 알고리즘 프로토타이핑
- .NET 라이브러리 활용 확장


## Chapter 27: Inspection 노드

Inspection 카테고리는 산업용 비전 검사를 위한 고수준 노드를 제공합니다. 색상/형상 기반 객체 검출, 결함 감지, 정렬 확인, 패턴 매칭, 밝기 균일성 검사 등 생산 라인에서 필요한 다양한 검사 기능을 단일 노드로 구현합니다. 각 노드는 결과 이미지에 검사 결과를 시각화하여 오버레이합니다.


#### 27.1 Color Object Detector

Color Object Detector 노드는 HSV 색공간에서 특정 색상 범위의 객체를 검출하고 위치와 개수를 출력합니다.


  Color Object Detector
Inspection


    Count int


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Hue Low / High | int | 100 / 130 | 색상(Hue) 범위 (0~179) |
| Saturation Low / High | int | 50 / 255 | 채도 범위 (0~255) |
| Value Low / High | int | 50 / 255 | 명도 범위 (0~255) |
| Min Area | double | 500.0 | 최소 객체 면적 (px) |
| Show Mask | bool | false | 이진 마스크 표시 여부 |


**기능 설명**

입력 이미지를 BGR에서 HSV로 변환한 후, 지정된 HSV 범위로 InRange 마스킹을 수행합니다. 모폴로지 Close 연산으로 작은 구멍을 메우고, 컨투어를 검출하여 MinArea 이상의 객체만 필터링합니다. 각 객체의 바운딩 박스, 중심점, 면적을 계산하여 출력합니다.


결과 이미지에는 녹색 바운딩 박스, 빨간 중심점, 인덱스와 면적 레이블이 오버레이됩니다. Show Mask 옵션으로 이진 마스크를 직접 확인할 수도 있습니다.


**응용 분야**


- 색상 기반 부품 분류
- 컬러 마커 위치 검출
- 과일/식품 색상 품질 검사


#### 27.2 Object Counter

Object Counter 노드는 이미지에서 객체를 검출하고 개수, 중심 좌표, 면적을 출력합니다.


  Object Counter
Inspection


    Count int


    Areas double[]


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 커널 크기 |
| Use Adaptive Threshold | bool | false | 적응형 이진화 사용 여부 |
| Block Size | int | 11 | 적응형 이진화 블록 크기 |
| Constant | double | 2.0 | 적응형 이진화 상수 |
| Invert Binary | bool | true | 이진화 반전 (어두운 객체 기준) |
| Min/Max Area | double | 200 / 10000000 | 객체 면적 범위 |
| Morph Kernel Size | int | 3 | 모폴로지 커널 크기 (0=건너뜀) |


**기능 설명**

그레이스케일 변환, 가우시안 블러, Otsu 또는 적응형 이진화, 모폴로지 Close, 컨투어 검출의 파이프라인을 자동 수행합니다. 면적 범위 필터링 후 모멘트 계산으로 정확한 중심좌표를 구합니다. 결과 이미지에 컨투어(녹색), 번호 레이블(빨간), 총 개수 표시(노란)를 오버레이합니다.


**응용 분야**


- 부품 개수 카운팅
- 알약/캡슐 계수
- PCB 부품 누락 검사


#### 27.3 Contour Center Finder

Contour Center Finder 노드는 이미지에서 객체를 검출하고 모멘트 기반으로 정확한 중심 좌표를 계산합니다.


  Contour Center Finder
Inspection


    Areas double[]

    Count int


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 커널 크기 |
| Auto Threshold | bool | true | Otsu 자동 임계값 사용 |
| Threshold | int | 128 | 수동 임계값 (Auto 해제 시) |
| Invert Binary | bool | false | 이진화 반전 |
| Min/Max Area | double | 100 / 10000000 | 면적 범위 |
| Draw Radius | int | 5 | 중심점 표시 반경 |


**기능 설명**

Otsu 또는 수동 이진화 후 모폴로지 Open으로 노이즈를 제거하고, 컨투어를 검출합니다. 각 컨투어의 모멘트(M00, M10, M01)를 계산하여 정밀 중심좌표를 구합니다. 결과 이미지에 컨투어(녹색), 중심점(빨간), 좌표 레이블을 표시합니다.


**응용 분야**


- 부품 위치 정밀 계측
- 로봇 픽앤플레이스 좌표 산출
- 정렬 기준점 검출


#### 27.4 Shape Classifier

Shape Classifier 노드는 검출된 컨투어를 Circle, Rectangle, Triangle, Square, Polygon으로 분류합니다.


  Shape Classifier
Inspection


    Count int

    ShapeList string


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 커널 크기 |
| Threshold | int | 128 | 이진화 임계값 |
| Use Otsu | bool | true | Otsu 자동 이진화 사용 |
| Min Area | double | 500.0 | 최소 컨투어 면적 |
| Approx Epsilon | double | 0.04 | 컨투어 근사 정밀도 (둘레 비율) |


**기능 설명**

컨투어를 ApproxPolyDP로 근사화한 후 꼭짓점 수로 형상을 분류합니다. 3개=Triangle, 4개일 때 종횡비 0.85~1.15이면 Square 아니면 Rectangle, 5개 이상이면서 원형도(circularity)가 0.8 초과이면 Circle, 나머지는 Polygon으로 판정합니다.


ShapeList 출력은 쉼표 구분 문자열로 모든 검출 형상을 나열합니다. 결과 이미지에 형상별 다른 색상으로 컨투어와 레이블을 표시합니다.


**응용 분야**


- 부품 형상별 분류
- 조립 올바름 검사
- 형상 기반 불량 감지


#### 27.5 Circle Detector

Circle Detector 노드는 허프 원 변환을 사용하여 원형 객체를 검출하고 중심과 반지름을 출력합니다.


  Circle Detector
Inspection


    Radii double[]

    Count int


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 9 | 전처리 블러 커널 |
| Dp | double | 1.2 | 역 누적기 비율 |
| Min Distance | double | 50.0 | 중심 간 최소 거리 |
| Canny Threshold | double | 100.0 | Canny 상위 임계값 |
| Accum Threshold | double | 50.0 | 누적기 임계값 |
| Min/Max Radius | int | 10 / 200 | 원 반지름 범위 |


**기능 설명**

HoughCircles(Gradient 방식)를 사용하여 원형 객체를 검출합니다. 가우시안 블러 전처리로 노이즈를 줄이고, 반지름 범위로 필터링합니다. 결과 이미지에 녹색 원, 빨간 중심점, 반지름 레이블을 표시합니다.


**응용 분야**


- 볼트/너트/와셔 검출
- 원형 부품 위치 측정
- 구멍/홀 검사


#### 27.6 Edge Inspector

Edge Inspector 노드는 Hough 변환으로 직선 에지를 검출하고 각도를 분석합니다.


  Edge Inspector
Inspection


    LineCount int

    Angles double[]

    AverageAngle double


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 크기 |
| Canny Low / High | int | 50 / 150 | Canny 에지 임계값 |
| Min Line Length | double | 50.0 | 최소 선분 길이 |
| Max Line Gap | double | 10.0 | 선분 간 최대 간격 |
| Rho | double | 1.0 | 거리 해상도 (px) |


**기능 설명**

Canny 에지 검출 후 HoughLinesP로 직선 세그먼트를 검출합니다. 각 선분의 각도를 atan2로 계산하고 0~180도 범위로 정규화합니다. 결과 이미지에 녹색 선분과 각도 레이블, 요약 통계를 표시합니다.


**응용 분야**


- 에지 직진도 검사
- 정렬 각도 측정
- 직선 패턴 검출


#### 27.7 Face Detector

Face Detector 노드는 Haar 캐스케이드 분류기를 사용하여 이미지에서 얼굴을 검출합니다.


  Face Detector
Inspection


    Count int


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Scale Factor | double | 1.1 | 다중 스케일 검출 비율 |
| Min Neighbors | int | 5 | 신뢰도 최소 이웃 수 |
| Min/Max Face Size | int | 30 / 0 | 얼굴 크기 범위 (px, 0=무제한) |
| Cascade File | string | haarcascade_frontalface_default.xml | 캐스케이드 파일명 |


**기능 설명**

그레이스케일 변환, 히스토그램 균일화, 다중 스케일 검출 파이프라인을 수행합니다. 대형 이미지는 800px 폭으로 리사이즈하여 속도를 최적화하고, 검출 좌표를 원본 해상도로 역변환합니다. 캐스케이드 파일은 앱 디렉토리, data 하위폴더 등에서 자동 탐색합니다.


**응용 분야**


- 출입 관리 시스템
- 영상 감시 모니터링
- 사용자 인터페이스 얼굴 인식


#### 27.8 Brightness Uniformity

Brightness Uniformity 노드는 이미지를 그리드로 분할하여 밝기 균일성을 검사합니다.


  Brightness Uniformity
Inspection


    MeanBrightness double

    StdDev double

    MinBrightness double

    MaxBrightness double

    IsUniform bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Grid Columns | int | 4 | 분석 그리드 열 수 |
| Grid Rows | int | 4 | 분석 그리드 행 수 |
| Max Std Dev | double | 30.0 | 균일 판정 최대 표준편차 |


**기능 설명**

이미지를 Grid Rows x Grid Columns 그리드로 분할하고 각 셀의 평균 밝기를 계산합니다. 전체 셀 밝기의 표준편차가 Max Std Dev 이하이면 PASS(균일), 초과이면 FAIL(불균일)로 판정합니다. 결과 이미지에 각 셀의 밝기 값과 합격/불합격 색상 오버레이, PASS/FAIL 배너를 표시합니다.


**응용 분야**


- 디스플레이 패널 밝기 균일성
- 조명 품질 검사
- 코팅/인쇄 균일도 확인


#### 27.9 Pattern Matcher

Pattern Matcher 노드는 템플릿 매칭으로 이미지에서 패턴의 모든 출현 위치를 찾고 합격/불합격을 판정합니다.


  Pattern Matcher
Inspection


    Count int

    Pass bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Match Threshold | double | 0.8 | 최소 매칭 점수 |
| Expected Min / Max | int | 1 / 100 | 기대 매칭 개수 범위 |
| NMS Overlap | double | 0.3 | NMS 중첩 임계값 |


**기능 설명**

CCoeffNormed 방식의 템플릿 매칭 수행 후 임계값 이상인 모든 위치를 후보로 추출합니다. IoU 기반 비최대 억제(NMS)로 중복 검출을 제거합니다. 매칭 개수가 Expected 범위 내이면 PASS, 범위 밖이면 FAIL로 판정합니다.


**응용 분야**


- PCB 부품 유무 검사
- 라벨/마커 위치 확인
- 반복 패턴 개수 검증


#### 27.10 Defect Detector

Defect Detector 노드는 검사 이미지를 기준 이미지와 비교하여 결함/차이를 검출합니다.


  Defect Detector
Inspection


    Count int


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 비교 전 블러 크기 |
| Threshold | int | 30 | 차이 임계값 |
| Min Defect Area | double | 50.0 | 최소 결함 면적 (px) |
| Morph Kernel | int | 3 | 노이즈 제거 모폴로지 커널 |
| Show Overlay | bool | true | 원본에 결함 오버레이 표시 |


**기능 설명**

검사 이미지와 참조 이미지의 절대차(Absdiff)를 계산하고, 블러와 이진화로 차이 영역을 추출합니다. 모폴로지 Open/Close로 노이즈를 제거하고 면적 필터링으로 의미 있는 결함만 검출합니다. Show Overlay 모드에서는 결함 영역을 반투명 빨간색으로 원본 이미지에 오버레이합니다.


**응용 분야**


- 제품 표면 결함 검사
- 인쇄물 불량 검출
- 조립 상태 변화 감지


#### 27.11 Alignment Checker

Alignment Checker 노드는 객체의 방향 각도를 측정하고 기대 각도와의 편차로 정렬 상태를 판정합니다.


  Alignment Checker
Inspection


    Angle double

    AngleError double

    Pass bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 크기 |
| Min Area | double | 1000.0 | 최소 컨투어 면적 |
| Expected Angle | double | 0.0 | 기대 방향 각도 (-180~180) |
| Angle Tolerance | double | 5.0 | 허용 각도 편차 |
| Invert Binary | bool | false | 이진화 반전 |


**기능 설명**

가장 큰 컨투어를 검출하고 FitEllipse(5점 이상) 또는 MinAreaRect로 방향 각도를 산출합니다. 기대 각도와의 최단 각도 차이를 계산하여 Tolerance 이내이면 PASS, 초과이면 FAIL로 판정합니다. 결과 이미지에 피팅 타원, 방향선, 각도 레이블과 합격 상태를 표시합니다.


**응용 분야**


- 부품 방향 정렬 검사
- 컨베이어 위 제품 회전 확인
- 인서트 삽입 각도 검증


#### 27.12 Presence Checker

Presence Checker 노드는 지정된 ROI 영역에서 특정 객체/특징의 존재 여부를 판정합니다.


  Presence Checker
Inspection


    FillRatio double

    PixelCount int

    Pass bool


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| ROI X / Y | int | 0 / 0 | ROI 좌상단 좌표 |
| ROI Width / Height | int | 100 / 100 | ROI 크기 |
| Threshold | int | 128 | 이진화 임계값 |
| Invert Binary | bool | false | 이진화 반전 |
| Min/Max Fill Ratio | double | 0.3 / 1.0 | 합격 판정 충전율 범위 |


**기능 설명**

지정된 ROI를 추출하여 이진화한 후 비제로 픽셀 비율(Fill Ratio)을 계산합니다. Fill Ratio가 Min~Max 범위 내이면 PASS(존재), 범위 밖이면 FAIL(부재 또는 과잉)로 판정합니다. ROI는 이미지 경계에 자동 클램핑됩니다.


**응용 분야**


- 부품 유무 확인
- 라벨 부착 확인
- 특정 위치 객체 존재 검사


#### 27.13 Scratch Detector

Scratch Detector 노드는 표면의 선형 스크래치와 결함을 검출합니다.


  Scratch Detector
Inspection


    Count int

    TotalLength double


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 3 | 가우시안 블러 커널 |
| Morph Kernel Size | int | 15 | 모폴로지 커널 (클수록 넓은 스크래치 감지) |
| Threshold | int | 30 | 감도 임계값 |
| Min Length | double | 30.0 | 최소 스크래치 길이 |
| Min Elongation | double | 3.0 | 최소 장축/단축 비율 |
| Detect Mode | ScratchDetectMode | DarkOnLight | 검출 모드 (어두운/밝은 스크래치) |


**기능 설명**

DarkOnLight 모드에서는 BlackHat, LightOnDark 모드에서는 TopHat 모폴로지 연산으로 선형 특징을 강조합니다. 이진화 후 MinAreaRect로 장축/단축 비율(elongation)과 길이를 측정하여 스크래치 여부를 판별합니다. 결과 이미지에 빨간색 오버레이로 스크래치를 표시하고 총 개수와 길이를 표시합니다.


**응용 분야**


- 금속 표면 스크래치 검사
- 유리/렌즈 흠집 검출
- 도장면 긁힘 불량 검사


## Chapter 28: Measurement 노드

Measurement 카테고리는 이미지에서 객체의 물리적 치수를 측정하는 노드를 제공합니다. 폭/높이, 두 객체 간 거리, 선분 간 각도를 픽셀 또는 실제 단위로 계측할 수 있습니다. Pixels Per Unit 파라미터를 통해 캘리브레이션된 실제 단위 측정이 가능합니다.


#### 28.1 Object Measure

Object Measure 노드는 검출된 객체의 폭과 높이를 측정합니다. MinAreaRect를 사용하여 회전된 객체도 정확하게 측정합니다.


  Object Measure
Measurement


    Widths double[]

    Heights double[]

    Count int


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 커널 크기 |
| Min Area | double | 500.0 | 최소 컨투어 면적 |
| Pixels Per Unit | double | 1.0 | 단위당 픽셀 수 (1=픽셀 단위) |
| Unit Name | string | px | 표시 단위명 |
| Invert Binary | bool | false | 이진화 반전 |


**기능 설명**

Otsu 이진화로 객체를 분리하고, 각 컨투어에 MinAreaRect를 피팅하여 회전된 최소 외접 사각형의 폭과 높이를 측정합니다. 일관성을 위해 항상 긴 변을 Width, 짧은 변을 Height로 정규화합니다. Pixels Per Unit으로 나누어 실제 단위로 변환합니다.


결과 이미지에 녹색 MinAreaRect, 노란 치수 레이블("W x H 단위")을 표시합니다. 캘리브레이션 기준 객체(예: 알려진 치수의 참조물)를 이용하여 Pixels Per Unit을 설정하면 mm, cm 등 실제 단위로 측정할 수 있습니다.


**응용 분야**


- 부품 치수 검사
- 제품 크기 합격 판정
- 실시간 치수 모니터링


#### 28.2 Distance Measure

Distance Measure 노드는 이미지에서 가장 큰 두 객체 사이의 거리를 측정합니다.


  Distance Measure
Measurement


    Distance double


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 커널 크기 |
| Min Area | double | 200.0 | 최소 컨투어 면적 |
| Pixels Per Unit | double | 1.0 | 단위당 픽셀 수 |
| Unit Name | string | px | 표시 단위명 |
| Invert Binary | bool | false | 이진화 반전 |


**기능 설명**

면적 기준으로 상위 2개 객체를 선택하고, 모멘트를 이용하여 각 객체의 중심점을 계산합니다. 두 중심점 간 유클리드 거리를 계산하고 Pixels Per Unit으로 나누어 실제 단위 거리를 산출합니다. 최소 2개 이상의 객체가 필요하며, 부족하면 에러를 발생시킵니다.


결과 이미지에 두 객체의 컨투어(녹색), 중심점(파란), 연결선(빨간), 거리 레이블(노란)을 표시합니다.


**응용 분야**


- 부품 간 간격 측정
- 핀/홀 간 피치 검사
- 조립 간격 합격 판정


#### 28.3 Angle Measure

Angle Measure 노드는 이미지에서 두 주요 직선 사이의 각도를 측정합니다.


  Angle Measure
Measurement


    Angle double

    Line1Angle double

    Line2Angle double


| 속성명 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| Blur Size | int | 5 | 가우시안 블러 커널 크기 |
| Canny Low / High | int | 50 / 150 | Canny 에지 임계값 |
| Min Line Length | double | 50.0 | 최소 선분 길이 |


**기능 설명**

Canny 에지 검출과 HoughLinesP로 직선 세그먼트를 검출한 후, 길이 기준으로 상위 2개 선분을 선택합니다. 두 선분의 각도 차이를 계산하여 0~180도 범위의 사잇각을 산출합니다. 최소 2개 선분이 필요하며, 부족하면 에러를 발생시킵니다.


두 선분의 교차점을 계산하여 교차점에 호(arc)를 그리고, 각도 레이블을 표시합니다. 평행선의 경우 교차점이 없으므로 이미지 좌상단에 각도를 표시합니다. 결과 이미지에 Line 1(파란), Line 2(녹색), 각도 호(노란)를 표시합니다.


**응용 분야**


- 에지 간 각도 측정
- 부품 기울기 검사
- 조립 각도 검증