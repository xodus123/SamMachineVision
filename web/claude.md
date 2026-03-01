# MVXTester Web — 구현 가이드

> 이 문서는 C#/WPF 기반 MVXTester 데스크톱 앱을 Web으로 포팅하기 위한 전체 아키텍처 및 구현 명세이다.
> 데스크톱 버전과 동일한 기능과 성능을 목표로 한다.

---

## 1. 프로젝트 개요

MVXTester는 **노드 기반 비주얼 프로그래밍 환경**으로, OpenCV 기반 컴퓨터 비전 파이프라인을 시각적으로 구성·실행한다. 120+ 노드, 실시간 스트리밍, 함수 노드(서브그래프), 루프 제어 흐름, Undo/Redo, Python/C# 코드 생성 등을 지원한다.

### 1.1 데스크톱 기술 스택
- **Core**: .NET 8, C#
- **Vision**: OpenCvSharp (OpenCV .NET wrapper)
- **UI**: WPF + Nodify (노드 에디터 라이브러리)
- **MVVM**: CommunityToolkit.Mvvm
- **직렬화**: System.Text.Json + ZIP 아카이브 (.mvxp)

### 1.2 Web 기술 스택 (권장)
- **Frontend**: React 18+ / TypeScript
- **노드 에디터**: React Flow (https://reactflow.dev)
- **Vision 엔진**: OpenCV.js (WebAssembly) — 브라우저 내 실행
- **카메라**: WebRTC (`getUserMedia`) — USB/IP 카메라 지원
- **상태 관리**: Zustand 또는 Redux Toolkit
- **직렬화**: JSON (데스크톱 .mvx 포맷과 호환)
- **빌드**: Vite + TypeScript
- **UI 프레임워크**: Tailwind CSS + shadcn/ui

---

## 2. 핵심 아키텍처

### 2.1 계층 구조 (3-Layer)

```
┌──────────────────────────────────────────────┐
│  UI Layer (React + React Flow)               │
│  - NodeCanvas, PropertyEditor, NodePalette   │
│  - Preview panels, toolbar, status bar       │
├──────────────────────────────────────────────┤
│  ViewModel/Store Layer (Zustand)             │
│  - GraphStore, ExecutionStore, UndoManager   │
│  - Selection, dirty tracking, auto-execute   │
├──────────────────────────────────────────────┤
│  Core Layer (TypeScript)                     │
│  - BaseNode, NodeGraph, GraphExecutor        │
│  - Ports, Connections, NodeProperty          │
│  - GraphSerializer, NodeRegistry             │
└──────────────────────────────────────────────┘
```

### 2.2 디렉터리 구조

```
web/
├── src/
│   ├── core/                    # 코어 엔진 (UI 무관)
│   │   ├── models/
│   │   │   ├── BaseNode.ts      # 추상 노드 기반 클래스
│   │   │   ├── NodeGraph.ts     # 그래프 컨테이너
│   │   │   ├── Port.ts          # InputPort, OutputPort
│   │   │   ├── Connection.ts    # 연결
│   │   │   ├── NodeProperty.ts  # 속성 시스템
│   │   │   └── FunctionNode.ts  # 서브그래프 함수 노드
│   │   ├── engine/
│   │   │   ├── GraphExecutor.ts # 실행 엔진 (토폴로지 정렬, 루프)
│   │   │   └── LoopExecutor.ts  # For/While/ForEach 루프 처리
│   │   ├── serialization/
│   │   │   ├── GraphSerializer.ts
│   │   │   └── SubGraphAnalyzer.ts
│   │   └── registry/
│   │       ├── NodeRegistry.ts
│   │       └── NodeCategories.ts
│   ├── nodes/                   # 노드 구현 (카테고리별)
│   │   ├── input/
│   │   ├── filter/
│   │   ├── edge/
│   │   ├── threshold/
│   │   ├── contour/
│   │   ├── arithmetic/
│   │   ├── color/
│   │   ├── morphology/
│   │   ├── transform/
│   │   ├── detection/
│   │   ├── drawing/
│   │   ├── histogram/
│   │   ├── feature/
│   │   ├── segmentation/
│   │   ├── value/
│   │   ├── control/
│   │   ├── data/
│   │   ├── inspection/
│   │   ├── measurement/
│   │   └── script/
│   ├── store/                   # 상태 관리 (Zustand)
│   │   ├── graphStore.ts
│   │   ├── executionStore.ts
│   │   └── undoStore.ts
│   ├── components/              # React 컴포넌트
│   │   ├── canvas/
│   │   │   ├── NodeCanvas.tsx   # React Flow wrapper
│   │   │   ├── CustomNode.tsx   # 노드 렌더링
│   │   │   └── CustomEdge.tsx   # 연결선 렌더링
│   │   ├── panels/
│   │   │   ├── NodePalette.tsx  # 노드 카탈로그 (좌측 사이드바)
│   │   │   ├── PropertyEditor.tsx # 속성 편집기 (우측)
│   │   │   └── PreviewPanel.tsx # 이미지 미리보기
│   │   ├── toolbar/
│   │   │   └── Toolbar.tsx
│   │   └── dialogs/
│   │       └── FunctionDetailDialog.tsx
│   ├── utils/
│   │   ├── opencv.ts            # OpenCV.js 로더 및 유틸리티
│   │   └── imageUtils.ts        # Mat <-> Canvas 변환
│   ├── App.tsx
│   └── main.tsx
├── public/
│   └── opencv.js                # OpenCV.js WASM 빌드
├── package.json
├── tsconfig.json
├── vite.config.ts
└── claude.md                    # 이 파일
```

---

## 3. 코어 모델 상세

### 3.1 BaseNode

```typescript
abstract class BaseNode {
  id: string;            // 8자 hex GUID
  name: string;          // NodeInfo에서
  category: string;      // NodeInfo에서
  description: string;

  inputs: InputPort[];
  outputs: OutputPort[];
  properties: NodeProperty[];

  isDirty: boolean;
  error: string | null;
  previewImageData: ImageData | null;  // canvas 호환 미리보기

  abstract setup(): void;     // 포트/속성 정의
  abstract process(): void;   // 메인 처리 로직
  cleanup(): void {}          // 리소스 해제

  // 포트 생성 헬퍼
  protected addInput<T>(name: string): InputPort<T>;
  protected addOutput<T>(name: string): OutputPort<T>;

  // 속성 생성 헬퍼
  protected addIntProperty(name, displayName, defaultValue, min, max, description): NodeProperty;
  protected addDoubleProperty(...): NodeProperty;
  protected addBoolProperty(...): NodeProperty;
  protected addStringProperty(...): NodeProperty;
  protected addEnumProperty(name, displayName, enumDef, defaultValue, description): NodeProperty;
  protected addFilePathProperty(...): NodeProperty;
  protected addMultilineStringProperty(...): NodeProperty;
  protected addDeviceListProperty(...): NodeProperty;

  // 데이터 접근 헬퍼
  protected getInputValue<T>(port: InputPort<T>): T | null;
  protected setOutputValue<T>(port: OutputPort<T>, value: T): void;
  protected setPreview(mat: cv.Mat): void;  // Mat -> ImageData 변환 후 저장
}
```

### 3.2 Port 시스템

```typescript
interface IPort {
  name: string;
  dataType: string;   // "Mat", "number", "boolean", "string", "Point[]", etc.
  owner: BaseNode;
}

class InputPort<T> implements IPort {
  connection: Connection | null;  // 단일 연결 (fan-in = 1)
  getValue(): T | null;           // connection?.source.getValue()
}

class OutputPort<T> implements IPort {
  connections: Connection[];      // 다중 연결 (fan-out = N)
  value: T | null;
  getValue(): T | null;
  setValue(v: T): void;
}
```

**데이터 흐름**: Pull 기반. InputPort.getValue()는 연결된 OutputPort의 값을 참조한다.

**타입 호환성**: 연결 시 양방향 `isAssignableFrom` 체크. 주요 타입:
- `Mat` — OpenCV Mat (이미지)
- `number` — int/float/double 통합
- `boolean`
- `string`
- `Point[]`, `Rect[]`, `Point[][]` (contours)
- `number[]` (double[])
- `object` — any (제네릭 패스스루)
- `List<object>` — 루프 수집용

### 3.3 Connection

```typescript
class Connection {
  source: OutputPort;
  target: InputPort;

  static canConnect(source: OutputPort, target: InputPort): boolean;
  static tryConnect(source: OutputPort, target: InputPort): Connection | null;
  static disconnect(conn: Connection): void;
}
```

**규칙:**
- InputPort는 최대 1개 연결 (기존 연결 자동 제거)
- OutputPort는 N개 연결 가능
- 같은 노드 간 연결 불가
- 사이클 감지 (BFS)
- 타입 호환성 체크

### 3.4 NodeProperty

```typescript
enum PropertyType {
  Integer, Float, Double, Boolean, String, Enum,
  Point, Size, Scalar, FilePath, Range, MultilineString, DeviceList
}

class NodeProperty {
  name: string;
  displayName: string;
  propertyType: PropertyType;
  value: any;
  defaultValue: any;
  minValue?: number;
  maxValue?: number;
  description?: string;
  enumOptions?: { label: string; value: any }[];  // Enum용
  deviceOptions?: { name: string; index: number }[];  // DeviceList용

  getValue<T>(): T;
  setValue(v: any): void;  // onChange 이벤트 발생

  onChange: ((newValue: any) => void) | null;
}
```

### 3.5 NodeGraph

```typescript
class NodeGraph {
  nodes: BaseNode[];
  connections: Connection[];

  addNode(node: BaseNode): void;
  removeNode(node: BaseNode): void;  // 관련 연결도 제거
  connect(source: OutputPort, target: InputPort): Connection | null;
  removeConnection(conn: Connection): void;
  markDirtyDownstream(node: BaseNode): void;  // BFS 전파
  wouldCreateCycle(from: BaseNode, to: BaseNode): boolean;
  clear(): void;
  snapshot(): { nodes: BaseNode[]; connections: Connection[] };

  // 이벤트
  onNodeAdded?: (node: BaseNode) => void;
  onNodeRemoved?: (node: BaseNode) => void;
  onConnectionAdded?: (conn: Connection) => void;
  onConnectionRemoved?: (conn: Connection) => void;
}
```

---

## 4. 실행 엔진

### 4.1 GraphExecutor

```typescript
class GraphExecutor {
  lastExecutionTime: number;  // ms

  // 단발 실행
  execute(graph: NodeGraph, forceAll?: boolean): void;

  // 연속 스트리밍 (카메라용)
  executeContinuous(graph: NodeGraph, onFrame?: () => void, targetFps?: number): void;

  // 런타임 모드 (이벤트 구동 — dirty 노드만 재실행)
  executeRuntime(graph: NodeGraph, onFrame?: () => void): void;

  cancel(): void;

  // 토폴로지 정렬 (Kahn's algorithm)
  static topologicalSort(nodes: BaseNode[], connections: Connection[]): BaseNode[];
}
```

### 4.2 실행 흐름

1. `snapshot()` — 그래프의 스냅샷 복사
2. `topologicalSort()` — 의존성 기반 실행 순서 결정
3. 순서대로 각 노드 실행:
   - `forceAll || node.isDirty` 일 때만 실행
   - `ILoopNode` 이면 루프 실행기 진입
   - `node.process()` 호출
   - 예외 발생 시 `node.error = ex.message`
   - `isDirty = false` 설정
4. 스트리밍 모드: `IStreamingSource` 노드를 매 프레임 dirty로 표시

### 4.3 루프 실행

**인터페이스:**
```typescript
interface ILoopNode {
  initializeLoop(): void;
  moveNext(): boolean;
  endLoop(): void;
  maxIterations: number;
}

interface ILoopCollector {
  clearCollection(): void;
  collectIteration(): void;
  finalizeCollection(): void;
}

interface IBreakSignal {
  shouldBreak: boolean;
  resetBreak(): void;
}
```

**루프 실행 알고리즘:**
1. `ILoopNode` 발견 시 → BFS로 downstream body 노드 수집 (`ILoopCollector`에서 중단)
2. `initializeLoop()` 호출
3. `while (moveNext() && iteration < maxIterations)`:
   a. body 노드들 순서대로 실행
   b. `ILoopCollector.collectIteration()` 호출
   c. `IBreakSignal.shouldBreak` 확인 → true면 break
4. `endLoop()`, `finalizeCollection()` 호출
5. body 노드들을 "처리됨"으로 표시 → 외부 순회에서 건너뜀
6. 중첩 루프 지원 (재귀)

---

## 5. 노드 카테고리 및 전체 목록 (120+ 노드)

### 5.1 카테고리

| 카테고리 | 설명 | 노드 수 |
|----------|------|---------|
| Input/Output | 이미지/비디오 읽기쓰기, 카메라 | 7 |
| Color | 색공간 변환, 채널 분리/병합 | 4 |
| Filter | 블러, 샤프닝, 노이즈 제거 | 10 |
| Edge Detection | Canny, Sobel, Laplacian, Scharr | 4 |
| Morphology | 침식, 팽창, 열림/닫힘 | 3 |
| Threshold | 고정/적응/Otsu 이진화 | 3 |
| Contour | 윤곽선 검출, 분석, 근사 | 13 |
| Feature Detection | SIFT, ORB, Harris, FAST, Blob | 8 |
| Drawing | 도형, 텍스트, 격자 그리기 | 10 |
| Transform | 크롭, 리사이즈, 회전, 원근변환 | 8 |
| Histogram | 히스토그램, CLAHE, 균일화 | 4 |
| Arithmetic | 이미지 산술, 논리 연산 | 11 |
| Detection | 템플릿매칭, 허프변환, 연결성분 | 9 |
| Segmentation | Watershed, GrabCut, FloodFill | 3 |
| Value | 상수값, 수학연산, 비교, 리스트 | 14 |
| Control | For/While/ForEach, Switch, Boolean | 11 |
| Data Processing | CSV, 문자열 처리, 변환 | 7 |
| Communication | TCP Client/Server, Serial Port | 3 |
| Event | 키보드, 마우스 이벤트 | 3 |
| Script | Python 스크립트 실행 | 1 |
| Inspection | 고수준 검사 (결함, 패턴, 정렬) | 13 |
| Measurement | 거리, 각도, 객체 치수 측정 | 3 |
| Function | 서브그래프 함수 노드 | 1 |

### 5.2 Web에서의 노드 구현 전략

**직접 포팅 가능 (OpenCV.js 지원):**
- Filter, Edge, Morphology, Threshold, Contour, Color, Transform, Histogram, Arithmetic, Detection, Segmentation, Drawing, Feature — OpenCV.js가 대부분 지원

**수정 필요:**
- **Camera 노드**: `getUserMedia` API 사용 (USB/IP 카메라는 WebRTC로 제한적)
- **Communication 노드**: WebSocket으로 대체 (브라우저에서 TCP/Serial 직접 접근 불가)
- **Event 노드**: 브라우저 이벤트 API 사용
- **Script 노드**: Pyodide (브라우저 Python) 또는 서버 사이드 실행
- **HIK/Cognex Camera**: 불가 → WebRTC 또는 서버 프록시

**Web 전용 추가:**
- WebSocket 통신 노드
- REST API 호출 노드
- 브라우저 파일 업로드/다운로드 노드

### 5.3 주요 노드 구현 예시

**GaussianBlurNode (TypeScript + OpenCV.js):**
```typescript
@nodeInfo("Gaussian Blur", "Filter", "Gaussian blur filter")
class GaussianBlurNode extends BaseNode {
  private imageInput!: InputPort<cv.Mat>;
  private resultOutput!: OutputPort<cv.Mat>;
  private kernelW!: NodeProperty;
  private kernelH!: NodeProperty;
  private sigmaX!: NodeProperty;

  setup() {
    this.imageInput = this.addInput<cv.Mat>("Image");
    this.resultOutput = this.addOutput<cv.Mat>("Result");
    this.kernelW = this.addIntProperty("KernelW", "Kernel Width", 5, 1, 51, "Must be odd");
    this.kernelH = this.addIntProperty("KernelH", "Kernel Height", 5, 1, 51, "Must be odd");
    this.sigmaX = this.addDoubleProperty("SigmaX", "Sigma X", 0.0, 0.0, 100.0, "Gaussian sigma");
  }

  process() {
    const image = this.getInputValue(this.imageInput);
    if (!image) { this.error = "No input image"; return; }

    let kw = this.kernelW.getValue<number>() | 1;  // ensure odd
    let kh = this.kernelH.getValue<number>() | 1;
    const sigma = this.sigmaX.getValue<number>();

    const result = new cv.Mat();
    cv.GaussianBlur(image, result, new cv.Size(kw, kh), sigma);

    this.setOutputValue(this.resultOutput, result);
    this.setPreview(result);
  }
}
```

---

## 6. OpenCV.js 통합

### 6.1 로딩

```typescript
// utils/opencv.ts
let cvReady = false;

export async function loadOpenCV(): Promise<void> {
  return new Promise((resolve) => {
    const script = document.createElement('script');
    script.src = '/opencv.js';
    script.onload = () => {
      (window as any).cv.onRuntimeInitialized = () => {
        cvReady = true;
        resolve();
      };
    };
    document.head.appendChild(script);
  });
}

export function getCv(): typeof cv {
  if (!cvReady) throw new Error("OpenCV.js not loaded");
  return (window as any).cv;
}
```

### 6.2 Mat ↔ Canvas 변환

```typescript
// Mat -> ImageData (미리보기용)
export function matToImageData(mat: cv.Mat): ImageData {
  const rgba = new cv.Mat();
  if (mat.channels() === 1) {
    cv.cvtColor(mat, rgba, cv.COLOR_GRAY2RGBA);
  } else if (mat.channels() === 3) {
    cv.cvtColor(mat, rgba, cv.COLOR_BGR2RGBA);
  } else {
    mat.copyTo(rgba);
  }
  const data = new ImageData(
    new Uint8ClampedArray(rgba.data),
    rgba.cols, rgba.rows
  );
  rgba.delete();
  return data;
}

// Canvas/ImageData -> Mat
export function imageDataToMat(imageData: ImageData): cv.Mat {
  const mat = cv.matFromImageData(imageData);
  const bgr = new cv.Mat();
  cv.cvtColor(mat, bgr, cv.COLOR_RGBA2BGR);
  mat.delete();
  return bgr;
}
```

### 6.3 메모리 관리

OpenCV.js는 WASM 힙을 사용하므로 **반드시 수동 메모리 해제** 필요:

```typescript
// BaseNode.setOutputValue에서 이전 Mat dispose
setOutputValue<T>(port: OutputPort<T>, value: T): void {
  const old = port.value;
  if (old && old instanceof cv.Mat) {
    old.delete();  // WASM 메모리 해제
  }
  port.setValue(value);
}

// cleanup()에서 모든 출력 Mat 해제
cleanup(): void {
  for (const output of this.outputs) {
    if (output.value instanceof cv.Mat) {
      output.value.delete();
      output.value = null;
    }
  }
}
```

---

## 7. UI 컴포넌트

### 7.1 React Flow 노드 에디터

```tsx
// components/canvas/NodeCanvas.tsx
function NodeCanvas() {
  const { nodes, edges, onConnect, onNodesChange, onEdgesChange } = useGraphStore();

  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      nodeTypes={{ custom: CustomNode }}
      onConnect={onConnect}
      onNodesChange={onNodesChange}
      onEdgesChange={onEdgesChange}
      fitView
    >
      <Background />
      <Controls />
      <MiniMap />
    </ReactFlow>
  );
}
```

### 7.2 커스텀 노드 렌더링

데스크톱 앱의 Nodify 노드 스타일 재현:

```tsx
// components/canvas/CustomNode.tsx
function CustomNode({ data }: NodeProps) {
  const { node } = data as { node: BaseNode };

  return (
    <div className={`node ${node.error ? 'node-error' : ''}`}>
      {/* 헤더 */}
      <div className="node-header" style={{ background: getCategoryColor(node.category) }}>
        <span>{node.name}</span>
        {node.error && <span className="error-icon">⚠</span>}
      </div>

      {/* 포트 */}
      <div className="node-body">
        <div className="input-ports">
          {node.inputs.map(port => (
            <div key={port.name} className="port input-port">
              <Handle type="target" position={Position.Left} id={port.name} />
              <span>{port.name}</span>
            </div>
          ))}
        </div>
        <div className="output-ports">
          {node.outputs.map(port => (
            <div key={port.name} className="port output-port">
              <span>{port.name}</span>
              <Handle type="source" position={Position.Right} id={port.name} />
            </div>
          ))}
        </div>
      </div>

      {/* 미리보기 */}
      {node.previewImageData && (
        <div className="node-preview">
          <canvas ref={el => drawPreview(el, node.previewImageData)} />
        </div>
      )}
    </div>
  );
}
```

### 7.3 속성 편집기

노드 선택 시 우측 패널에 속성 표시:

```tsx
function PropertyEditor() {
  const selectedNode = useGraphStore(s => s.selectedNode);
  if (!selectedNode) return <div>Select a node</div>;

  return (
    <div className="property-editor">
      <h3>{selectedNode.name}</h3>
      {selectedNode.error && <div className="error">{selectedNode.error}</div>}

      {selectedNode.properties.map(prop => (
        <PropertyField key={prop.name} property={prop} />
      ))}
    </div>
  );
}

function PropertyField({ property }: { property: NodeProperty }) {
  switch (property.propertyType) {
    case PropertyType.Integer:
    case PropertyType.Double:
      return <NumberInput property={property} />;
    case PropertyType.Boolean:
      return <BooleanToggle property={property} />;
    case PropertyType.String:
      return <TextInput property={property} />;
    case PropertyType.Enum:
      return <EnumSelect property={property} />;
    case PropertyType.FilePath:
      return <FilePickerInput property={property} />;
    case PropertyType.MultilineString:
      return <TextareaInput property={property} />;
    case PropertyType.DeviceList:
      return <DeviceSelect property={property} />;
    // ...
  }
}
```

### 7.4 노드 팔레트

카테고리별 노드 목록, 검색 및 드래그앤드롭:

```tsx
function NodePalette() {
  const [search, setSearch] = useState("");
  const registry = useNodeRegistry();

  const categories = registry.getGroupedByCategory();
  const filtered = search
    ? registry.search(search)
    : categories;

  return (
    <div className="node-palette">
      <input placeholder="Search nodes..." value={search} onChange={...} />
      {Object.entries(filtered).map(([category, nodes]) => (
        <details key={category} open>
          <summary>{category} ({nodes.length})</summary>
          {nodes.map(info => (
            <div
              key={info.name}
              draggable
              onDragStart={e => e.dataTransfer.setData('nodeType', info.typeName)}
            >
              {info.name}
            </div>
          ))}
        </details>
      ))}
    </div>
  );
}
```

---

## 8. 상태 관리 (Zustand Store)

### 8.1 GraphStore

```typescript
interface GraphState {
  // 그래프 데이터
  graph: NodeGraph;
  nodes: ReactFlowNode[];     // React Flow 노드 배열
  edges: ReactFlowEdge[];     // React Flow 엣지 배열
  selectedNode: BaseNode | null;

  // 파일 상태
  filePath: string | null;
  isDirty: boolean;

  // 액션
  addNode: (type: string, position: { x: number; y: number }) => void;
  removeNode: (id: string) => void;
  onConnect: (params: ReactFlowConnection) => void;
  removeEdge: (id: string) => void;
  selectNode: (id: string | null) => void;
  updateNodeProperty: (nodeId: string, propName: string, value: any) => void;

  // 그래프 관리
  newGraph: () => void;
  loadGraph: (json: string) => void;
  saveGraph: () => string;

  // React Flow 동기화
  onNodesChange: (changes: NodeChange[]) => void;
  onEdgesChange: (changes: EdgeChange[]) => void;
  syncFromGraph: () => void;  // Core NodeGraph -> React Flow 동기화
}
```

### 8.2 ExecutionStore

```typescript
interface ExecutionState {
  isExecuting: boolean;
  isStreaming: boolean;
  executionTime: number;
  autoExecute: boolean;

  execute: () => void;
  executeForce: () => void;
  startStreaming: () => void;
  stopStreaming: () => void;
  cancel: () => void;
  toggleAutoExecute: () => void;
}
```

### 8.3 UndoStore

```typescript
interface UndoState {
  canUndo: boolean;
  canRedo: boolean;
  undo: () => void;
  redo: () => void;
  pushAction: (action: UndoAction) => void;
}

interface UndoAction {
  description: string;
  execute: () => void;   // redo
  unexecute: () => void; // undo
}
```

---

## 9. 직렬화 (GraphSerializer)

### 9.1 JSON 포맷 (데스크톱 .mvx와 호환)

```json
{
  "version": "1.0",
  "nodes": [
    {
      "id": "A1B2C3D4",
      "typeName": "GaussianBlurNode",
      "x": 300,
      "y": 200,
      "properties": {
        "KernelW": 5,
        "KernelH": 5,
        "SigmaX": 1.5
      }
    }
  ],
  "connections": [
    {
      "sourceNodeId": "A1B2C3D4",
      "sourcePortName": "Result",
      "targetNodeId": "E5F6G7H8",
      "targetPortName": "Image"
    }
  ]
}
```

### 9.2 파일 I/O (Web)

```typescript
// 저장
function saveToFile(graph: NodeGraph, positions: Map<string, {x:number,y:number}>): void {
  const json = GraphSerializer.serialize(graph, positions);
  const blob = new Blob([json], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'project.mvx';
  a.click();
}

// 로드
function loadFromFile(): Promise<string> {
  return new Promise(resolve => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.mvx,.mvxp,.json';
    input.onchange = async () => {
      const file = input.files![0];
      const text = await file.text();
      resolve(text);
    };
    input.click();
  });
}
```

---

## 10. Function Node (서브그래프)

### 10.1 구조

```typescript
class FunctionNode extends BaseNode {
  sourceFilePath: string;
  subGraph: NodeGraph | null;
  subGraphPositions: Map<string, { x: number; y: number }>;
  customName: string | null;

  // .mvx 파일을 로드하여 서브그래프 재구성
  initialize(filePath: string, jsonContent: string): void;

  process(): void {
    // 1. FunctionNode 입력 → 서브그래프 소스 노드 출력에 주입
    // 2. 서브그래프 내부 실행 (자체 GraphExecutor)
    // 3. 서브그래프 싱크 노드 출력 → FunctionNode 출력에 매핑
  }
}
```

### 10.2 경계 분석 (SubGraphAnalyzer)

```typescript
interface BoundaryPort {
  nodeId: string;
  nodeName: string;
  portName: string;
  dataType: string;
  isInput: boolean;       // true = FunctionNode의 입력이 됨
  readFromInputPort: boolean;  // 싱크 노드의 입력 포트를 읽을지 여부
}

class SubGraphAnalyzer {
  static analyze(graph: NodeGraph): BoundaryPort[] {
    // 소스 노드 (in-degree 0) → 출력 포트를 FunctionNode 입력으로
    // 싱크 노드 (out-degree 0) → 출력 포트를 FunctionNode 출력으로
    //   (출력 없는 디스플레이 노드는 입력 포트를 읽음)
  }
}
```

---

## 11. 카메라 통합 (Web)

### 11.1 WebRTC 카메라

데스크톱의 `UsbCameraNode` 대응:

```typescript
class WebCameraNode extends BaseNode implements IStreamingSource {
  private stream: MediaStream | null = null;
  private video: HTMLVideoElement;
  private canvas: OffscreenCanvas;

  setup() {
    this.addOutput<cv.Mat>("Frame");
    this.addDeviceListProperty("DeviceList", "Camera", -1, "Select camera");
    this.addIntProperty("Width", "Width", 640, 320, 3840);
    this.addIntProperty("Height", "Height", 480, 240, 2160);
    this.enumerateDevices();
  }

  async enumerateDevices() {
    const devices = await navigator.mediaDevices.enumerateDevices();
    const cameras = devices
      .filter(d => d.kind === 'videoinput')
      .map((d, i) => ({ name: d.label || `Camera ${i}`, index: i }));
    this.deviceList.updateDeviceOptions(cameras);
  }

  async process() {
    if (!this.stream) await this.openCamera();
    // video -> canvas -> Mat
    this.canvas.getContext('2d')!.drawImage(this.video, 0, 0);
    const imageData = this.canvas.getContext('2d')!.getImageData(0, 0, w, h);
    const mat = cv.matFromImageData(imageData);
    this.setOutputValue(this.frameOutput, mat);
    this.setPreview(mat);
  }
}
```

### 11.2 IP 카메라 (MJPEG/RTSP)

```typescript
class IPCameraNode extends BaseNode implements IStreamingSource {
  // MJPEG: <img> 태그로 스트림 수신 후 canvas로 변환
  // RTSP: 서버 프록시 필요 (브라우저에서 직접 불가)
  // WebRTC relay 서버를 통한 접근 권장
}
```

---

## 12. 성능 최적화

### 12.1 Web Worker 기반 실행

무거운 이미지 처리를 메인 스레드에서 분리:

```typescript
// worker/executionWorker.ts
self.onmessage = async (e) => {
  const { graphData, nodeInputs } = e.data;
  // OpenCV.js 로드 (Worker 내)
  // 그래프 재구성 및 실행
  // 결과 (미리보기 ImageData) 반환
  self.postMessage({ previews, errors, executionTime });
};
```

**주의사항:**
- `cv.Mat`은 Worker와 메인 스레드 간 전송 불가 → `ImageData`로 변환 후 `Transferable`로 전송
- Worker 내에서 별도 OpenCV.js 인스턴스 로드 필요
- SharedArrayBuffer 사용 시 `Cross-Origin-Isolation` 헤더 필요

### 12.2 증분 실행

- `isDirty` 플래그 기반: 변경된 노드와 downstream만 재실행
- 속성 변경 시 debounce (150ms) 후 auto-execute
- 스트리밍 모드: `requestAnimationFrame` 기반 루프

### 12.3 메모리 관리

- OpenCV.js Mat은 WASM 힙 사용 → 반드시 `.delete()` 호출
- 노드 출력 교체 시 이전 Mat 자동 dispose
- 그래프 Clear 시 전체 cleanup
- 큰 이미지: 미리보기 축소 (최대 256px)

---

## 13. Undo/Redo 시스템

데스크톱 앱과 동일한 Command 패턴:

```typescript
class UndoManager {
  private undoStack: UndoAction[] = [];
  private redoStack: UndoAction[] = [];

  push(action: UndoAction): void {
    action.execute();
    this.undoStack.push(action);
    this.redoStack = [];
  }

  undo(): void {
    const action = this.undoStack.pop();
    if (action) {
      action.unexecute();
      this.redoStack.push(action);
    }
  }

  redo(): void {
    const action = this.redoStack.pop();
    if (action) {
      action.execute();
      this.undoStack.push(action);
    }
  }
}
```

**추적 대상:**
- 노드 추가/삭제
- 연결 추가/삭제
- 속성 변경
- 노드 위치 이동

---

## 14. 키보드 단축키

| 단축키 | 동작 |
|--------|------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo |
| `Ctrl+S` | 저장 |
| `Ctrl+Shift+S` | 다른 이름으로 저장 |
| `Ctrl+O` | 열기 |
| `Ctrl+N` | 새 그래프 |
| `Delete` | 선택된 노드/연결 삭제 |
| `Ctrl+A` | 전체 선택 |
| `Ctrl+C` / `Ctrl+V` | 복사/붙여넣기 |
| `F5` | 실행/중지 토글 |
| `F6` | 강제 실행 |
| `Space` | 스트리밍 시작/중지 |
| `Ctrl+F` | 노드 검색 |

---

## 15. 구현 우선순위

### Phase 1 — 코어 엔진 + 기본 UI
1. BaseNode, Port, Connection, NodeProperty, NodeGraph
2. GraphExecutor (토폴로지 정렬, 단발 실행)
3. NodeRegistry (동적 등록)
4. React Flow 캔버스 + 커스텀 노드 렌더러
5. 속성 편집기
6. 노드 팔레트 (카테고리, 검색, D&D)
7. OpenCV.js 로딩 및 Mat 유틸리티
8. 기본 노드 10개: ImageRead, ImageShow, GaussianBlur, Canny, Threshold, CvtColor, Resize, Crop, Add, BitwiseNot

### Phase 2 — 완전한 노드 세트
9. Filter 카테고리 전체 (10노드)
10. Edge, Morphology, Threshold 전체
11. Contour 전체 (13노드)
12. Arithmetic 전체 (11노드)
13. Color, Transform, Histogram 전체
14. Detection 전체 (9노드)
15. Drawing 전체 (10노드)
16. Value, Control 전체

### Phase 3 — 고급 기능
17. 스트리밍 실행 엔진 (카메라용)
18. WebRTC 카메라 노드
19. 루프 실행 (For, While, ForEach, Collect, BreakIf)
20. Undo/Redo 시스템
21. 직렬화/역직렬화 (저장/로드)
22. Feature Detection 전체

### Phase 4 — 프로 기능
23. FunctionNode (서브그래프 임포트/실행)
24. Inspection, Measurement 카테고리
25. Data Processing, Communication 노드
26. Script 노드 (Pyodide)
27. 코드 생성 (Python/JavaScript)
28. 테마 (다크/라이트)
29. Web Worker 기반 병렬 실행
30. PWA (오프라인 지원)

---

## 16. 데스크톱 ↔ Web 호환성 매트릭스

| 기능 | 데스크톱 (C#/WPF) | Web (React/TS) | 비고 |
|------|-------------------|----------------|------|
| OpenCV 처리 | OpenCvSharp | OpenCV.js (WASM) | API 거의 동일 |
| 노드 에디터 | Nodify | React Flow | 유사한 UX |
| USB 카메라 | VideoCapture | getUserMedia | WebRTC |
| HIK/Cognex 카메라 | 네이티브 SDK | 불가 | 서버 프록시 필요 |
| TCP/Serial 통신 | System.Net/IO | WebSocket | 프로토콜 변환 |
| 파일 I/O | System.IO | File API | 제한적 |
| Python 스크립트 | Process spawn | Pyodide/서버 | WASM Python |
| Undo/Redo | 동일 | 동일 | Command 패턴 |
| 직렬화 포맷 | JSON (.mvx) | JSON (.mvx) | **완전 호환** |
| 프로젝트 아카이브 | ZIP (.mvxp) | JSZip (.mvxp) | 호환 가능 |
| 키보드/마우스 훅 | Win32 API | 브라우저 이벤트 | 전역 훅 불가 |
| 멀티스레드 | Task/Thread | Web Worker | 제한적 |

---

## 17. 테스트 전략

```
tests/
├── core/
│   ├── BaseNode.test.ts
│   ├── NodeGraph.test.ts
│   ├── GraphExecutor.test.ts
│   ├── Connection.test.ts
│   └── GraphSerializer.test.ts
├── nodes/
│   ├── filter.test.ts
│   ├── edge.test.ts
│   ├── contour.test.ts
│   └── ...
└── integration/
    ├── pipeline.test.ts      # 전체 파이프라인 테스트
    └── serialization.test.ts # 저장/로드 왕복 테스트
```

- **단위 테스트**: Vitest
- **E2E**: Playwright
- **OpenCV.js 테스트**: Node.js 환경에서 opencv4nodejs 또는 WASM 로드

---

## 18. 빌드 및 배포

```bash
# 개발
npm run dev          # Vite dev server (http://localhost:5173)

# 빌드
npm run build        # dist/ 생성

# 프리뷰
npm run preview      # 빌드 결과 로컬 서빙

# 테스트
npm run test         # Vitest
npm run test:e2e     # Playwright
```

**배포 옵션:**
- 정적 호스팅 (Vercel, Netlify, GitHub Pages) — 서버리스
- Docker + Nginx — 자체 호스팅
- Electron wrapper — 데스크톱 재패키징 (옵션)
