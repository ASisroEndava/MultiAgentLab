# Discussion: US Review History & Cross-Model Comparison

**Type:** Feature Proposal  
**Status:** Draft  
**Date:** 2026-05-11  
**Agents involved:** IntakeAgent, ProductAnalystAgent, ArchitectureAgent, BackendDataAgent, UXAgent, SolutionArchitectAgent, QAExpertAgent, ReviewerAgent, SummaryAgent

---

## 1. Requirement

> Keep track of all reviewed user stories and which model was used so we can compare two different results of the same US from the US history.

### Normalized Requirement

Provide per-story execution history by grouping all past reviews under a shared `storyId`, track which LLM provider and model was used per execution, and expose a comparison API + UI to diff two executions of the same story side-by-side.

### Flags

| Flag | Value |
|---|---|
| `hasUi` | ✅ new history panel + comparison view |
| `hasBackendImpact` | ✅ new endpoints + `IExecutionLogger` extension |
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

### New Domain Types

```csharp
// Lightweight per-execution metadata — returned in history list and embedded in ComparisonResult
public sealed class ExecutionSnapshot
{
    public required string ExecutionId   { get; init; }
    public required string Timestamp     { get; init; }  // from request_received event
    public required string Provider      { get; init; }
    public required string Model         { get; init; }
    public required string Status        { get; init; }
    public List<string>    InvokedAgents { get; init; } = [];
}

// Full diff between two executions of the same story
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

### New `IExecutionLogger` Methods

```csharp
// Returns all executionIds that belong to a given storyId, newest first.
Task<List<string>> GetExecutionIdsByStoryIdAsync(string storyId, CancellationToken ct = default);

// Reads only the final_result_generated event from a single execution.
Task<ReviewResult?> GetFinalResultAsync(string executionId, CancellationToken ct = default);
```

`GetFinalResultAsync` is a focused helper that avoids reading all events just to access the result.

### New API Endpoints

```
GET /stories/{storyId}/executions
    → 200  List<ExecutionSnapshot>  (ordered newest first)
    → 200  []  (empty list if storyId has no executions)

GET /executions/compare?a={executionId}&b={executionId}
    → 200  ComparisonResult
    → 400  if storyId of A ≠ storyId of B
    → 400  if only one query parameter supplied
    → 404  if either executionId is not found
```

### Existing Endpoints — No Changes

All current contracts (`GET /executions`, `GET /executions/{id}`, `GET /executions/{id}/log`, etc.) are unaffected.

---

## 5. UX Design (UXAgent)

### New Interaction Flows

**Flow 1 — Story History**  
After a review completes (or when loading an existing storyId in the input panel), a collapsible "Previous reviews for this story" section appears. Each row shows:

```
[status badge]  2025-05-11 14:32  ·  anthropic.claude-3-5-sonnet  /  Bedrock
[status badge]  2025-05-10 09:15  ·  llama3.1:8b                  /  Ollama
```

**Flow 2 — Comparison**  
User selects 2 rows via checkboxes → "Compare" button activates → comparison panel expands inline (or opens as a full-width panel below).

### Comparison Panel Layout

```
┌────────────────────────────────┬────────────────────────────────┐
│  Review A                      │  Review B                      │
│  2025-05-10 · green ✅         │  2025-05-11 · yellow ⚠️       │
│  Claude 3.5 Sonnet / Bedrock   │  llama3.1:8b / Ollama          │
├────────────────────────────────┴────────────────────────────────┤
│  🔴 Only in A                  │  🔴 Only in B                  │
│  • Missing AC for error flow   │  • No rate-limit mentioned     │
│                                │  • Auth flow not described     │
├────────────────────────────────┴────────────────────────────────┤
│  ⚪ In both                                                      │
│  • Acceptance criteria not defined for edge cases               │
│  • No rollback strategy described                               │
├─────────────────────────────────────────────────────────────────┤
│  Agents  A: clarity, qa, ux, technical                          │
│          B: clarity, qa, compliance                             │
└─────────────────────────────────────────────────────────────────┘
```

### UI States Required

| State | Behaviour |
|---|---|
| Loading history | Skeleton rows (3 placeholder items) |
| Empty history (first review) | Hide section entirely |
| Comparison loading | Spinner inside comparison panel |
| Identical results | "Both reviews produced identical findings" info banner |
| Different storyId guard | Disable "Compare" button; show tooltip "Select two reviews from the same story" |

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

## 9. Implementation Plan (SummaryAgent)

### Phase 1 — Backend (self-contained, no UI changes)

1. **Add `ExecutionSnapshot` and `ComparisonResult`** to `src/Api/Domain/`.
2. **Extend `IExecutionLogger`** with `GetExecutionIdsByStoryIdAsync` and `GetFinalResultAsync`.
3. **Update `JsonlExecutionLogger`**:
   - Add `_storyIndex: ConcurrentDictionary<string, ConcurrentBag<string>>`.
   - Populate index in `LogAsync` when `EventType == "request_received"`.
   - Rebuild index at constructor time by scanning existing JSONL files.
   - Implement the two new interface methods.
4. **Add route handlers** in `ReviewEndpoints.cs`:
   - `GET /stories/{storyId}/executions`
   - `GET /executions/compare`

### Phase 2 — Angular UI

1. **`ReviewApiService`**: add `getStoryExecutions(storyId)` and `compareExecutions(a, b)`.
2. **`StoryHistoryComponent`**: collapsible list of past executions for the active story, with status badge + model chip + checkbox selection.
3. **`ComparisonPanelComponent`**: side-by-side diff view with three sections (only in A / only in B / in both) for issues, recommendations, and agents.
4. Wire into `HistoryInputComponent` or `AppComponent` layout as a new panel.

### What Does NOT Change

- `ReviewResult` — already has all required fields.
- JSONL file format — no migration, no compatibility break.
- All existing API contracts — fully preserved.
- Angular frontend existing components — no modifications required.
