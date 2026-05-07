# Angular UI Design — Decoupled Interface Project
## MultiAgentLab POC · Revision de Historias de Usuario

> Framework decision: **Angular 17+ (standalone components, Signals, RxJS)**
> Adapted from `06_Discusion_Refactor_UI_Desacoplada.md` replacing the React/Vite proposal.

---

## 1. Reality Check — Actual Backend API

Before designing, the real endpoints were audited from `ReviewEndpoints.cs`. This supersedes the proposed API in doc `06`.

### 1.1 Existing Endpoints (no changes required)

| Method | Path | Response | Notes |
|---|---|---|---|
| `POST` | `/review-story` | `ReviewResult` | **Sync** — blocks until all agents complete |
| `POST` | `/mock-cases/{id}/start` | `{ executionId, caseId, status }` | **Async** — fires pipeline, returns immediately |
| `POST` | `/mock-cases/{id}/run` | `{ mockCase, result }` | Sync version of above |
| `GET` | `/mock-cases` | `MockCaseSummary[]` | List with expectedAgents, expectedStatus |
| `GET` | `/executions` | `ExecutionSummary[]` | History list |
| `GET` | `/executions/{id}` | `ExecutionLogEvent` | Last log event (final_result_generated) |
| `GET` | `/executions/{id}/log` | `ExecutionLogEvent[]` | **Full event array** ← used for polling |
| `GET` | `/executions/{id}/log/text` | `text/plain` | Human-readable formatted log |
| `GET` | `/dashboard` | `text/html` | Existing inline HTML UI (to be replaced) |

### 1.2 Required Backend Change (one-time, minimal)

| Change | File | Why |
|---|---|---|
| Add CORS for `localhost:4200` | `Program.cs` | Browser blocks cross-origin requests from Angular dev server |

### 1.3 Real-time Strategy: Polling (Phase 1)

No SSE endpoint exists. The Angular `ExecutionStreamService` will **poll** `GET /executions/{id}/log` every 1 second, diff against previously seen events, and emit new ones as an `Observable<ExecutionLogEvent>`. It stops when `request_completed` is received.

This is fully transparent to the components — if SSE is added later, only `ExecutionStreamService` changes.

### 1.4 Domain Models (from C# source)

```typescript
// ReviewRequest
{ storyId, title, storyText, provider: ProviderSelection, logging?: LoggingOptions }

// ProviderSelection
{ type: 'ollama'|'bedrock', model, endpoint?, region?, temperature?, maxTokens? }

// LoggingOptions
{ level: 'basic'|'standard'|'full', includePrompts?, includeResponses? }

// ReviewResult
{ executionId, status: 'verde'|'amarillo'|'rojo', summary?, provider, model,
  invokedAgents, skippedAgents: SkippedAgent[], issues, recommendations,
  conflicts, resolution, agentResults: AgentResult[] }

// AgentResult
{ agent, status, score, issues, recommendations, questions, rawSummary? }

// SkippedAgent
{ agent, reason }

// ExecutionLogEvent
{ executionId, timestamp, eventType, data: unknown }

// MockCaseSummary  (from GET /mock-cases)
{ caseId, title, description, expectedAgents, expectedStatus }
```

### 1.5 Event Types (from execution log)

| eventType | data shape | Consumer |
|---|---|---|
| `request_received` | `{ storyId, title }` | Timeline |
| `supervisor_started` | `{}` | Timeline |
| `selected_agents` | `{ invoked: string[], skipped: [{agent,reason}] }` | **DecisionPanel**, Timeline |
| `agent_started` | `{ agent }` | **DecisionPanel**, Timeline |
| `agent_prompt_sent` | `{ agent, prompt }` | Timeline (collapsible) |
| `agent_response_received` | `{ agent, response }` | Timeline (collapsible) |
| `agent_completed` | `{ agent, status, score, issues }` | **DecisionPanel**, Timeline |
| `agent_failed` | `{ agent, error }` | **DecisionPanel**, Timeline |
| `conflict_detected` | `{ conflicts: string[] }` | Timeline |
| `supervisor_resolution` | `{ resolution: string[] }` | Timeline |
| `final_result_generated` | `ReviewResult` | **FinalResultPanel**, Timeline |
| `request_completed` | `{ totalMs }` | All panels (signals end of stream) |

---

## 2. Project Structure

```
MultiAgentLab/                      ← monorepo root
├── src/                            ← existing .NET backend (unchanged)
├── ui/                             ← NEW standalone Angular project
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   │   ├── models/
│   │   │   │   │   └── api.models.ts          ← all TS interfaces (from §1.4)
│   │   │   │   └── services/
│   │   │   │       ├── review-api.service.ts      ← HttpClient wrappers
│   │   │   │       ├── execution-stream.service.ts ← polling abstraction
│   │   │   │       └── execution-state.service.ts  ← signals-based state
│   │   │   ├── features/
│   │   │   │   ├── history-input/
│   │   │   │   │   ├── history-input.component.ts
│   │   │   │   │   ├── history-input.component.html
│   │   │   │   │   └── history-input.component.scss
│   │   │   │   ├── decision-panel/
│   │   │   │   │   ├── decision-panel.component.ts
│   │   │   │   │   ├── decision-panel.component.html
│   │   │   │   │   └── decision-panel.component.scss
│   │   │   │   ├── event-timeline/
│   │   │   │   │   ├── event-timeline.component.ts
│   │   │   │   │   ├── event-timeline.component.html
│   │   │   │   │   └── event-timeline.component.scss
│   │   │   │   └── final-result/
│   │   │   │       ├── final-result.component.ts
│   │   │   │       ├── final-result.component.html
│   │   │   │       └── final-result.component.scss
│   │   │   ├── shared/
│   │   │   │   └── components/
│   │   │   │       ├── agent-card/
│   │   │   │       │   └── agent-card.component.ts   ← reusable agent status card
│   │   │   │       └── status-badge/
│   │   │   │           └── status-badge.component.ts ← verde/amarillo/rojo badge
│   │   │   ├── app.component.ts
│   │   │   ├── app.component.html
│   │   │   ├── app.component.scss
│   │   │   └── app.config.ts                         ← provideHttpClient, providers
│   │   ├── environments/
│   │   │   ├── environment.ts                        ← apiBaseUrl, pollIntervalMs
│   │   │   └── environment.prod.ts
│   │   ├── assets/
│   │   │   └── mock/
│   │   │       ├── mock-cases.json                   ← for offline development
│   │   │       └── execution-log.mock.json
│   │   └── styles.scss                               ← global styles + Material theme
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   ├── tsconfig.app.json
│   ├── jest.config.ts
│   └── README.md
├── docker-compose.yml              ← NEW: starts backend + ui
└── README.md                       ← updated monorepo instructions
```

---

## 3. Layout Design

### 3.1 Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  MultiAgentLab  ·  POC — Revisión de Historias        ● Polling: Connected  │
├────────────────────┬──────────────────────┬────────────────────────────────-┤
│  STORY INPUT       │  AGENT DECISIONS      │  EVENT TIMELINE                 │
│                    │                       │                                 │
│  [Mock Case ▼]     │  ✓ clarity  0.85      │  10:01:01  ▶ request_received   │
│                    │  ✓ ux       0.90      │  10:01:01  ⚙ supervisor_started │
│  [─── textarea ──] │  ⟳ qa       running   │  10:01:02  🎯 selected_agents   │
│  [Story text...  ] │  ─ technical skipped  │  10:01:02  ┌─ clarity STARTED   │
│  [───────────────] │  ─ compliance skipped │  10:01:04  └─ clarity OK 0.85   │
│                    │                       │  10:01:04  ┌─ ux STARTED        │
│  Provider [Ollama▼]│                       │  ...                            │
│  Model    [qwen ▼] │                       │  [Filter ▼] [All event types]   │
│  Level    [std  ▼] │                       │                                 │
│  [▶ Review Story ] │                       │                                 │
├────────────────────┴──────────────────────┴─────────────────────────────────┤
│  FINAL RESULT                                                    [● VERDE ]  │
│                                                                              │
│  Summary: Historia simple y clara, sin impacto técnico relevante.            │
│  Issues (0)  ·  Recommendations (1)  ·  Agents: clarity, ux                │
│  ▶ Agent Details                                                             │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Panel Grid (CSS Grid)

```scss
.app-grid {
  display: grid;
  grid-template-columns: 320px 1fr 1fr;   // left fixed, center/right flex
  grid-template-rows: auto 1fr auto;
  grid-template-areas:
    "header  header  header"
    "input   decision timeline"
    "result  result  result";
  height: 100vh;
}
```

---

## 4. Component Specifications

### 4.1 AppComponent

**Responsibility**: Layout shell + coordinates execution lifecycle.

```typescript
@Component({ standalone: true, ... })
export class AppComponent {
  protected readonly state = inject(ExecutionStateService);

  startExecution(executionId: string): void {
    this.state.reset();
    this.state.begin(executionId);
  }
}
```

**State flow it manages:**
```
idle ──[user submits]──► executing ──[request_completed]──► complete
                                  ↘ [error]──► error
```

---

### 4.2 HistoryInputComponent

**States**: `idle`, `submitting`, `submitted`

**Inputs from API**: `GET /mock-cases` → populates mock case selector on init.

**Actions**:
- Select mock case → pre-fill `storyText`, `provider`, `model` from mock case
- Manually type story → free form
- Submit: calls `POST /mock-cases/{id}/start` (if mock case selected) OR `POST /review-story` (if manual)
- Emits `executionStarted(executionId)` → AppComponent coordinates the stream

**Key UX rules**:
- Button disabled while submitting or execution in progress
- Text area disabled while execution in progress
- Shows spinner on submit button while `submitting`
- Reset button clears everything and returns to `idle`

**Template sketch**:
```html
<mat-card>
  <mat-card-header>Story Input</mat-card-header>
  <mat-card-content>
    <mat-select placeholder="Load mock case..." (selectionChange)="onMockCaseSelect($event)">
      @for (c of mockCases(); track c.caseId) {
        <mat-option [value]="c">{{ c.title }}</mat-option>
      }
    </mat-select>
    <mat-form-field>
      <textarea matInput [(ngModel)]="storyText" rows="6" placeholder="Write or paste a user story..."></textarea>
    </mat-form-field>
    <!-- provider / model / logging level selects -->
    <button mat-raised-button color="primary" (click)="submit()" [disabled]="!canSubmit()">
      @if (state() === 'submitting') { <mat-spinner diameter="18"></mat-spinner> }
      Review Story
    </button>
  </mat-card-content>
</mat-card>
```

---

### 4.3 DecisionPanelComponent

**States**: `empty`, `executing`, `complete`

**Reacts to events**:
- `selected_agents` → create `AgentCard` for each invoked agent (`state: 'selected'`) and each skipped agent (`state: 'skipped'`)
- `agent_started` → update card to `state: 'executing'`
- `agent_completed` → update card to `state: 'completed'`, set `score`, `issues`
- `agent_failed` → update card to `state: 'failed'`, set `error`

**AgentCard model**:
```typescript
interface AgentCard {
  name: string;
  state: 'selected' | 'executing' | 'completed' | 'failed' | 'skipped';
  score?: number;
  issueCount?: number;
  error?: string;
  skipReason?: string;
}
```

**AgentCardComponent** (shared, reusable):

| Card state | Visual |
|---|---|
| `selected` | Grey chip, clock icon |
| `executing` | Blue chip, spinner |
| `completed` | Green chip, checkmark, score badge |
| `failed` | Red chip, X icon, error tooltip |
| `skipped` | Muted chip, dash icon, reason tooltip |

---

### 4.4 EventTimelinePanelComponent

**States**: `empty`, `streaming`, `complete`

**Reacts to**: every `ExecutionLogEvent` in order

**Features**:
- Auto-scrolls to bottom as new events arrive
- Filter dropdown by `eventType` (hides noisy prompt/response events by default)
- `agent_prompt_sent` and `agent_response_received` are collapsed by default (expandable)
- Timestamp shown as `HH:mm:ss.SSS`
- Each event type has a distinct color/icon (matching existing dashboard style)

**Event type → display**:

| eventType | Icon | Color class |
|---|---|---|
| `request_received` | `▶` | `ev-request` |
| `supervisor_started` | `⚙` | `ev-supervisor` |
| `selected_agents` | `🎯` | `ev-selection` |
| `agent_started` | `┌─` | `ev-agent-start` |
| `agent_completed` | `└─ ✓` | `ev-agent-ok` |
| `agent_failed` | `└─ ✗` | `ev-agent-fail` |
| `conflict_detected` | `⚡` | `ev-conflict` |
| `supervisor_resolution` | `✔` | `ev-resolution` |
| `final_result_generated` | `★` | `ev-result` |
| `request_completed` | `■` | `ev-done` |

**Default filter**: hide `agent_prompt_sent` and `agent_response_received` (shown only if `logging.level === 'full'` and user explicitly enables).

---

### 4.5 FinalResultComponent

**States**: `empty`, `pending`, `complete`, `partial`, `error`

| State | Condition | Display |
|---|---|---|
| `empty` | No execution started | Placeholder: "Submit a story to see the result" |
| `pending` | Execution in progress, `final_result_generated` not yet received | Spinner + "Analysis in progress..." |
| `complete` | `final_result_generated` received, status is `verde` or `amarillo` | Full result card |
| `partial` | Some agents failed (status `amarillo` + failed agent cards) | Result with warning banner |
| `error` | Status `rojo` or execution error | Error card with details |

**StatusBadgeComponent** (shared):
```
verde    → green chip  ● Verde
amarillo → yellow chip ● Amarillo
rojo     → red chip    ● Rojo
```

**Layout**:
```
[● STATUS BADGE]   Summary text...

Issues (N)           Recommendations (N)       Agents invoked (N)
▸ Issue 1            ▸ Rec 1                   clarity · ux
▸ Issue 2

▼ Agent Details
  ┌─ clarity | score: 0.85 | 0 issues
  ├─ ux      | score: 0.90 | 0 issues
  └─ [skipped: technical → "No technical impact detected"]
```

---

## 5. Services

### 5.1 ReviewApiService

```typescript
@Injectable({ providedIn: 'root' })
export class ReviewApiService {
  private readonly http = inject(HttpClient);
  private readonly env = environment;

  getMockCases(): Observable<MockCaseSummary[]>
  // GET /mock-cases

  startMockCase(caseId: string): Observable<StartCaseResponse>
  // POST /mock-cases/{caseId}/start → { executionId, caseId, status }

  reviewStory(request: ReviewRequest): Observable<ReviewResult>
  // POST /review-story → ReviewResult (sync)

  getExecutionLog(executionId: string): Observable<ExecutionLogEvent[]>
  // GET /executions/{executionId}/log

  getExecutions(): Observable<ExecutionSummary[]>
  // GET /executions
}
```

---

### 5.2 ExecutionStreamService

Abstracts polling. Components never know whether updates come from polling or SSE.

```typescript
@Injectable({ providedIn: 'root' })
export class ExecutionStreamService {
  private readonly api = inject(ReviewApiService);

  /** Stream of new events for a given executionId.
   *  Polls GET /executions/{id}/log every pollIntervalMs.
   *  Emits only new events (diff from previous poll).
   *  Completes when 'request_completed' is received.
   *  Errors on HTTP failure after maxRetries. */
  stream(executionId: string): Observable<ExecutionLogEvent> {
    return timer(0, environment.pollIntervalMs).pipe(
      switchMap(() => this.api.getExecutionLog(executionId)),
      scan(({ seen }, events) => {
        const newEvents = events.slice(seen);
        return { seen: events.length, newEvents };
      }, { seen: 0, newEvents: [] as ExecutionLogEvent[] }),
      concatMap(({ newEvents }) => from(newEvents)),
      takeWhile(e => e.eventType !== 'request_completed', true),
      shareReplay(1)
    );
  }

  // Connection status (for the UI indicator)
  readonly status$ = new BehaviorSubject<StreamStatus>('idle');
}

type StreamStatus = 'idle' | 'polling' | 'complete' | 'error';
```

**Phase 2 upgrade path** (SSE): Replace the `timer + switchMap` block with `new EventSource(url)` wrapped in `fromEventSource()`. Interface to components stays identical.

---

### 5.3 ExecutionStateService

Central state using Angular Signals (no NgRx needed for POC).

```typescript
@Injectable({ providedIn: 'root' })
export class ExecutionStateService {
  // ── Signals (readable by components) ──────────────────────────────
  readonly executionId  = signal<string | null>(null);
  readonly overallState = signal<OverallState>('idle');
  readonly agentCards   = signal<AgentCard[]>([]);
  readonly events       = signal<ExecutionLogEvent[]>([]);
  readonly finalResult  = signal<ReviewResult | null>(null);
  readonly streamStatus = signal<StreamStatus>('idle');

  // ── Computed ───────────────────────────────────────────────────────
  readonly isExecuting  = computed(() => this.overallState() === 'executing');
  readonly statusColor  = computed(() => {
    const s = this.finalResult()?.status;
    return s === 'verde' ? 'success' : s === 'amarillo' ? 'warning' : s === 'rojo' ? 'error' : 'default';
  });

  // ── Actions ───────────────────────────────────────────────────────
  begin(executionId: string): void { ... }  // sets executionId, overallState='executing', subscribes to stream
  reset(): void { ... }                     // clears all signals, cancels subscription
  abort(): void { ... }                     // cancels in-flight stream

  private applyEvent(event: ExecutionLogEvent): void {
    this.events.update(e => [...e, event]);
    switch (event.eventType) {
      case 'selected_agents': /* populate agentCards */ break;
      case 'agent_started':   /* update card state */ break;
      case 'agent_completed': /* update card score */ break;
      case 'agent_failed':    /* update card error */ break;
      case 'final_result_generated': /* set finalResult */ break;
      case 'request_completed': /* set overallState='complete' */ break;
    }
  }
}

type OverallState = 'idle' | 'executing' | 'complete' | 'error';
```

---

## 6. Environment Configuration

```typescript
// src/environments/environment.ts  (dev)
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',   // .NET default
  pollIntervalMs: 1000,
  useMock: false,
};

// src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiBaseUrl: '',                        // relative — served from same origin
  pollIntervalMs: 2000,
  useMock: false,
};
```

**Angular CLI build replacement** in `angular.json`:
```json
"fileReplacements": [
  { "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.prod.ts" }
]
```

> No `.env` files needed — Angular uses TypeScript environment files natively. For Docker, `apiBaseUrl` is injected at build time via `--configuration=production` + environment replacement.

---

## 7. Backend Change Required — CORS

One change needed in `Program.cs` to allow the Angular dev server:

```csharp
// Program.cs — add before builder.Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// After builder.Build() — add before app.UseSwagger()
app.UseCors("Angular");
```

---

## 8. Tech Stack

| Concern | Technology | Version |
|---|---|---|
| Framework | Angular | 17+ |
| Language | TypeScript | 5.x |
| UI Components | Angular Material | 17+ |
| Icons | Material Icons | (bundled with Angular Material) |
| State | Angular Signals | built-in |
| Reactivity | RxJS | 7.x |
| Build | Angular CLI | 17+ |
| Styles | SCSS + Angular Material theme | — |
| Unit tests | Jest + `jest-preset-angular` | — |
| E2E tests | Playwright | latest |
| HTTP mock (dev) | Angular `HttpInterceptor` + JSON assets | — |
| Container | Docker + Nginx (production) | — |

---

## 9. Angular Material Theme

```scss
// styles.scss
@use '@angular/material' as mat;

$primary: mat.define-palette(mat.$blue-palette, 600);
$accent:  mat.define-palette(mat.$amber-palette, 600);
$warn:    mat.define-palette(mat.$red-palette, 600);

$theme: mat.define-dark-theme((
  color: (primary: $primary, accent: $accent, warn: $warn)
));

@include mat.all-component-themes($theme);

// Status colors
.status-verde    { color: #4ade80; }
.status-amarillo { color: #facc15; }
.status-rojo     { color: #f87171; }
```

Dark theme matches the existing backend dashboard style.

---

## 10. Testing Strategy

### 10.1 Unit Tests (Jest)

| Component/Service | Test cases |
|---|---|
| `ReviewApiService` | Each method returns correct mapped response; error handling |
| `ExecutionStreamService` | Emits only new events; completes on `request_completed`; retries on HTTP error |
| `ExecutionStateService` | `begin()` sets state; each event type correctly updates signals; `reset()` clears all |
| `HistoryInputComponent` | Empty state; submitting state; mock case pre-fill; disabled during execution |
| `DecisionPanelComponent` | Empty state; agent cards created on `selected_agents`; spinner on `agent_started`; checkmark on `agent_completed` |
| `EventTimelinePanelComponent` | Empty state; events appended; filter hides prompt events by default |
| `FinalResultComponent` | Empty state; pending state; complete with verde/amarillo/rojo; partial warning |

Mock strategy: `HttpClientTestingModule` for service tests; `NO_ERRORS_SCHEMA` for component unit tests.

### 10.2 E2E Tests (Playwright)

| Scenario | Steps |
|---|---|
| Happy path (mock case) | Load app → select mock case 01 → click Review → see agent cards update → see final result verde |
| Manual story input | Type story → select Ollama/qwen → submit → see pipeline run → see result |
| Partial result | Mock case with failing agent → result shows with warning banner |
| Backend unavailable | API returns 500 → UI shows error state, not blank screen |
| Event filter | Expand timeline → enable prompts filter → prompt events appear |
| Reset | After execution → click reset → all panels return to empty state |

---

## 11. Implementation Sequence

```
Step 1  ── Backend: Add CORS to Program.cs (5 min)

Step 2  ── Scaffold Angular project
           ng new multiagentlab-ui --standalone --routing=false --style=scss
           cd ui && ng add @angular/material

Step 3  ── Core models
           src/app/core/models/api.models.ts  (from §1.4)

Step 4  ── ReviewApiService
           All HTTP methods, environment.apiBaseUrl, typed responses

Step 5  ── ExecutionStreamService
           Polling loop with RxJS timer + scan + takeWhile

Step 6  ── ExecutionStateService
           Signals, event router (applyEvent switch), begin/reset/abort

Step 7  ── HistoryInputComponent
           Mock case selector + text area + provider/model form + submit

Step 8  ── DecisionPanelComponent + AgentCardComponent
           Reactive to agentCards signal

Step 9  ── EventTimelinePanelComponent
           Reactive to events signal, filter, auto-scroll

Step 10 ── FinalResultComponent + StatusBadgeComponent
           Reactive to finalResult signal

Step 11 ── AppComponent wiring
           Grid layout, connect HistoryInput → state.begin()

Step 12 ── Unit tests (Jest)
           Services first, then components

Step 13 ── E2E (Playwright)

Step 14 ── Docker Compose
           /ui Dockerfile (ng build → Nginx), docker-compose.yml at root

Step 15 ── README.md
           Setup, dev, test, docker instructions
```

---

## 12. Acceptance Criteria (Angular-adapted from doc 06)

- **AC-01**: `cd ui && npm install && ng serve` starts the app at `localhost:4200` with no backend required (mock interceptor active by default in dev).
- **AC-02**: Changing `environment.apiBaseUrl` is the only change to point the UI at a different backend.
- **AC-03**: Adding a new panel requires changes only within `ui/src/app/features/` — zero backend changes.
- **AC-04**: `docker-compose up` at repo root starts both services; demo accessible at `localhost:4200`.
- **AC-05**: Submitting a mock case shows agent cards updating progressively as polling events arrive; `FinalResultComponent` renders only after `final_result_generated` is received.
- **AC-06**: All four panels have explicit `empty`, `loading/pending`, `error`, and `success` states.
- **AC-07**: The three status values (`verde`, `amarillo`, `rojo`) are visually distinct and labeled.
- **AC-08**: Component unit tests for all four panels run without a live backend.

---

## 13. Decisions Overriding Doc 06

| Doc 06 decision | Angular adaptation |
|---|---|
| React + TypeScript + Vite | Angular 17 + TypeScript + Angular CLI |
| `VITE_API_BASE_URL` env var | `environment.ts` file replacement |
| `useExecutionStream` hook | `ExecutionStreamService` (RxJS Observable) |
| MSW Mock Service Worker | Angular `HttpInterceptor` + JSON assets |
| Vitest + MSW unit tests | Jest (`jest-preset-angular`) + `HttpClientTestingModule` |
| React context / useState | Angular Signals + `ExecutionStateService` |
| `openapi-typescript` type gen | Manual `api.models.ts` derived from C# domain (simpler for POC) |

All architectural decisions from doc 06 remain valid: monorepo, polling→SSE upgrade path, Docker Compose, OpenAPI spec as future contract artifact.
