
# MVXTester 챗봇 (HelperBot) 구조 문서

## 개요

MVXTester의 HelperBot은 RAG(Retrieval-Augmented Generation) 기반 도움말 챗봇입니다.
사용자의 질문에 대해 내부 도움말 자료를 검색한 뒤, LLM이 해당 자료를 참고하여 답변합니다.

챗봇 코드는 **MVXTester.Chat** 프로젝트로 독립 분리되어 있어, 다른 프로젝트에 쉽게 붙일 수 있습니다.


## 프로젝트 구조

```
src/

├── MVXTester.Chat/              <- 챗봇 독립 프로젝트 (이식 가능)
│   ├── Data/
│   │   ├── chat_config.json     <- 챗봇 설정
│   │   ├── prompts.json         <- 프롬프트 템플릿
│   │   ├── node_aliases.json    <- 노드/카테고리 한국어 별칭 맵핑
│   │   ├── rag_index.json       <- 프리빌트 RAG 인덱스 (임베딩 포함)
│   │   └── data/*.md            <- 도움말/교재 원본 마크다운
│   ├── ViewModels/
│   │   ├── ChatbotViewModel.cs  <- 챗봇 UI 로직
│   │   ├── ChatMessageViewModel.cs <- 메시지 모델 (분리됨)
│   │   └── NodeDescriptions.cs  <- 노드 한국어 설명/카테고리 데이터 (분리됨)
│   ├── Views/
│   │   ├── ChatbotView.xaml     <- 챗봇 UI
│   │   ├── ChatWindow.xaml      <- 챗봇 팝업 윈도우
│   │   └── ChatSettingsDialog.xaml <- 설정 다이얼로그
│   ├── Converters/
│   │   └── Converters.cs        <- WPF 컨버터
│   ├── RagEngine.cs             <- RAG 파이프라인 핵심
│   ├── RagDocumentStore.cs      <- 문서 저장소, 하이브리드 검색
│   ├── HelpContentExtractor.cs  <- 도움말 데이터 추출/청킹
│   ├── DocumentChunk.cs         <- 문서 청크 모델
│   ├── OllamaChatService.cs     <- Ollama API 통신 (채팅/스트리밍)
│   ├── ApiChatService.cs        <- OpenAI/Claude/Gemini API 통신
│   ├── OllamaEmbeddingService.cs <- Ollama 임베딩 생성 (쿼리용)
│   ├── OllamaModelManager.cs    <- Ollama 설치/시작/모델 자동 다운로드
│   ├── NodeDirectLookup.cs      <- NodeRegistry 직접 조회 (LLM 없이 즉시 응답)
│   ├── KoreanTextNormalizer.cs  <- 한글 초성 분리 정규화
│   ├── ChatConfig.cs            <- 설정 모델
│   ├── PromptConfig.cs          <- 프롬프트 로드 (prompts.json만 사용)
│   └── build_rag_index.py       <- 임베딩 프리빌드 스크립트
│
├── MVXTester.App/               <- 메인 앱 (MVXTester.Chat 참조)
├── MVXTester.Core/              <- 핵심 라이브러리
└── MVXTester.Nodes/             <- 노드 라이브러리
```


## 사용 모델

| 역할 | 모델 | 파라미터 | 크기 | 설명 |
|------|------|---------|------|------|
| 텍스트 챗 + 비전 (통합) | qwen3.5:2b | 2B | 2.7GB | 멀티모달 (텍스트+이미지), 256K 컨텍스트, 201개 언어 |
| 쿼리 임베딩 | qwen3-embedding:0.6b | 596M | 639MB | 384차원, 32K 컨텍스트, 100+ 언어 |

- qwen3.5:2b 하나로 LLM + VLM 통합 (별도 비전 모델 불필요)
- VRAM 합계: ~3.3GB (RTX 4070 12GB에서 여유)
- think: false로 thinking 모드 비활성화
- keep_alive: 30m으로 VRAM 유지 (5분 기본값 대신)
- 앱 시작 시 미설치 모델은 자동 다운로드 (ollama pull)
- WarmupModelsAsync()로 VRAM에 미리 로드 (프롬프트 없이 model+keep_alive만 전송)


## 앱 시작 시 초기화 흐름

```
앱 시작 (ChatbotViewModel 생성)
  |
  v
[1] _setupTask = InitializeAsync()    <- 백그라운드 비동기 실행
  |
  v
[2] RagEngine.InitializeAsync()
  |
  v
[3] Ollama 프로바이더인 경우: EnsureOllamaReadyAsync()
  |
  +-- IsRunningAsync() -> GET /api/tags (3초 타임아웃)
  |     |
  |     +-- 응답 200 (이미 실행 중) -> 그대로 연결 (재시작 안 함)
  |     |
  |     +-- 응답 없음 -> IsInstalled() 확인
  |           |
  |           +-- 설치됨 -> StartAsync() -> ollama serve 실행
  |           |     OLLAMA_MAX_LOADED_MODELS=3, OLLAMA_NUM_PARALLEL=2
  |           |     RedirectStandardOutput=false (버퍼 데드락 방지)
  |           |     500ms 간격 최대 30회(15초) 폴링
  |           |
  |           +-- 미설치 -> StatusChanged("NEED_INSTALL")
  |                 -> ChatbotViewModel이 수신
  |                 -> MessageBox "Ollama 설치할까요?"
  |                 -> Yes -> InstallAsync() + StartAsync()
  |
  v
[4] EnsureModelsAsync() — 필요한 모델 자동 다운로드
  |
  +-- 설치된 모델 목록 조회 (GET /api/tags)
  +-- 미설치 모델 자동 pull:
  |     - qwen3.5:2b (텍스트+비전 통합)
  |     - qwen3-embedding:0.6b (쿼리 임베딩)
  +-- Ollama 버전 부족 시 자동 업데이트 후 재시도
  |
  v
[4b] WarmupModelsAsync() — 모델을 VRAM에 미리 로드
  |
  +-- POST /api/generate { model, keep_alive: "30m" } (프롬프트 없음)
  +-- Ollama가 모델을 VRAM에 올리고 즉시 리턴
  +-- 모델별 120초 타임아웃
  |
  v
[5] 프리빌트 RAG 인덱스 로드 (rag_index.json)
  |
  +-- 로드 성공 (625개 청크 + 임베딩)
  |     |
  |     +-- 쿼리 임베딩 서비스 생성 시도 (qwen3-embedding:0.6b)
  |     +-- 워밍업 (백그라운드, non-blocking)
  |     +-- 워밍업 완료 -> 하이브리드 검색 자동 전환
  |     +-- 워밍업 실패 -> BM25 키워드 검색만 사용
  |
  +-- 로드 실패 -> RebuildIndexAsync() (폴백)
```


## 3계층 방어 아키텍처 + 답변 흐름

```
사용자 질문 입력
      |
      v
===== Layer 1: 클라이언트 키워드/정규식 필터 =====
[1] IsBlockedQuery()
      |-- BlockedKeywords: 보안 위협 + injection 패턴 (40+ 키워드)
      |     - 보안: 해킹, 악성코드, 탈옥, DDos 등
      |     - Injection (영어): ignore previous, you are now, act as 등
      |     - Injection (한국어): 너는 이제부터, 위의 지시, 규칙을 무시 등
      |     - 시스템 정보: 프롬프트를 보여, 설정을 알려 등
      |-- InjectionPatterns: 정규식 4개
      |     - (무시|잊어|버려).{0,10}(규칙|지시|프롬프트|역할)
      |     - (너는|넌|당신은).{0,10}(이제|지금).{0,10}(부터)
      |     - ignore.{0,20}(instruction|rule|prompt|above|previous)
      |     - (reveal|show|print|output).{0,20}(system|prompt|instruction)
      |-- 차단됨 --> "MVXTester 관련 질문만 답변할 수 있습니다." (LLM 호출 없음)
      |
      v
[2] NodeDirectLookup 직접 조회 (LLM 없이 즉시 응답)
      |-- 노드 카테고리 질문 ("크롭 어느 목록?") -> NodeRegistry에서 즉시 반환
      |-- 카테고리 노드 목록 ("필터 노드 뭐 있어?") -> 해당 카테고리 전체 목록
      |-- 노드 상세 ("가우시안 블러 설명") -> 포트/속성/설명 즉시 반환
      |-- 전체 카테고리 ("카테고리 목록") -> 27개 카테고리 + 노드 수
      |-- 매칭 안 됨 -> 다음 단계로
      |
      v
[3] 초기화 대기 (_setupTask)
      |
      v
[4] Ollama 서버 확인 (매 질문마다)
      |
      v
[5] 이미지 첨부 여부 분기
      |
      +--- 텍스트만 ---+--- 이미지+텍스트 ---+
      |                |                     |
      v                |                     |
  RAG 검색             |                     |
  (벡터+키워드)         |                     |
      |                |                     |
      v                |                     |
  결과 있음?           |                     |
      |                |                     |
  +---+---+            |                     |
  |       |            |                     |
  v       v            |                     |
 있음    없음           |                     |
  |       |            |                     |
  |       v            |                     |
  |  ==== Layer 2: 도메인 관련성 체크 ====    |
  |  IsPotentiallyOnTopic()                  |
  |       |                                  |
  |   +---+---+                              |
  |   |       |                              |
  |   v       v                              |
  | On-topic Off-topic                       |
  |   |       |                              |
  |   v       v                              |
  | 폴백    off_topic_response 반환           |
  | 응답    (LLM 호출 없음)                   |
  |   |                                      |
  +---+--------------------------------------+
      |
      v
===== Layer 3: 시스템 프롬프트 가드레일 =====
  LLM 호출 시 시스템 프롬프트에:
  - 허용 도메인 양성 목록
  - Few-shot 예시 3개 (거부 + 노드 설명 + 목록 질문)
  - 용어 사전 (머신비전=Machine Vision 등 15개)
  - Injection 방어 지시
      |
      v
[5] 반복 감지 (스트리밍 중)
      |-- 3회 반복 --> 중단 + 정리
      |
      v
[6] 마크다운 제거 (StripMarkdown)
      |
      v
[7] 예제 매칭 (키워드 기반)
      |-- 매칭됨 --> "예제 열기" 버튼 표시
      |
      v
[8] 답변 표시
```


## 프롬프트 구조

모든 프롬프트는 Data/prompts.json에서 관리합니다. 코드에 하드코딩 없음.

### 프롬프트 종류

| 프롬프트 | 사용 시점 | 설명 |
|----------|-----------|------|
| system_prompt | RAG 결과 있을 때 | 도메인 가드 + few-shot 예시 + 답변 규칙 |
| image_system_prompt | 이미지 질문 | 이미지 분석 규칙 + 도메인 가드 |
| fallback_system_prompt | RAG 결과 없을 때 | 자체 지식 답변 + 도메인 가드 |
| fallback_disclaimer | 폴백 시 접두사 | 정확하지 않을 수 있다는 안내 |
| rag_context_instruction | RAG 프롬프트 끝 | 참고 자료 기반 답변 유도 |
| image_captioning_prompt | 이미지 캡셔닝 | VLM에 이미지 내용 나열 요청 |
| off_topic_response | 도메인 외 질문 | 즉시 반환 (LLM 호출 없음) + 도움말 버튼 |

### system_prompt 구조 (Sandwich Defense + Few-shot)

```
역할 정의 (1줄)
  -> 허용 도메인 양성 목록 (MVXTester, 노드, 머신비전, YOLO, OCR 등)
  -> 규칙 (한국어, 10줄 이내, 마크다운 금지, 노드명 형식)
  -> Few-shot 예시:
       질문: 김치볶음밥 만드는 법
       답변: MVXTester 관련 질문만 답변할 수 있습니다.

       질문: Canny 에지 검출은?
       답변: Canny Edge(캐니 에지) 노드를 추가하고...
  -> Injection 방어 지시 (프롬프트 끝 = recency bias 활용)
```

### RAG 프롬프트 구조 (BuildRagPrompt)

```
---
1. {chunk_text (least relevant)}
2. {chunk_text}
3. {chunk_text (most relevant)}   <- 가장 관련 높은 것이 마지막 (recency bias)
---

질문: {question}
위 참고 자료의 내용을 그대로 사용하여 답변하세요. 참고 자료에 없는 내용을 만들지 마세요.
```

- 청크 역순 정렬: Lost-in-the-Middle 대응
- `---` 구분자: 모델이 답변에 반복하지 않는 단순 형태
- 번호 부여로 토큰 절약

### 동적 청크 수 (GetOptimalChunkCount)

| 질문 길이 | 청크 수 | 이유 |
|-----------|---------|------|
| < 20자 (단순 질문) | 3개 | context 절약, Lost-in-the-Middle 방지 |
| >= 20자 (복잡 질문) | 5개 (기본값) | 충분한 참고 자료 제공 |


## 멀티턴 대화 구조

### 대화 히스토리 구성

```
[Ollama /api/chat 요청 메시지 구조]

1. system  : 시스템 프롬프트 (prompts.json)
2. user    : [이전 대화 요약] (10턴 초과 시 compacting)
3. assistant: 네, 이전 대화 내용을 참고하겠습니다.
4. user    : (최근 대화 1) 사용자 메시지
5. assistant: (최근 대화 1) 봇 응답
   ...
N. user    : [참고 자료 시작]...[참고 자료 끝] + 질문: {현재 질문}
```

### 히스토리 관리

| 항목 | 값 |
|------|-----|
| 최대 히스토리 | 10개 메시지 (chat_config.json에서 조절 가능) |
| 10턴 이하 | 전체 히스토리 그대로 전달 |
| 10턴 초과 | 오래된 메시지를 Q->A 한 줄 요약으로 압축 (compacting) |
| 압축 형식 | "- Q: {질문 80자}... -> A: {답변 80자}..." |
| 메모리 | RAM에만 존재, 앱 종료 시 삭제, ClearChat()으로 즉시 정리 |

### Compacting 예시

15턴째 대화 시:
```
[이전 대화 요약]
- Q: Canny 에지 검출 어떻게 써? -> A: Canny Edge 노드를 추가하고 입력에 이미지를 연결하세요...
- Q: 파라미터 설명해줘 -> A: Threshold1은 하한값, Threshold2는 상한값입니다...

+ 최근 10개 메시지 (원문 그대로)
```


## 이미지 질문 처리 (2단계 VLM 파이프라인)

```
사용자: 이미지 첨부 + "기능 설명 좀"
      |
      v
도메인 체크 (텍스트 30자 초과 + 도메인 키워드 없음 -> 거부)
      |
      v
이미지 리사이즈 (768px 이하로 축소)
      |
      v
[1단계] VLM 캡셔닝 (이미지 내용을 텍스트로 변환)
  - prompts.json의 image_captioning_prompt 사용
  - 캡션 후처리: EOS 토큰 제거, 중국어 제거, HTML 제거, 100자 제한
  - 예시 출력: "새로운 파일 열기 저장하기 Python 코드 C# 코드 실행 스트림 자동실행"
      |
      v
[2단계] NodeDirectLookup 직접 조회
  - 캡션에서 노드명 감지 시 -> NodeRegistry에서 즉시 응답 (LLM 호출 없음)
  - 질문 텍스트 -> 캡션+질문 순으로 매칭 시도
  - 매칭 안 됨 -> 다음 단계로
      |
      v
[3단계] 캡션 + 사용자 질문으로 RAG 검색
  - 검색 쿼리: "캡션 텍스트 + 질문 텍스트"
  - RetrieveContextAsync 공유 (텍스트 질문과 동일 파이프라인, topK=2)
      |
      v
[4단계] VLM 최종 답변
  - 이미지 + RAG 결과 + 질문 -> qwen3.5:2b 호출
  - image_system_prompt 사용
  - 단일 응답 반환 (스트리밍 아님)
  - TrimRepeatedSuffix로 문단 단위 반복 제거
```

- 이미지 입력: 파일 첨부 버튼 (PNG, JPG, BMP, TIFF) 또는 Ctrl+V 클립보드
- VLM 호출 2회 (캡셔닝 + 답변): 응답 지연은 있지만 이미지 내용 기반 정확한 RAG 검색 보장
- 캡셔닝 실패 시 사용자 질문 텍스트만으로 RAG 검색 폴백


## RAG 인덱스: 프리빌트 임베딩

### 핵심 구조

```
[빌드 타임 (개발자 PC)]                    [런타임 (사용자 PC)]

.md 파일 13개                              앱 시작
    |                                        |
    v                                        v
build_rag_index.py                      rag_index.json 로드
  - ##/###/#### 헤딩 기준 청킹                (~523개 청크 + 임베딩 즉시 사용)
  - 마크다운 장식 제거 (볼드, 테이블 등)         |
  - qwen3-embedding:0.6b로 임베딩             v
    |                                    챗봇 즉시 사용 가능 (BM25 검색)
    v                                        |
rag_index.json                               v
  - 텍스트 + 임베딩 벡터 포함            임베딩 모델 워밍업 (백그라운드)
  - 앱과 함께 배포                            |
                                             v
                                        워밍업 완료 -> 하이브리드 검색 자동 전환
                                        (RRF: 벡터 + BM25 순위 기반 퓨전)
```

### 하이브리드 검색 (RRF: Reciprocal Rank Fusion)

벡터 검색과 BM25 키워드 검색 결과를 **순위 기반(RRF)**으로 결합합니다.
기존 가중치 합산(60/40) 대신 RRF를 사용하여 스코어 스케일 불일치 문제를 해결합니다.

```
RRF_score = 0.7/(k + rank_vector) + 0.3/(k + rank_bm25)    (k=60)
```

| 검색 방식 | 가중치 | 역할 | 설명 |
|-----------|--------|------|------|
| 벡터 유사도 | 70% | 의미 기반 검색 | 코사인 유사도로 순위 산출 |
| BM25 키워드 | 30% | 용어 기반 검색 | IDF + TF 포화 + 문서 길이 정규화 (k1=1.2, b=0.75) |

- 벡터 검색 우선: 의미 기반이라 키워드만으로 구분 안 되는 질문에서 더 정확
- 양쪽 모두에서 상위에 있는 청크가 자연스럽게 최상위로 올라옴
- 임베딩 워밍업 미완료/실패 시 -> BM25 100%로 자동 폴백
- 동의어 사전 (70+쌍): 영어 <-> 한글 용어 매핑 (edge -> 에지, yolo -> 욜로 등)
- 불용어 제거, 바이그램 보너스, 정확 매칭 보너스
- 한글 초성 분리 자동 정규화 (KoreanTextNormalizer): "크로ㅂ" -> "크롭"

### 동적 청크 수

| 질문 길이 | 청크 수 | 이유 |
|-----------|---------|------|
| < 20자 (단순 질문) | 3개 | context 절약, Lost-in-the-Middle 방지 |
| >= 20자 (복잡 질문) | 5개 (기본값) | 충분한 참고 자료 제공 |
| 이미지 질문 | 3개 | VLM 모델이 RAG 과다 시 혼란, 이미지 분석에 집중 |

### 청킹 전략

| 항목 | 값 |
|------|-----|
| 최대 청크 | 1500자 (초과 시 강제 분할) |
| 오버랩 | 200자 (직전 문단 1개) |
| 분할 기준 | 마크다운 헤딩(##, ###, ####) 단위 |
| 최소 청크 | 50자 미만 건너뜀 |
| 헤딩 유지 | 2번째 이후 청크에 "(계속)" 접두사 |
| 마크다운 제거 | 볼드(**), 테이블 구분선, 코드블록 → 인덱스 단계에서 제거 |
| 요약 청크 | 각 섹션 상단에 한 줄 요약 포함 (툴바, 노드팔레트, 트러블슈팅 등) |


## RAG 인덱스 데이터 소스

| 소스 | 위치 | 내용 |
|------|------|------|
| 도움말/교재 .md 파일 | Data/data/*.md | 교재, 사용법, 도구 기능, 트러블슈팅 등 |
| 노드 설명 (Korean) | 코드 내 Dictionary | 각 노드의 한글 설명 + 응용 분야 |
| 노드 레지스트리 | NodeRegistry (런타임) | 포트, 속성 등 상세 메타데이터 |
| 예제 프로젝트 목록 | examples/*.mvxp | 예제 파일 리스트 |

### data/ 폴더 파일 (13개)

| 파일명 | 내용 |
|--------|------|
| help_usage.md | 기본 사용법, dirty 상태, 실행 모드, 툴바 도구 기능 |
| help_setup-guide.md | 설치 가이드 |
| help_tech-stack.md | 기술 스택 설명 |
| help_program-structure.md | 프로그램 구조 + 전체 노드 목록 |
| help_troubleshooting.md | 오류 해결 가이드 |
| help_tutorials.md | 30개 튜토리얼 |
| textbook_ch08-09_*.md | 교재 8~9장: 입출력/값 |
| textbook_ch10-11_*.md | 교재 10~11장: 색상/필터 |
| textbook_ch12-13_*.md | 교재 12~13장: 에지/모폴로지 |
| textbook_ch14-21_*.md | 교재 14~21장: 윤곽/특징/검출/그리기/변환/히스토그램/산술 |
| textbook_ch22-28_*.md | 교재 22~28장: 제어/데이터/통신/이벤트/스크립트/검사/측정 |
| textbook_ch29-32_*.md | 교재 29~32장: MediaPipe/YOLO/OCR/LLM |
| textbook_ch33-38_*.md | 교재 33~38장: 튜토리얼/산업응용/AI |


## Ollama 실행 구조

### 앱이 Ollama를 관리하는 범위

| 항목 | 동작 |
|------|------|
| 설치 | 미설치 시 사용자에게 확인 -> OllamaSetup.exe 사일런트 설치 |
| 시작 | 이미 실행 중 -> 그대로 연결 (재시작 안 함). 미실행 -> StartAsync()로 시작 |
| 모델 다운로드 | 미설치 모델 자동 pull (qwen3.5:2b, qwen3-embedding:0.6b) |
| VRAM 워밍업 | WarmupModelsAsync()로 모델을 VRAM에 미리 로드 |
| 자동 업데이트 | 모델 pull 시 412/버전 부족 -> 최신 Ollama로 자동 업데이트 |
| 종료 | 앱이 시작한 프로세스만 종료 (_processOwned 플래그) |

### API 통신 방식

**텍스트 질문 (스트리밍)**
```
POST http://localhost:11434/api/chat
{
  "model": "qwen3.5:2b",
  "messages": [
    {"role": "system", "content": "시스템 프롬프트 (도메인 가드 + few-shot)"},
    {"role": "user", "content": "과거 질문"},
    {"role": "assistant", "content": "과거 답변"},
    {"role": "user", "content": "---\n1. 청크1\n2. 청크2\n---\n\n질문: 현재 질문"}
  ],
  "stream": true,
  "think": false,
  "keep_alive": "30m",
  "options": {
    "temperature": 0.3,
    "num_predict": 2048,
    "top_k": 20,
    "repeat_penalty": 1.5
  }
}
```

**이미지 질문 (단일 응답)**
```
POST http://localhost:11434/api/chat
{
  "model": "qwen3.5:2b",
  "messages": [
    {"role": "system", "content": "이미지 분석 프롬프트"},
    {"role": "user", "content": "질문", "images": ["base64..."]}
  ],
  "stream": false,
  "think": false,
  "keep_alive": "30m",
  "options": {"temperature": 0.3, "num_predict": 2048, "top_k": 20, "repeat_penalty": 1.3}
}
```

**쿼리 임베딩 (벡터 검색용)**
```
POST http://localhost:11434/api/embed
{
  "model": "qwen3-embedding:0.6b",
  "input": "사용자 질문 텍스트",
  "keep_alive": "30m"
}
```

**VRAM 워밍업 (모델 프리로드)**
```
POST http://localhost:11434/api/generate
{
  "model": "qwen3.5:2b",
  "keep_alive": "30m"
}
-> 프롬프트 없이 model+keep_alive만 전송, 모델 로드 후 즉시 리턴
```


## 설정 파일

### Data/chat_config.json

| 설정 | 설명 | 기본값 |
|------|------|--------|
| provider | 프로바이더 | ollama |
| ollama_base_url | Ollama 서버 주소 | http://localhost:11434 |
| ollama_chat_model | 텍스트+비전 통합 모델 | qwen3.5:2b |
| ollama_vision_model | 비전 모델 (챗 모델과 동일) | qwen3.5:2b |
| ollama_embed_model | 임베딩 모델 | qwen3-embedding:0.6b |
| embedding_backend | 임베딩 백엔드 | ollama |
| api_provider | API 프로바이더 | openai |
| api_key | API 키 | (빈 값) |
| api_model | API 모델명 | (빈 값) |
| temperature | 응답 창의성 (0.0~1.0) | 0.3 |
| max_tokens | 최대 응답 토큰 수 | 2048 |
| max_context_chunks | RAG 검색 최대 청크 수 | 5 |
| repeat_penalty | 반복 방지 패널티 | 1.5 |
| max_history_messages | 최대 히스토리 메시지 수 | 10 |


## 도메인 키워드 양성 목록 (IsPotentiallyOnTopic)

RAG 검색 결과 0개 + 이 키워드 없음 -> off_topic_response 즉시 반환 (LLM 호출 없음)

- MVXTester: mvx, 노드, 파이프라인, 포트, 연결, 실행, 프로젝트
- 머신비전: 비전, 영상, 이미지, 사진, 화면, 픽셀, 카메라, 캡처, 프레임
- 이미지 처리: 필터, 블러, 에지, canny, threshold, 이진화, 모폴로지, 컨투어, 히스토그램, 그레이, hsv, rgb, roi 등
- AI/검출: yolo, ocr, mediapipe, 얼굴, 포즈, 객체검출, 매칭, 세그멘테이션
- 통신: 시리얼, tcp, 소켓, rs232, modbus
- 프로그래밍: 파이썬, python, 스크립트, c#
- 일반: opencv, 검사, 측정, 불량, 설치, 설정, 오류, 에러


## 예제 열기 기능

### 동작
1. 챗봇 답변에 관련 예제가 있으면 "예제 열기" 버튼 표시
2. 클릭 시 examples/{name}.mvxp 파일 로드
3. 로드 후 Image Read 노드의 파일 경로가 비어있으면 MessageBox로 안내

### 예제 매칭
- 질문 키워드 -> 예제 프로젝트 매핑 (31개)
- 예: "에지 검출" -> 01_EdgePartInspection
- 오류/트러블슈팅 질문에는 예제 추천 안 함


## NodeDirectLookup (NodeRegistry 직접 조회)

특정 패턴 질문은 LLM 없이 NodeRegistry에서 직접 응답합니다 (hallucination 제로, <10ms).

### 매칭 패턴

| 패턴 | 예시 질문 | 응답 |
|------|-----------|------|
| **전체 주제: 툴바** | "툴바 설명", "메뉴 기능", "버튼 뭐야" | 13개 버튼 요약 직접 반환 |
| **전체 주제: 팔레트** | "노드 팔레트 설명", "palette 목록" | 27개 카테고리 목록 |
| **전체 주제: 튜토리얼** | "튜토리얼 목록", "학습 예제" | 18개 튜토리얼 목록 |
| **전체 주제: 단축키** | "단축키", "키보드 shortcut" | 15개 단축키 목록 |
| 전체 카테고리 | "전체 카테고리 목록" | 27개 카테고리 + 노드 수 |
| 카테고리 노드 목록 | "필터 노드 뭐 있어?" | Filter 카테고리 전체 노드 목록 |
| 노드 카테고리 조회 | "크롭 어느 카테고리?" | "Crop(크롭) 노드는 [변환(Transform)] 카테고리에 있습니다." |
| 노드 상세 | "가우시안 블러 설명" | 포트 + 파라미터 + 설명 |
| 짧은 노드명 | "크롭" (25자 미만) | 해당 노드 상세 정보 |

### 별칭 맵핑 (node_aliases.json)

JSON 파일로 관리 — 재컴파일 없이 수정 가능:
- node_aliases: 한국어 노드명 -> 영문 노드명 (43개)
- category_aliases: 한국어 카테고리명 -> 영문 카테고리 (36개)

### 안전장치
- 에러/트러블슈팅 키워드 포함 시 직접 조회 건너뜀 (RAG+LLM이 더 적합)
- 매칭 안 되면 기존 RAG+LLM 경로 그대로
- 텍스트 + 이미지 질문 모두 적용

### 디버그 로그
```
[HH:mm:ss] [DIRECT] lookup=있음, question="크롭 어디에 있어?"
[HH:mm:ss] [DIRECT] HIT: "크롭 어디에 있어?" -> Crop(크롭) 노드는 [변환(Transform)]...
```
또는:
```
[HH:mm:ss] [DIRECT] MISS: "노드 팔레트 설명좀" -> RAG 경로로 전환
```


## 한글 초성 정규화 (KoreanTextNormalizer)

초성 분리된 오타 입력을 정상 음절로 재조합합니다.

| 입력 | 정규화 결과 |
|------|------------|
| 크로ㅂㄴㅗㄷㅡ | 크롭노드 |
| 프로그래ㅁㅇㅣㅇㅑ | 프로그램이야 |
| 파이ㅆㄴ | 파이썬 |

적용 위치:
- IsPotentiallyOnTopic: 도메인 키워드 매칭
- FindExampleByKeyword: 예제 버튼 매칭
- ComputeKeywordScores: BM25 키워드 검색
- IsBlockedQuery: injection 방어


## 답변 후처리 (StripMarkdown)

모델 출력에서 자동 정리하는 항목:
- 리터럴 `\n` -> 실제 줄바꿈
- 프롬프트 구분자 (`---`, `[참고 자료 시작/끝]`) 제거
- 중국어 문자 (CJK \u4E00-\u9FFF) 자동 제거 (Qwen 모델 중국어 혼입 방지)
- 한국어 외래어 오표기 교정 (TermCorrections): "마이치언 비전"->"머신비전", "크로프"->"크롭" 등 20+개
- "스크린샷 -" 접두사 제거 (VLM 이미지 응답)
- 숫자 이모지 (1⃣, 2⃣ 등) 제거
- [대괄호] 링크 -> 내용만 유지
- 마크다운 헤딩(##), 볼드(**), 코드블록(```), 이모지 제거
- TrimRepeatedSuffix: 문단 단위 반복 제거 (동일 문단 첫 번째만 유지)
- 텍스트 + 이미지 답변 모두 적용


## 디버그 로그 (rag_debug.log)

Models/Chat/rag_debug.log에 실시간 기록:

**텍스트 질문:**
```
[HH:mm:ss] Query: "질문 텍스트"
  Vector: Yes/No, Candidates: N/625
  #1 [0.032] (source) 청크 미리보기...
  #2 [0.031] (source) 청크 미리보기...
[HH:mm:ss] TIMING: 검색=Nms, 첫토큰=Nms, 총응답=Nms
```

**이미지 질문:**
```
[HH:mm:ss] [IMAGE] 질문: "질문 텍스트"
[HH:mm:ss] [IMAGE] 캡셔닝 (Nms): "캡션 내용"
[HH:mm:ss] [IMAGE] RAG 검색 쿼리: "캡션 + 질문"
[HH:mm:ss] [IMAGE] RAG 결과: N개 청크
[HH:mm:ss] [IMAGE]   - (source) 청크 미리보기...
[HH:mm:ss] [IMAGE] 최종 프롬프트 길이: N자
[HH:mm:ss] [IMAGE] 답변 완료 (Nms, N자)
```

**직접 조회 (NodeDirectLookup):**
```
[HH:mm:ss] [DIRECT] lookup=있음, question="크롭 어디에 있어?"
[HH:mm:ss] [DIRECT] HIT: "크롭 어디에 있어?" -> 직접 응답
또는:
[HH:mm:ss] [DIRECT] MISS: "노드 팔레트 설명좀" -> RAG 경로로 전환
```

**도메인 외 차단:**
```
[HH:mm:ss] [OFF_TOPIC] "질문" 차단 (Nms)
```


## 클라우드 API 모델 (설정에서 선택)

| 프로바이더 | 모델 | ID |
|-----------|------|-----|
| OpenAI | GPT-5-mini (빠름) | gpt-5-mini |
| | GPT-5.4 (최신) | gpt-5.4 |
| | GPT-5.4 Pro (최고) | gpt-5.4-pro |
| Claude | Haiku 4.5 (빠름) | claude-haiku-4-5 |
| | Sonnet 4.6 (균형) | claude-sonnet-4-6 |
| | Opus 4.6 (최고) | claude-opus-4-6 |
| Gemini | 2.5 Flash (빠름) | gemini-2.5-flash |
| | 3.1 Flash-Lite (경량) | gemini-3.1-flash-lite-preview |
| | 3.1 Pro (최고) | gemini-3.1-pro-preview |

- 프로바이더 전환 시 API 키 자동 초기화
- Gemini: x-goog-api-key 헤더 인증 (URL 쿼리 파라미터가 아님, 보안)
- Anthropic: anthropic-version 헤더 (상수화)
- 설정 UI 고급 설정: 임베딩 재구축 버튼만 표시 (Temperature, Max Tokens 등은 chat_config.json에서 관리)


## 후속 질문 맥락 보강 (EnrichQueryWithHistory)

15자 미만 짧은 후속 질문은 이전 대화의 마지막 user 메시지를 RAG 검색 쿼리에 결합합니다.

예: "아니 씨샾" (15자 미만) + 이전 "시샾 스크립트 예제 알려줘" -> "시샾 스크립트 예제 알려줘 아니 씨샾"으로 검색


## 에러 처리

| 에러 상황 | 표시 메시지 |
|-----------|------------|
| Ollama 미설치/연결 불가 | Ollama 서버에 연결할 수 없습니다. + 설치 안내 |
| 모델 미설치 | 모델이 설치되어 있지 않습니다. + 설치 명령 안내 |
| API 인증 실패 | API 인증에 실패했습니다. + 키 확인 안내 |
| 보안/injection 차단 | MVXTester 관련 질문만 답변할 수 있습니다. |
| 도메인 외 질문 | MVXTester 관련 질문만 답변할 수 있습니다. (off_topic_response) |
| 초기화 실패 | 챗봇 초기화에 실패했습니다. + 재시작 안내 |
