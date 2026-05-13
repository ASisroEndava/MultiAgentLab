# Multi-Agent Discussion: UI Refactor — Decoupled Interface Project
## Multi-Agent POC for User Story Review

---

## Pipeline Execution

```
IntakeAgent → SpecialistRouter
    → [ProductAnalystAgent, ArchitectureAgent, SolutionArchitectAgent,
       BackendDataAgent, UXAgent, MultiAgentSystemsAgent, QAExpertAgent]
    → ReviewerAgent
    → TechLeadReviewAgent
    → SummaryAgent
```

---

## Stage 1 — IntakeAgent

**Input received:**
> "Refactor of the UI. The user interface must be decoupled from the main project and generated as its own standalone project so that it is easily extensible and adaptable."

**Output:**

```json
{
  "agentName": "IntakeAgent",
  "summary": "The current UI/Demo Console is embedded in or tightly co-located with the main POC project. The requirement is to extract it into an independent, standalone project with its own lifecycle, build system, and deployment. The new UI project must communicate with the backend exclusively through the Review API contract, making it replaceable, extensible to new frameworks or contexts, and adaptable to different deployment scenarios (local demo, cloud, CI/CD pipeline).",
  "observations": [
    "Current architecture defines UI as 'a console, a simple web page, or a Postman endpoint' (section 4.1) — no separation of concerns enforced",
    "The API boundary (Review API) already exists as a conceptual seam — this is the correct decoupling point",
    "Section 9.3 proposes a 4-panel demo view that requires real-time data — any decoupled UI must support SSE or polling",
    "No current definition of API contract as a formal spec (no OpenAPI)"
  ],
  "risks": [],
  "recommendations": [],
  "openQuestions": [
    "Monorepo (ui/ package within the same repo) or separate repository?",
    "Should the UI framework be fixed (React, Vue, plain HTML) or kept framework-agnostic?",
    "Is a development mock server needed for UI development without a running backend?"
  ],
  "confidence": 0.95,
  "flags": {
    "hasUi": true,
    "hasBackendImpact": true,
    "hasIntegration": true,
    "hasSensitiveData": false,
    "hasSecurityImplications": false
  }
}
```

---

## Stage 2 — SpecialistRouter

```json
{
  "agentName": "SpecialistRouter",
  "summary": "Selected agent pipeline for UI decoupling refactor.",
  "observations": [
    "ProductAnalystAgent — required: defines functional goals and acceptance criteria for the new project structure",
    "ArchitectureAgent — required: hasBackendImpact=true, must analyze impact on existing components",
    "SolutionArchitectAgent — required: must propose the target architecture for the standalone UI project",
    "BackendDataAgent — required: hasIntegration=true, API contract must be formalized",
    "UXAgent — required: hasUi=true, decoupled UI must preserve UX design and state model",
    "MultiAgentSystemsAgent — required: evaluate impact on the overall pipeline and deployment",
    "QAExpertAgent — required: define test strategy for a standalone UI project"
  ],
  "risks": [],
  "recommendations": ["SecurityAgent not required: hasSensitiveData=false, hasSecurityImplications=false for this refactor"],
  "openQuestions": [],
  "confidence": 1.0
}
```

---

## Stage 3 — Specialist Findings

---

### 3.1 ProductAnalystAgent

```json
{
  "agentName": "ProductAnalystAgent",
  "summary": "The goal of this refactor is to achieve independent evolvability of the UI layer without modifying the backend. The UI project must be a first-class deliverable with its own lifecycle.",
  "observations": [
    "Actors: (1) Developer extending the UI with new views or frameworks; (2) Demo presenter running a local or cloud demo; (3) Any API consumer replacing the UI entirely with a different client",
    "Primary value flow: User inputs a story in the UI → UI calls Review API → UI renders real-time agent decisions and final result",
    "Implicit business rule: the UI must never contain analysis logic — it is purely a presentation and interaction layer",
    "Implicit business rule: switching UI implementations must require zero changes to the backend codebase",
    "Ambiguity resolved: 'extensible' means new panels, new views, or new framework implementations can be added without touching the API; 'adaptable' means the same UI project can be pointed at different API environments via configuration"
  ],
  "risks": [
    "If the API contract is not formally specified before UI decoupling, both projects will diverge independently",
    "Without a mock server, UI developers cannot work without a running backend — this blocks parallel development"
  ],
  "recommendations": [
    "Define acceptance criteria for the decoupled project before starting the refactor",
    "Establish the OpenAPI spec as the contract artifact that both projects share",
    "Provide a mock server (or static JSON fixtures) so the UI can be developed independently"
  ],
  "openQuestions": [
    "Should the UI project own its own README, package.json, and CI pipeline from day one?",
    "Is the target audience for the UI strictly internal demo, or will external stakeholders access it?"
  ],
  "confidence": 0.9
}
```

**Acceptance Criteria (preliminary):**

- **AC-01**: The UI project builds and runs independently with only an API base URL as configuration.
- **AC-02**: Changing the API base URL (e.g., from localhost to a cloud endpoint) requires only an environment variable change — no code modification.
- **AC-03**: Adding a new panel or view to the UI requires no changes to the backend or any other project.
- **AC-04**: The UI project can run against a mock API server without a live backend.
- **AC-05**: The Review API is the only communication boundary — the UI does not access the database, logger, or any internal service directly.
- **AC-06**: The decoupled UI must reproduce all behaviors defined in section 9.3 of the architecture document (4-panel demo view with real-time events).

---

### 3.2 ArchitectureAgent

```json
{
  "agentName": "ArchitectureAgent",
  "summary": "Decoupling the UI introduces a clean architectural boundary and has minimal but critical impact on the existing system. The Review API becomes the single, hardened seam between the two projects.",
  "observations": [
    "Current state: UI is co-located or loosely described within the same project boundary — no enforced separation",
    "Components directly impacted: (1) Review API — must expose all data needed by the UI with no implicit assumptions about co-location; (2) Mock Case Loader — must be accessible via API, not just in-memory",
    "Components unaffected: Supervisor Service, Model Router, Bedrock/Ollama adapters, Execution Logger internals",
    "The current architecture diagram (section 3) already shows UI → API as a separate arrow — the refactor formalizes what was conceptual",
    "New coupling risk: if the UI project imports any type definitions or utilities from the backend project directly (e.g., shared TypeScript types), a hidden dependency is created — this must be prevented"
  ],
  "risks": [
    "Shared types/models between UI and backend create hidden coupling — must be avoided or explicitly managed via a shared contract package",
    "The Mock Case Loader (section 4.9) is currently described as accessible via the API, but if it is only an in-memory fixture, the decoupled UI cannot use it without a running backend"
  ],
  "recommendations": [
    "Enforce the API boundary strictly: the UI project must have zero imports from the backend project",
    "If TypeScript is used in both projects, consider a shared 'contracts' package (types only, no logic) that both projects depend on",
    "Document the seam explicitly: which API endpoints does the UI consume, and what is the exact shape of each response"
  ],
  "openQuestions": [
    "Will the Mock Case Loader be exposed via a real API endpoint (GET /mock-cases) or will the UI include its own fixture files?",
    "Is CORS configuration on the Review API already defined for cross-origin requests from the standalone UI?"
  ],
  "confidence": 0.88
}
```

---

### 3.3 SolutionArchitectAgent

```json
{
  "agentName": "SolutionArchitectAgent",
  "summary": "The target architecture is a standalone SPA (Single Page Application) project that communicates with the Review API exclusively via REST and SSE. The project is self-contained, environment-configurable, and independently deployable.",
  "observations": [
    "Recommended project structure: monorepo with two top-level packages — /backend (existing) and /ui (new)",
    "Monorepo is preferred over polyrepo for a POC: single clone, shared git history, easier to keep in sync, no cross-repo dependency management",
    "Framework recommendation: React + TypeScript for extensibility; plain HTML+JS+fetch as the minimal fallback if the team prefers no build tooling for the POC",
    "Real-time mechanism: SSE (Server-Sent Events) from GET /execution/{id}/events — unidirectional, sufficient, lower complexity than WebSockets",
    "Configuration: API base URL via environment variable (VITE_API_BASE_URL or similar) — no hardcoded URLs in source code",
    "Build output: static assets (HTML, JS, CSS) — deployable to any static host, CDN, or served by the backend itself in production"
  ],
  "risks": [
    "If the team uses plain HTML+JS, extensibility is limited — adding new views requires discipline without a component model",
    "SSE requires the Review API to keep a connection open per execution — must validate that the backend framework supports this without blocking"
  ],
  "recommendations": [
    "Use Vite as the build tool for React — fast, minimal config, supports env variables natively",
    "Define a /ui/README.md from day one with: how to install, how to configure, how to run against mock, how to run against live backend",
    "The API contract (OpenAPI spec) lives in /backend/docs/openapi.yaml and is the source of truth for both projects"
  ],
  "openQuestions": [
    "Should the backend serve the built UI assets in production (single deployable) or should they remain separate deployables?",
    "Is there a preference for CSS framework (Tailwind, plain CSS, Bootstrap) for the demo UI?"
  ],
  "confidence": 0.92
}
```

**Proposed Project Structure:**

```
MultiAgentLab/                  ← monorepo root
├── backend/                    ← existing project (renamed or kept as-is)
│   ├── src/
│   ├── docs/
│   │   └── openapi.yaml        ← NEW: API contract (source of truth)
│   └── ...
├── ui/                         ← NEW: standalone UI project
│   ├── src/
│   │   ├── components/
│   │   │   ├── HistoryInputPanel/
│   │   │   ├── DecisionPanel/
│   │   │   ├── EventTimelinePanel/
│   │   │   └── FinalResultPanel/
│   │   ├── api/
│   │   │   └── reviewApi.ts    ← all API calls centralized here
│   │   ├── hooks/
│   │   │   └── useExecutionStream.ts  ← SSE hook
│   │   └── App.tsx
│   ├── mock/
│   │   └── fixtures/           ← static JSON for mock API
│   ├── .env.example            ← VITE_API_BASE_URL=http://localhost:8080
│   ├── package.json
│   ├── vite.config.ts
│   └── README.md
└── README.md                   ← updated monorepo root README
```

**Component → API Endpoint Mapping:**

| Component | API Call | Method |
|---|---|---|
| `HistoryInputPanel` | Submit story | `POST /review-story` |
| `HistoryInputPanel` | Load mock cases | `GET /mock-cases` |
| `DecisionPanel` | Stream agent events | `GET /execution/{id}/events` (SSE) |
| `EventTimelinePanel` | Stream log events | `GET /execution/{id}/events` (SSE, same stream) |
| `FinalResultPanel` | Get final result | `GET /execution/{id}/result` |

---

### 3.4 BackendDataAgent

```json
{
  "agentName": "BackendDataAgent",
  "summary": "Decoupling the UI requires formalizing and completing the Review API contract. Several endpoints implied by the architecture must be explicitly defined, and CORS must be configured for cross-origin requests.",
  "observations": [
    "Currently defined: POST /review-story (section 8.1)",
    "Implied but not defined: GET /execution/{id}/result, GET /execution/{id}/events (SSE), GET /mock-cases, GET /mock-cases/{id}",
    "All four missing endpoints must be defined with their response shapes before or during the UI refactor",
    "CORS: when the UI runs on a different origin (e.g., localhost:5173 vs localhost:8080), the Review API must include CORS headers — currently not mentioned anywhere in the architecture document",
    "Content-Type for SSE endpoint: must return text/event-stream with proper cache-control headers"
  ],
  "risks": [
    "If the UI is built against an informal API, any backend change silently breaks the UI — schema drift risk",
    "Missing CORS configuration will cause the decoupled UI to fail immediately in browser environments"
  ],
  "recommendations": [
    "Define the complete API contract in an openapi.yaml file before starting UI development",
    "Add CORS middleware to the Review API — at minimum allow the UI dev origin in local and demo environments",
    "Define the SSE event schema explicitly: each event must have eventType, executionId, timestamp, and data fields (consistent with section 9.2 of the architecture doc)"
  ],
  "openQuestions": [
    "Should GET /execution/{id}/result return the same shape as the inline result in POST /review-story, or a different projection?"
  ],
  "confidence": 0.91
}
```

**New/Formalized API Endpoints:**

```yaml
POST /review-story
  body: ReviewRequest (section 8.1)
  response: { executionId, status }   ← change: return executionId immediately, result via polling/SSE

GET /execution/{id}/result
  response: FinalReviewResult (section 8.3)

GET /execution/{id}/events
  response: text/event-stream
  events: one per log event (section 9.2 format)

GET /mock-cases
  response: [{ id, title, description }]

GET /mock-cases/{id}
  response: { id, title, storyText, suggestedAgents }
```

---

### 3.5 UXAgent

```json
{
  "agentName": "UXAgent",
  "summary": "The decoupled UI project must implement the full 4-panel layout with explicit state management per panel. The component model proposed by SolutionArchitectAgent aligns with the UX requirements.",
  "observations": [
    "The component structure (HistoryInputPanel, DecisionPanel, EventTimelinePanel, FinalResultPanel) maps directly to the 4-panel layout from section 9.3",
    "Each component must own its loading, empty, error, and success states independently — they do not share a single global loading state",
    "The SSE stream enables progressive rendering: DecisionPanel and EventTimelinePanel update in real-time while HistoryInputPanel and FinalResultPanel are static during execution",
    "MockCaseSelector inside HistoryInputPanel should pre-populate the text area and optionally pre-select provider/model defaults from the mock case metadata"
  ],
  "risks": [
    "If the SSE connection drops mid-execution, the UI must reconnect and resume — without this, the demo experience is fragile",
    "FinalResultPanel must not render until the execution is complete — rendering partial results as 'final' misleads the user"
  ],
  "recommendations": [
    "Implement a useExecutionStream hook that manages SSE lifecycle: connect, receive events, detect completion, reconnect on drop",
    "FinalResultPanel only renders after receiving the request_completed event in the stream — until then, show a 'Waiting for final result...' state",
    "DecisionPanel renders agent cards progressively: each card appears when the agent's selected_agents event arrives and updates when agent_result arrives",
    "Include a visual indicator of SSE connection health (connected / reconnecting / disconnected)"
  ],
  "openQuestions": [
    "Should the UI support viewing previous executions (history), or is each session stateless?",
    "Should the MockCaseSelector persist the last selected case across page reloads?"
  ],
  "confidence": 0.89
}
```

---

### 3.6 MultiAgentSystemsAgent

```json
{
  "agentName": "MultiAgentSystemsAgent",
  "summary": "Extracting the UI into its own project adds a deployment unit but does not affect the agent orchestration pipeline. The main risk is operational: two services must be running for a demo to work.",
  "observations": [
    "Agent pipeline (IntakeAgent → SpecialistRouter → specialists → ReviewerAgent → TechLeadReviewAgent → SummaryAgent) is unaffected by this refactor",
    "The new deployment topology for a local demo becomes: (1) backend process, (2) UI dev server — two terminals instead of one",
    "For a cloud demo, two deployable artifacts exist: backend service + static UI assets",
    "The SSE connection from the UI to the backend creates a long-lived HTTP connection — this must be factored into backend server configuration (timeouts, max connections)"
  ],
  "risks": [
    "Demo fragility: if the backend is not running when the UI starts, the UI shows an error with no clear recovery path — must be handled gracefully",
    "SSE long-lived connections may conflict with default request timeout configurations in some server frameworks"
  ],
  "recommendations": [
    "Add a Docker Compose file (docker-compose.yml) at the monorepo root that starts both backend and UI services together for local demo",
    "Implement a health check endpoint (GET /health) on the backend — the UI can poll it on startup and show a clear 'Backend not available' message",
    "Configure SSE-specific timeout settings in the backend server (disable request timeout for /execution/{id}/events)"
  ],
  "openQuestions": [
    "Should the UI be served as static assets by the backend in the docker-compose setup, or as a separate container?"
  ],
  "confidence": 0.87
}
```

---

### 3.7 QAExpertAgent

```json
{
  "agentName": "QAExpertAgent",
  "summary": "A standalone UI project requires a dedicated test strategy covering component tests, API contract tests, E2E tests, and mock-based development tests. The test suite must be runnable without a live backend.",
  "observations": [
    "Three test layers are needed: (1) component/unit tests for each panel in isolation, (2) contract tests validating UI assumptions against the OpenAPI spec, (3) E2E tests against a running backend",
    "Component tests must use mock API responses — Vitest + MSW (Mock Service Worker) is the recommended approach for React",
    "Contract tests can be generated from the openapi.yaml — validate that UI request shapes and expected response shapes match the spec",
    "E2E tests (Playwright or Cypress) run against a live backend and validate full user flows"
  ],
  "risks": [
    "Without contract tests, API schema drift between backend and UI will only be caught in E2E tests or in production",
    "If component tests are coupled to implementation details (internal state, DOM structure), they break on refactors — must test behavior, not implementation"
  ],
  "recommendations": [
    "Implement component tests for all four panels covering: empty state, loading state, populated state, error state",
    "Use MSW to intercept API calls in tests — mock responses can be derived from the openapi.yaml fixtures",
    "Add a contract test job in the CI pipeline that validates openapi.yaml against actual backend responses",
    "E2E tests should cover: happy path (full execution), agent failure (partial result), mock case selection flow"
  ],
  "openQuestions": [
    "Should the openapi.yaml be validated on every backend PR to prevent silent contract breakage?",
    "Is Playwright acceptable for E2E, or is there a preference for Cypress?"
  ],
  "confidence": 0.9
}
```

**Test Matrix:**

| Test type | Tool | Runs without backend | Covers |
|---|---|---|---|
| Component/unit | Vitest + MSW | Yes | Each panel, all states |
| Contract | openapi-ts / zod | Yes (against spec) | Request/response shape alignment |
| Integration | Vitest + live API | No | API call logic in reviewApi.ts |
| E2E | Playwright | No | Full user flows |

---

## Stage 4 — ReviewerAgent

```json
{
  "agentName": "ReviewerAgent",
  "summary": "The specialist findings are largely consistent and complementary. Three gaps and one tension were identified.",
  "observations": [
    "GAP-01: No agent addressed the migration path — how does the current co-located UI move to the new /ui project? Is it a clean rewrite or a migration of existing code?",
    "GAP-02: SolutionArchitectAgent proposes React+TypeScript but also mentions plain HTML as a fallback. No agent resolved this decision. The choice affects the entire test strategy (QAExpertAgent assumes Vitest+MSW which requires a bundler).",
    "GAP-03: BackendDataAgent proposed changing POST /review-story to return executionId immediately (async pattern) instead of the full result. This is a breaking change from the current contract in section 8.1. No other agent acknowledged this change or its impact.",
    "TENSION-01: MultiAgentSystemsAgent recommends a Docker Compose setup, but SolutionArchitectAgent recommends a monorepo. These are compatible but the relationship between them was not made explicit."
  ],
  "risks": [
    "GAP-03 is the highest risk: if POST /review-story changes to async (returns executionId only), existing consumers of the current sync API break — this must be a deliberate versioned decision",
    "GAP-02 unresolved framework choice will cause rework if the team starts with plain HTML and then needs component tests"
  ],
  "recommendations": [
    "Resolve GAP-02 explicitly: commit to React+TypeScript or plain HTML before starting — do not leave it open",
    "Resolve GAP-03: decide whether to make POST /review-story async (recommended for SSE flow) or keep it sync and add a separate streaming endpoint — document the decision",
    "Address GAP-01: define whether the refactor is a clean-slate /ui project or a migration of existing UI code"
  ],
  "openQuestions": [
    "Is there existing UI code to migrate, or is this a greenfield /ui project?",
    "Async vs sync API: which pattern does the team commit to?"
  ],
  "confidence": 0.85
}
```

---

## Stage 5 — TechLeadReviewAgent

```json
{
  "agentName": "TechLeadReviewAgent",
  "summary": "The technical proposals are sound. Adjustments needed on three points: async API contract, framework decision, and the contracts package proposal from ArchitectureAgent.",
  "observations": [
    "ADJUST-01 (resolves GAP-03): The async pattern for POST /review-story is the correct technical decision. Returning executionId immediately and streaming events via SSE is the only pattern that supports real-time UI updates without polling. The current sync contract in section 8.1 should be superseded. Recommend versioning: keep POST /review-story as-is but change its response to include executionId, then the full result is available via GET /execution/{id}/result once the stream closes.",
    "ADJUST-02 (resolves GAP-02): React+TypeScript is the correct choice. Plain HTML does not support the component state model required by UXAgent findings (independent panel states, SSE hook). The complexity of managing 4 panel states + SSE lifecycle in plain HTML exceeds the benefit of avoiding a build tool. Vite makes setup trivial.",
    "ADJUST-03: ArchitectureAgent proposed a shared 'contracts' package for TypeScript types. For a POC, this adds overhead. Recommended alternative: generate TypeScript types from openapi.yaml using openapi-typescript — both projects generate types from the same source of truth without a third package.",
    "CONFIRM: Docker Compose + monorepo is the correct combination. The monorepo root contains docker-compose.yml; /backend and /ui are independent packages within it."
  ],
  "risks": [
    "If openapi.yaml is not created before UI development starts, teams will build against informal assumptions and accumulate drift",
    "The useExecutionStream SSE hook is the most technically complex piece of the UI — it should be implemented and tested first, before any panel UI work"
  ],
  "recommendations": [
    "First artifact to create: openapi.yaml covering all 5 endpoints defined by BackendDataAgent",
    "Second artifact: useExecutionStream hook with full lifecycle (connect, receive, reconnect, complete, error)",
    "Third: component scaffolding for the 4 panels against the mock API",
    "Fourth: wire panels to the live API and validate E2E"
  ],
  "openQuestions": [],
  "confidence": 0.93
}
```

---

## Stage 6 — SummaryAgent: Final Report

```json
{
  "agentName": "SummaryAgent",
  "summary": "The multi-agent analysis converges on a clear, actionable solution for the UI decoupling refactor. The proposal is a React+TypeScript SPA in a /ui package within a monorepo, communicating with the backend exclusively via a formalized REST+SSE API contract.",
  "observations": [
    "All specialist findings are consistent with the proposed monorepo + standalone UI project structure",
    "The ReviewerAgent identified 3 gaps; all were resolved by TechLeadReviewAgent",
    "No security concerns were raised — SecurityAgent was correctly excluded"
  ],
  "risks": [
    "API contract drift if openapi.yaml is not maintained as the source of truth",
    "SSE connection management is the most technically complex UI concern — must be addressed first"
  ],
  "recommendations": [
    "See implementation plan below"
  ],
  "openQuestions": [
    "Is there existing UI code to migrate or is this a greenfield /ui project?",
    "Async vs sync pattern for POST /review-story: decision must be made before starting backend changes"
  ],
  "confidence": 0.92
}
```

---

## Final Proposal: Decoupled UI Architecture

### Decision Record

| Decision | Choice | Rationale |
|---|---|---|
| Project structure | Monorepo (`/backend` + `/ui`) | Single clone, shared history, easier POC coordination |
| UI framework | React + TypeScript | Component model required for independent panel states; Vite makes setup trivial |
| Real-time mechanism | SSE (`GET /execution/{id}/events`) | Unidirectional, sufficient, lower complexity than WebSocket |
| API contract format | OpenAPI 3.x (`openapi.yaml`) | Source of truth for both projects; enables type generation |
| TypeScript types sharing | Generated from `openapi.yaml` via `openapi-typescript` | No third package needed; both projects stay in sync |
| Backend API pattern | Async: `POST /review-story` returns `executionId`, result via SSE + `GET /execution/{id}/result` | Enables real-time UI updates |
| Development mock | MSW (Mock Service Worker) | UI development without a running backend; reusable in component tests |
| Deployment | Docker Compose at monorepo root | Single command spins up both services for demo |

---

### Target Architecture Diagram

```
MultiAgentLab/  (monorepo)
│
├── backend/
│   ├── src/               ← unchanged agent orchestration logic
│   ├── docs/
│   │   └── openapi.yaml   ← NEW: source of truth for API contract
│   └── ...
│
├── ui/
│   ├── src/
│   │   ├── api/
│   │   │   └── reviewApi.ts          ← all fetch/SSE calls
│   │   ├── hooks/
│   │   │   └── useExecutionStream.ts ← SSE lifecycle hook
│   │   ├── components/
│   │   │   ├── HistoryInputPanel/
│   │   │   ├── DecisionPanel/
│   │   │   ├── EventTimelinePanel/
│   │   │   └── FinalResultPanel/
│   │   └── App.tsx
│   ├── mock/
│   │   └── handlers.ts     ← MSW mock handlers
│   ├── .env.example        ← VITE_API_BASE_URL=http://localhost:8080
│   ├── package.json
│   ├── vite.config.ts
│   └── README.md
│
├── docker-compose.yml      ← NEW: spins up backend + ui
└── README.md               ← updated
```

---

### Updated API Contract (supersedes section 8.1 of architecture doc)

```
POST   /review-story              → { executionId: string }   (async, fires pipeline)
GET    /execution/{id}/result     → FinalReviewResult
GET    /execution/{id}/events     → text/event-stream (log events per section 9.2)
GET    /mock-cases                → [{ id, title, description }]
GET    /mock-cases/{id}           → { id, title, storyText, suggestedAgents }
GET    /health                    → { status: "ok" }
```

---

### Acceptance Criteria (final)

- **AC-01**: `ui/` builds and runs with `npm install && npm run dev` — no backend required (uses MSW mock).
- **AC-02**: `VITE_API_BASE_URL` is the only change needed to point the UI at a different backend environment.
- **AC-03**: Adding a new panel to the UI requires changes only within `ui/src/components/` — zero backend changes.
- **AC-04**: `docker-compose up` starts both backend and UI and the full demo is accessible in a browser.
- **AC-05**: `POST /review-story` returns `executionId` immediately; the UI streams events via SSE and renders FinalResultPanel only after `request_completed` is received.
- **AC-06**: All four panels have explicit loading, empty, error, and success states implemented.
- **AC-07**: The `openapi.yaml` file is the source of truth; TypeScript types in both projects are generated from it.
- **AC-08**: Component tests for all four panels run without a live backend using MSW.

---

### Implementation Sequence (recommended)

```
Step 1: Create openapi.yaml in /backend/docs/
        → defines all 6 endpoints with request/response schemas

Step 2: Create /ui project scaffold
        → Vite + React + TypeScript
        → .env.example, README.md, package.json

Step 3: Implement useExecutionStream hook
        → SSE connect, receive events, reconnect on drop, detect completion
        → full unit tests with MSW

Step 4: Implement reviewApi.ts
        → typed wrappers for all API calls using openapi-typescript generated types

Step 5: Implement 4 panel components
        → each with loading / empty / error / success states
        → component tests with MSW

Step 6: Wire panels to live API
        → integration test against running backend

Step 7: Update docker-compose.yml
        → add ui service

Step 8: Update root README.md
        → monorepo setup instructions
```

---

### Impact on Existing Architecture Document

| Section | Change |
|---|---|
| `§3 Diagram` | Add `/ui` as a separate node; split `UI / Demo Console / API Client` into `UI Project` and `API Client (Postman/curl)` |
| `§4.1 UI / Demo Console` | Replace with reference to `/ui` standalone project and its README |
| `§8.1 ReviewRequest` | Update: `POST /review-story` now returns `{ executionId }` only — add new response schema |
| `§13 Deployment` | Add Opcion D: monorepo with Docker Compose for both services |
| `§14 Future evolution` | Remove "UI simple" references; add "UI plugin system" and "theming" as future options enabled by decoupling |
