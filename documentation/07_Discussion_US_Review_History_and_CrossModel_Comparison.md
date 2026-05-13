# Discussion: US Review History & Cross-Model Comparison

**Type:** Feature Implementation  
**Status:** ✅ Implemented  
**Proposal Date:** 2026-05-11  
**Implementation Date:** 2026-05-12  
**Branch:** `feature/us-review-history-comparison`  
**Agents involved:** IntakeAgent, ProductAnalystAgent, ArchitectureAgent, BackendDataAgent, UXAgent, SolutionArchitectAgent, QAExpertAgent, ReviewerAgent, SummaryAgent

---

## 1. Requirement

> Keep track of all reviewed user stories and which model was used so we can compare two different results of the same US from the US history.

### Normalized Requirement

Provide per-story execution history by grouping all past reviews under a shared `storyId`, track which LLM provider and model was used per execution, and expose a comparison API + UI to diff two executions of the same story side-by-side — using **semantic LLM analysis** to detect similar issues and recommendations even when phrased differently.

### Flags

| Flag | Value |
|---|---|
| `hasUi` | ✅ two new tabs: History + Compare User Stories |
| `hasBackendImpact` | ✅ 2 new endpoints + `IExecutionLogger` extension |
| `hasIntegration` | ❌ internal data only |
| `hasSensitiveData` | ❌ |
| `hasMultiAgentImpact` | ✅ model metadata originates in the multi-agent execution |

### Key Assumption

"User story identity" = `storyId` field already present in `ReviewRequest` and persisted in the `request_received` log event. No change to the domain model is required.

---

## 2. Business Analysis (ProductAnalystAgent)

### Business Goals

- **Auditability** — allow teams to see how a story has been reviewed over time.
- **Model calibration** — compare Claude vs Llama vs Bedrock output quality on the same input.
- **Story evolution** — track whether re-reviews improve or worsen the result.

### Business Rules

1. A story is identified by `storyId`; reviews are grouped by it.
2. A comparison requires exactly **2 `executionId`s from the same `storyId`**.
3. Diff dimensions: status change, issues (added/removed/shared), recommendations (added/removed/shared), models used, agents invoked.
4. History must survive server restarts (JSONL already satisfies this).
5. A comparison between executions from different stories returns `400 Bad Request`.

### Acceptance Criteria

- `GET /stories/{storyId}/executions` returns an ordered list of all executions for that story, including timestamp, status, provider, and model.
- `GET /executions/compare?a={id}&b={id}` returns a structured diff between the two reviews.
- Comparing executions from different stories returns `400 Bad Request` with a descriptive message.
- The UI surfaces the model name and provider alongside each history entry.
- Results are ordered newest-first.

---

## 3. Architecture Impact (ArchitectureAgent)

### Key Finding: All data already exists

`ReviewResult` already carries `Provider`, `Model`, `ExecutionId`, and `Status`. The `request_received` event stores `storyId`. No new data needs to be produced or re-stored.

### Impacted Components

| Component | Change |
|---|---|
| `IExecutionLogger` | Add `GetExecutionIdsByStoryIdAsync` + `GetFinalResultAsync` |
| `JsonlExecutionLogger` | Implement new methods + maintain `_storyIndex` in-memory |
| `ReviewEndpoints.cs` | 2 new route handlers |
| Domain | New `ComparisonResult` + `ExecutionSnapshot` records |
| Angular frontend | New history panel + comparison view components |

### Architecture Risk — O(n) Scan

Without an index, `GetExecutionIdsByStoryIdAsync` would require reading the first event of every JSONL file per request, which degrades linearly with history size.

**Recommended fix:** Add an in-memory story index to `JsonlExecutionLogger`:

```csharp
// Populated on every LogAsync(request_received) call; rebuilt at startup.
private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _storyIndex = new();
```

This keeps JSONL as the single source of truth while making story lookups O(1).

---

## 4. Data Model & API Contract (BackendDataAgent)

### Domain Types — Original Proposal

```csharp
// Lightweight per-execution metadata — returned in history list and embedded in ComparisonResult
public sealed class ExecutionSnapshot
{
    public required string ExecutionId   { get; init; }
    public required string Timestamp     { get; init; }  // from request_received event
    public required string StoryId       { get; init; }
    public required string Title         { get; init; }
    public required string Provider      { get; init; }
    public required string Model         { get; init; }
    public required string Status        { get; init; }
    public List<string>    InvokedAgents { get; init; } = [];
}

// String-exact diff between two executions of the same story
public sealed class ComparisonResult
{
    public required string            StoryId                    { get; init; }
    public required string            Title                      { get; init; }
    public required ExecutionSnapshot SnapshotA                  { get; init; }
    public required ExecutionSnapshot SnapshotB                  { get; init; }
    public List<string>               IssuesOnlyInA              { get; init; } = [];
    public List<string>               IssuesOnlyInB              { get; init; } = [];
    public List<string>               IssuesInBoth               { get; init; } = [];
    public List<string>               RecommendationsOnlyInA     { get; init; } = [];
    public List<string>               RecommendationsOnlyInB     { get; init; } = [];
    public List<string>               RecommendationsInBoth      { get; init; } = [];
    public List<string>               AgentsOnlyInA              { get; init; } = [];
    public List<string>               AgentsOnlyInB              { get; init; } = [];
    public List<string>               AgentsInBoth               { get; init; } = [];
}
```

### ✅ Additional Domain Types — Semantic Comparison (Implemented)

The semantic comparison feature introduced three new types in `src/Api/Domain/SemanticPair.cs`:

```csharp
// A matched pair of semantically equivalent items from two reviews
public sealed class SemanticPair
{
    [JsonPropertyName("a")] public required string A { get; init; }
    [JsonPropertyName("b")] public required string B { get; init; }
}

// Per-dimension diff: semantically-similar pairs + unique items on each side
public sealed class SemanticDiff
{
    [JsonPropertyName("similar")]  public List<SemanticPair> Similar  { get; init; } = [];
    [JsonPropertyName("onlyInA")]  public List<string>       OnlyInA  { get; init; } = [];
    [JsonPropertyName("onlyInB")]  public List<string>       OnlyInB  { get; init; } = [];
}

// Full semantic comparison result
public sealed class SemanticComparisonResult
{
    public required string            StoryId         { get; init; }
    public required string            Title           { get; init; }
    public required ExecutionSnapshot SnapshotA       { get; init; }
    public required ExecutionSnapshot SnapshotB       { get; init; }
    public required SemanticDiff      Issues          { get; init; }
    public required SemanticDiff      Recommendations { get; init; }
    public List<string>               AgentsOnlyInA   { get; init; } = [];
    public List<string>               AgentsOnlyInB   { get; init; } = [];
    public List<string>               AgentsInBoth    { get; init; } = [];
}

// Request body for the semantic comparison endpoint
public sealed class SemanticCompareRequest
{
    [JsonPropertyName("a")]        public required string            A        { get; init; }
    [JsonPropertyName("b")]        public required string            B        { get; init; }
    [JsonPropertyName("provider")] public required ProviderSelection Provider { get; init; }
}
```

### New `IExecutionLogger` Methods

```csharp
// Returns all executionIds that belong to a given storyId, newest first.
Task<List<string>> GetExecutionIdsByStoryIdAsync(string storyId, CancellationToken ct = default);

// Reads only the final_result_generated event from a single execution.
Task<ReviewResult?> GetFinalResultAsync(string executionId, CancellationToken ct = default);
```

`GetFinalResultAsync` is a focused helper that avoids reading all events just to access the result.

### New API Endpoints

#### Proposed

```
GET /stories/{storyId}/executions
    → 200  List<ExecutionSnapshot>  (ordered newest first)
    → 200  []  (empty list if storyId has no executions)

GET /executions/compare?a={executionId}&b={executionId}
    → 200  ComparisonResult  (string-exact diff)
    → 400  if storyId of A ≠ storyId of B
    → 400  if only one query parameter supplied
    → 404  if either executionId is not found
```

#### ✅ Actually Implemented

> **Note:** `GET /stories/{storyId}/executions` was not added. Instead, the existing `GET /executions` endpoint (returning all executions as `ExecutionSummary[]`) is used by the History tab, with story grouping performed client-side. This avoids adding a new endpoint while achieving the same result.

```
GET /executions/{executionId}/result
    → 200  ReviewResult    (parsed full result for a single execution)
    → 404  if execution has no final_result_generated event

POST /executions/compare/semantic
    Body: { "a": string, "b": string, "provider": ProviderSelection }
    → 200  SemanticComparisonResult
    → 400  if a or b is missing
    → 404  if either execution is not found
    → 500  ProblemDetails { title, detail }  if the LLM call fails

GET /executions/compare?a={id}&b={id}          ← preserved (string-exact diff)
    → 200  ComparisonResult
    → 400  if storyIds differ or parameters missing
    → 404  if either execution is not found
```

### Existing Endpoints — No Changes

All existing contracts (`GET /executions`, `GET /executions/{id}`, `GET /executions/{id}/log`, `GET /executions/{id}/log/text`) are fully preserved.

---

## 5. UX Design (UXAgent)

### Proposed Interaction Flows

**Flow 1 — Story History**  
After a review completes (or when loading an existing storyId in the input panel), a collapsible "Previous reviews for this story" section appears. Each row shows:

```
[status badge]  2025-05-11 14:32  ·  anthropic.claude-3-5-sonnet  /  Bedrock
[status badge]  2025-05-10 09:15  ·  llama3.1:8b                  /  Ollama
```

**Flow 2 — Comparison**  
User selects 2 rows via checkboxes → "Compare" button activates → comparison panel expands inline (or opens as a full-width panel below).

### ✅ Actual Implementation — Two Dedicated Tabs

The UI was implemented as two separate top-level tabs rather than inline sections within the review tab. This keeps the review workflow uncluttered and makes the history and comparison features first-class surfaces.

#### Tab: History

**Component:** `ExecutionHistoryComponent` (`ui/src/app/features/execution-history/`)  
**Data source:** `GET /executions` → client-side group by `storyId`

- Shows all executions grouped by story in collapsible `mat-accordion` panels
- Each execution row: status badge, ISO timestamp, provider/model chip, duration (ms), execution ID (truncated)
- Clicking a row fetches `GET /executions/{id}/result` and **expands the full result inline**:
  - Provider, model, invoked agent count
  - Summary text
  - Issues list
  - Recommendations list
  - Per-agent result cards (agent name, status, issues, recommendations)
- Loading and error states handled per-row

#### Tab: Compare User Stories

**Component:** `StoryHistoryComponent` + `ComparisonPanelComponent`  
**Data source:** `GET /executions` → `POST /executions/compare/semantic`

- Same execution list (grouped by story, same row format as History)
- User selects exactly **2 executions** via checkboxes (third checkbox disabled while 2 selected)
- When 2 are selected, a **LLM provider/model selector** appears above the Compare button:
  - Provider dropdown (Ollama / Bedrock — Bedrock disabled with tooltip if AWS not configured)
  - Model dropdown (Ollama models fetched live from the Ollama API; Bedrock models hardcoded list)
  - Ollama endpoint field (shown only for Ollama, triggers model re-fetch on blur)
- **"Semantic Compare"** button triggers `POST /executions/compare/semantic`
- Result renders as `ComparisonPanelComponent` below the list

### ✅ Actual Comparison Panel Layout (Semantic)

```
┌─────────────────────────────────────────────────────────────────┐
│  Semantic Comparison — <story title>                 <storyId>  │
├────────────────────────────────┬────────────────────────────────┤
│  Review A                      │  Review B                      │
│  2025-05-10 · green ✅         │  2025-05-11 · yellow ⚠️       │
│  Claude 3.5 Haiku / Bedrock    │  qwen2.5:3b / Ollama           │
│  exec-abc123…                  │  exec-def456…                  │
├─────────────────────────────────────────────────────────────────┤
│  ⚠️ ISSUES                                                      │
│  ┌─ Semantically similar (N) ──────────────────────────────┐   │
│  │  Missing AC for error flow  ⟺  No error handling AC     │   │
│  │  No rollback described      ⟺  Rollback not mentioned   │   │
│  └─────────────────────────────────────────────────────────┘   │
│  Only in A (N)                  Only in B (N)                   │
│  • Auth flow not described      • No rate-limit mentioned       │
├─────────────────────────────────────────────────────────────────┤
│  💡 RECOMMENDATIONS  (same layout as Issues)                    │
├─────────────────────────────────────────────────────────────────┤
│  🤖 AGENTS                                                      │
│  Only in A    │  In both              │  Only in B              │
│  ux           │  clarity · qa · tech  │  compliance             │
└─────────────────────────────────────────────────────────────────┘
```

Semantically similar pairs are rendered as orange ↔ blue cards with a purple arrow, visually distinct from the unique-to-each lists.

### UI States Implemented

| State | Behaviour |
|---|---|
| Loading executions | Spinner + "Loading history…" |
| Empty history | Centered icon + "No executions yet" |
| Backend unreachable | Error message with reload button |
| 1 execution selected | "Select 1 more to compare" hint |
| 2 executions selected | Provider/model selector + Semantic Compare button appear |
| Comparing | Button shows spinner, disabled |
| Comparison error | Red banner with actual error message from backend |
| Identical results (semantically) | Green "semantically equivalent" banner |

---

## 6. Target Architecture (SolutionArchitectAgent)

### Decision: Extend JSONL Infrastructure — No New Storage

The JSONL logger is the single source of truth. The story index is a derived, in-memory projection rebuilt at startup from existing files.

### Component Diagram

```
Startup
  └─ JsonlExecutionLogger
       ├─ Directory.GetFiles("logs/*.jsonl")
       └─ For each file: read first line (request_received)
            └─ Extract storyId → populate _storyIndex[storyId] = executionId

GET /stories/{storyId}/executions
  ├─ _storyIndex[storyId] → List<executionId>            [O(1)]
  ├─ Parallel: GetFinalResultAsync(id) for each          [O(k) I/O]
  └─ Map → ExecutionSnapshot[], order by timestamp desc

GET /executions/compare?a=&b=
  ├─ Parallel: GetFinalResultAsync(a), GetFinalResultAsync(b)
  ├─ Guard: resultA.StoryId == resultB.StoryId
  └─ Set diff on Issues, Recommendations, AgentResults → ComparisonResult
```

### Storage Trade-off Analysis

| Approach | Pro | Con |
|---|---|---|
| **JSONL + in-memory index (proposed)** | Zero new dependencies, no migration, consistent with existing pattern | Index lost on crash — rebuilt at startup (acceptable for POC) |
| SQLite / EF Core | ACID, persistent, queryable | Adds dependency, migration complexity, overkill for lab |
| Separate `story-index.json` file | Persistent index | Consistency risk between index file and JSONL; locking complexity |

**Verdict:** JSONL + in-memory index is sufficient for the current POC/lab scope. Graduate to SQLite if query latency becomes measurable or the log directory exceeds ~10k files.

---

## 7. Testing Strategy (QAExpertAgent)

### Positive Test Cases

| Scenario | Expected |
|---|---|
| Story with 3 executions across 2 providers | All 3 returned, ordered newest first |
| Compare 2 executions of same story | Correct items in each diff bucket |
| Compare execution with itself | All issues in `IssuesInBoth`, exclusive lists empty |
| Execution with no issues | All diff lists empty — no error |

### Negative Test Cases

| Scenario | Expected |
|---|---|
| `GET /stories/nonexistent/executions` | `200 []` (empty list; story may not exist yet) |
| `GET /compare?a=exec-A&b=exec-from-other-story` | `400` with clear message |
| `GET /compare?a=exec-missing&b=exec-valid` | `404` |
| `GET /compare` with only `a` parameter | `400` |

### Edge Cases

| Scenario | Expected |
|---|---|
| Aborted execution (no `final_result_generated` event) | Excluded from history list gracefully |
| Malformed JSONL file at startup | Logged warning + skipped; does not crash startup scan |
| Concurrent reviews of same story | Both correctly indexed |
| `parse_error` status execution | Still comparable; error surfaced in agent results diff |

### Regression Risks

- `GET /executions` (existing list) must remain unaffected.
- `GET /executions/{id}/log` must still work after `IExecutionLogger` changes.
- Adding `_storyIndex` to `JsonlExecutionLogger` must not affect `LogAsync` latency.

---

## 8. ReviewerAgent — Gaps & Consistency Notes

1. **Malformed JSONL at startup:** the rebuild scan must wrap each file read in a try/catch and skip invalid files — not crash the whole startup.

2. **Aborted executions:** `GetFinalResultAsync` returns `null` for executions without a `final_result_generated` event. The history endpoint must filter these out rather than surface a null entry.

3. **Timestamp source is ambiguous:** `ExecutionSnapshot.Timestamp` should always come from the `request_received` event (start time), not from `final_result_generated` (end time), so that ordering is deterministic.

4. **Same-story guard:** the comparison endpoint must compare the canonical `storyId` field (from `ReviewResult`), not story content or title, to avoid false positives.

5. **Empty storyId:** `ReviewRequest.StoryId` can technically be empty. If so, history grouping is meaningless — consider a validation rule in `AgentSelectionExecutor`.

---

## 9. Implementation Record

### What Was Proposed vs What Was Built

| Area | Proposed | Implemented |
|---|---|---|
| History grouping | `GET /stories/{storyId}/executions` (server-side) | `GET /executions` + client-side grouping |
| Comparison type | String-exact set diff | **Semantic LLM comparison** via `POST /executions/compare/semantic` |
| Comparison provider | N/A (server-side only) | User-selectable LLM (Ollama or Bedrock) per comparison |
| UI placement | Inline section in review tab | **Two dedicated tabs**: History + Compare User Stories |
| Execution detail | Metadata only in list | Full `ReviewResult` expandable inline in History tab |
| Error reporting | Generic | Actual backend error message surfaced via `ProblemDetails` |

---

### Phase 1 — Backend

**`src/Api/Domain/`**
- `ExecutionSnapshot.cs` — lightweight metadata record embedded in `ComparisonResult` and `SemanticComparisonResult`
- `ComparisonResult.cs` — string-exact diff result (issues/recs in three buckets + agents)
- `SemanticPair.cs` — contains `SemanticPair`, `SemanticDiff`, `SemanticComparisonResult`, `SemanticCompareRequest`

**`src/Api/Infrastructure/Logging/`**
- `IExecutionLogger` — extended with `GetExecutionIdsByStoryIdAsync` and `GetFinalResultAsync`
- `JsonlExecutionLogger` — implemented both new methods; `GetFinalResultAsync` reads the `final_result_generated` event from the full log

**`src/Api/ReviewEndpoints.cs`**
- `GET /executions/compare` — string-exact `ComparisonResult` (existing, preserved)
- `GET /executions/{executionId}/result` — returns parsed `ReviewResult` for a single execution
- `POST /executions/compare/semantic` — semantic LLM comparison (see below)

#### Semantic Comparison Logic (`POST /executions/compare/semantic`)

1. Loads both `ReviewResult`s from the logger
2. Builds execution snapshots from the `request_received` and `final_result_generated` log events
3. Constructs a structured prompt using `$$"""..."""` (C# raw string interpolation) listing all issues and recommendations from both reviews
4. Calls the LLM (any configured provider) via `IModelRouter.Resolve(provider)`
5. Sets explicit `MaxTokens = 4096` to prevent truncation of the JSON response
6. Parses the LLM response: extracts the first `{...}` block, deserializes with lenient options (`AllowTrailingCommas`, `SkipComments`)
7. **Fallback**: if JSON parsing fails for any reason, all items are returned as `OnlyInA` / `OnlyInB` (no silent data loss)
8. Agents diff uses exact string matching (model/token names are deterministic)
9. **Exception handling**: LLM failures return `ProblemDetails { title, detail }` with the actual exception message; `OperationCanceledException` returns `499`

#### LLM Prompt Structure

```
You are a semantic similarity analyzer for user story reviews.
Two reviews were performed independently on the same user story.
Identify which items express the same quality concern (even in different words),
and which are unique to each review.

ISSUES from Review A: ...
ISSUES from Review B: ...
RECOMMENDATIONS from Review A: ...
RECOMMENDATIONS from Review B: ...

IMPORTANT: Output ONLY a valid JSON object — no markdown fences, no comments.
{
  "issues": {
    "similar": [{"a": "<text from A>", "b": "<text from B>"}, ...],
    "onlyInA": [...],
    "onlyInB": [...]
  },
  "recommendations": { ... }
}
```

---

### Phase 2 — Angular UI

**`ui/src/app/core/models/api.models.ts`**
- Added `SemanticPair`, `SemanticDiff`, `SemanticComparisonResult`, `SemanticCompareRequest` TypeScript interfaces

**`ui/src/app/core/services/review-api.service.ts`**
- `getExecutionResult(id)` → `GET /executions/{id}/result`
- `semanticCompareExecutions(req)` → `POST /executions/compare/semantic`

**`ui/src/app/features/execution-history/`** *(new component)*
- `ExecutionHistoryComponent` — History tab, groups all executions by `storyId`, expands full result on click via `getExecutionResult()`

**`ui/src/app/features/story-history/story-history.component.ts`**
- Switched comparison call from `compareExecutions()` to `semanticCompareExecutions()`
- Added `bedrockModels`, `ollamaModels` (live-fetched), `providersStatus`, `onProviderChange()`, `fetchOllamaModels()` — identical pattern to `HistoryInputComponent`
- Error handler reads `detail ?? message ?? title` from `ProblemDetails`

**`ui/src/app/features/comparison-panel/comparison-panel.component.ts/.html/.scss`**
- Input type changed from `ComparisonResult` to `SemanticComparisonResult`
- Template rewritten: semantic similar pairs rendered as a purple-bordered block above the two-column unique-items diff
- Agents section retains its own 3-column exact-match layout

**`ui/src/app/app.ts` / `app.html`**
- Added **History** tab (index 2) with `<app-execution-history />`
- Renamed **Compare User Stories** tab (index 3, previously "History & Compare") with icon `compare_arrows`

### What Did NOT Change

- `ReviewResult` — no modifications
- JSONL file format — no migration, no compatibility break
- All pre-existing API contracts — fully preserved
- `HistoryInputComponent`, `DecisionPanelComponent`, `EventTimelineComponent`, `FinalResultComponent` — no modifications
