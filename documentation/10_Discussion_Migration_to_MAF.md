# Discussion: Migration to Microsoft Agent Framework
## MultiAgentLab — Architecture Evolution

**Status:** Proposal / Open Discussion  
**Date:** 2026-05-13  
**Reference:** https://learn.microsoft.com/en-us/agent-framework/overview/?pivots=programming-language-csharp

---

## 1. What is Microsoft Agent Framework?

Microsoft Agent Framework (MAF) is the direct successor to both **Semantic Kernel** and **AutoGen**, built by the same teams. It combines:

- AutoGen's simple agent abstractions
- Semantic Kernel's enterprise features (session state, type safety, middleware, telemetry)
- New graph-based **Workflows** for explicit multi-agent orchestration

Key capabilities relevant to this solution:

| MAF Concept | Description |
|---|---|
| `AIAgent` / `ChatClientAgent` | Agent backed by any `IChatClient` (Ollama, Azure OpenAI, Anthropic…) |
| `WorkflowBuilder` + `Executor` | Graph-based orchestration with typed, validated message routing |
| `InProcessExecution.RunStreamingAsync` | Streaming execution returning `IAsyncEnumerable<WorkflowEvent>` |
| `IWorkflowContext` | Context object used inside executors to send messages and yield outputs |
| Function Tools (`AIFunctionFactory`) | Turn any C# method into a tool an agent can call |
| Agent-as-Tool (`agent.AsAIFunction()`) | Expose a specialist agent as a callable tool for an orchestrator agent |
| `OllamaChatClient` | Native Ollama integration via `Microsoft.Extensions.AI.Ollama` |
| Session / Context Providers | State management across turns and across agents (memory layer) |
| `[MessageHandler]` source gen | Compile-time-validated handler registration on `Executor` subclasses |

---

## 2. Current Architecture — What We Have

```
API Request
    │
    ▼
ReviewSupervisor.ReviewAsync()
    ├─ AgentSelectionRules.Select()       — keyword heuristics → invoked/skipped lists
    ├─ Task.WhenAll(agents in parallel)   — each agent calls IModelRouter → LLM → ParseResponse
    ├─ ConflictResolver.Detect()          — rule-based tension detection
    └─ Compose ReviewResult
            │
            ▼
    IExecutionLogger  →  JSONL file

IModelRouter
    "bedrock" → BedrockClient (custom AWS SDK wrapper)
    "ollama"  → OllamaClient (custom HttpClient wrapper)
```

### Strengths of current design

- **Clean interfaces** — `IReviewAgent`, `IModelClient`, `IExecutionLogger` are well-defined
- **Deterministic** — keyword-based selection is predictable and observable
- **Zero external dependencies** — runs fully in-process without cloud services
- **Parallel execution** — `Task.WhenAll` achieves concurrent agent dispatch

### Gaps MAF would address

- **Observability** — `WorkflowEvent` stream gives per-executor events for free; currently we build this manually with `IExecutionLogger`
- **Memory** — no cross-execution or cross-agent context today; MAF has session and context providers
- **Streaming** — frontend polls; MAF's workflow streams events natively
- **Standardized provider abstraction** — custom `IModelClient` is functionally equivalent to `IChatClient` from `Microsoft.Extensions.AI`, but not interoperable with the ecosystem
- **Tool composition** — agents cannot expose capabilities as tools today; MAF enables agent-as-tool patterns

---

## 3. How the Current Code Maps to MAF Concepts

| Current component | MAF equivalent | Migration effort |
|---|---|---|
| `ReviewSupervisor` | `WorkflowBuilder` graph + `AgentSelectionExecutor` | Medium — decompose into 3 executors |
| `Task.WhenAll` (parallel agents) | Workflow superstep fan-out via parallel edges | Low — the model handles it |
| `AgentSelectionRules.Select()` | Called from `AgentSelectionExecutor.[MessageHandler]` | None — keep as-is |
| `IReviewAgent.ExecuteAsync()` | Each agent becomes an `Executor` wrapping a `ChatClientAgent` | Low — thin wrapper |
| `ConflictResolver.Detect()` | Called from `AggregationExecutor.[MessageHandler]` | None — keep as-is |
| `IModelClient` / `ModelRouter` | `IChatClient` + `OllamaChatClient` / wrapped `BedrockClient` | Medium — BedrockClient needs `IChatClient` adapter |
| `BedrockClient` | Custom `IChatClient` wrapper (no native MAF Bedrock provider) | Medium — implement `IChatClient` adapter |
| `OllamaClient` | Replace with `OllamaChatClient` from `Microsoft.Extensions.AI.Ollama` | Low — drop-in |
| `IExecutionLogger` / `JsonlExecutionLogger` | Preserved — fed by `WorkflowEvent` handlers | None — keep as-is |
| Domain models | Preserved entirely | None |
| All API endpoints | Preserved — Angular frontend unaffected | None |
| Mock case system | Preserved | None |

---

## 4. Proposed Migration — Three Phases

### Phase 1 — Orchestration via WorkflowBuilder (Core Migration)

Replace `ReviewSupervisor.ReviewAsync()` with a MAF workflow graph.

#### Workflow graph

```
[ReviewRequest]
      │
      ▼
AgentSelectionExecutor
  - calls AgentSelectionRules.Select()
  - logs: request_received, supervisor_started, selected_agents
  - sends WorkflowDispatchInput to AgentDispatchExecutor
      │
      ▼
AgentDispatchExecutor
  - Phase A (sequential): runs ClarityAgent first
  - stores clarity findings in IWorkflowContext shared state
  - Phase B (parallel): fans out remaining selected agents
  - collects all AgentResult objects
  - logs: agent_started, agent_completed, agent_failed
  - sends WorkflowDispatchOutput to AggregationExecutor
      │
      ▼
AggregationExecutor
  - calls ConflictResolver.Detect()
  - composes ReviewResult
  - logs: conflicts_detected, final_result_generated
  - calls context.YieldOutputAsync(reviewResult)
```

#### Key implementation points

```csharp
// Executor pattern (source-gen validated)
internal sealed partial class AgentSelectionExecutor(
    AgentSelectionRules rules,
    IExecutionLogger logger) : Executor("AgentSelection")
{
    [MessageHandler]
    private async ValueTask<WorkflowDispatchInput> HandleAsync(
        ReviewRequest request, IWorkflowContext context)
    {
        var selection = rules.Select(request);
        await logger.LogAsync(LogEvents.SelectedAgents(executionId, selection));
        return new WorkflowDispatchInput(request, selection);
    }
}
```

Each `IReviewAgent` is wrapped as an `Executor` — or directly replaces `IReviewAgent` as a `ChatClientAgent` using MAF's native agent model:

```csharp
// Option A: wrap existing IReviewAgent as an executor
internal sealed partial class ClarityAgentExecutor(ClarityAgent agent) : Executor("clarity")
{
    [MessageHandler]
    private async ValueTask<AgentResult> HandleAsync(
        AgentContext context, IWorkflowContext wfCtx)
        => await agent.ExecuteAsync(context);
}

// Option B (longer term): replace with native ChatClientAgent
var clarityAgent = ollamaChatClient.AsAIAgent(
    instructions: ClarityPrompt,
    name: "clarity");
// clarity agent becomes a workflow node directly (AIAgent implements Executor)
```

**For now Option A is recommended** — it preserves all existing agent logic and lets the migration proceed incrementally.

#### Provider adaptation

```csharp
// OllamaClient → replace with Microsoft.Extensions.AI.Ollama
var ollamaChatClient = new OllamaChatClient(
    new Uri(provider.Endpoint ?? "http://localhost:11434"),
    modelId: provider.Model);

// BedrockClient → wrap as IChatClient adapter (no native MAF support)
public sealed class BedrockChatClientAdapter : IChatClient
{
    private readonly BedrockClient _inner;
    // delegate CompleteAsync → _inner.GenerateAsync()
    // map ChatMessage[] → ModelRequest, ModelResponse → ChatCompletion
}
```

**What this gives us immediately:**
- `WorkflowEvent` stream per execution — no polling
- `ExecutorCompletedEvent` per agent — real-time updates to the Angular timeline
- Type-validated message routing at build time
- Superstep-based parallel dispatch (replaces `Task.WhenAll`)

**What is preserved:**
- All domain models (`ReviewResult`, `AgentResult`, `AgentContext`, etc.)
- `AgentSelectionRules`, `ConflictResolver` — unchanged
- `IExecutionLogger` + JSONL — unchanged, fed by workflow event handlers
- All API endpoints — Angular frontend unaffected
- Mock cases — unchanged

---

### Phase 2 — Memory: Context Across Agents and Executions

This is the largest quality-of-life improvement the migration enables.

#### Memory Layer 1 — ClarityAgent findings shared with other agents

Today: `BaseReviewAgent.BuildPrompt()` manually reads `SharedFacts["clarity_findings"]` from a dictionary.  
With MAF: `IWorkflowContext` carries typed shared state between executors in the same run — no dictionary hacking.

```csharp
// In AgentDispatchExecutor, after Phase A (clarity)
var clarityResult = await clarityExecutor.ExecuteAsync(context);
await wfContext.SetStateAsync("clarity_findings", clarityResult.Issues);

// In each subsequent agent's executor
var clarityFindings = await wfContext.GetStateAsync<List<string>>("clarity_findings");
// prepend to prompt: "Clarity agent already found: ..."
```

#### Memory Layer 2 — Cross-execution memory (story history)

MAF's session/context providers can maintain state across workflow runs. This enables:

- **Historical review context**: before selecting agents, the supervisor can query past reviews of the same `storyId`
- **Incremental quality tracking**: "this story has been reviewed 3 times and still has acceptance criteria gaps"
- **Trend detection**: which agents consistently flag the same story

Implementation approach:
1. Expose `GetPastReviews(storyId)` as a **Function Tool** on a supervisor `ChatClientAgent`
2. The supervisor calls this tool before deciding agent selection
3. Past review summaries are injected into each agent's prompt as context

```csharp
[Description("Get past review summaries for a user story")]
async Task<string> GetPastReviews(
    [Description("The story ID to look up")] string storyId)
{
    var past = await _logger.GetExecutionsByStoryAsync(storyId);
    return string.Join("\n", past.Select(r => $"[{r.Date}] Status: {r.Status}, Issues: {r.Issues.Count}"));
}
```

#### Memory Layer 3 — Project context provider (future)

A context provider injected at the supervisor level that carries:
- Known project conventions (tech stack, naming rules, compliance requirements)
- Team preferences (e.g., "this team always needs Given/When/Then")
- Persistent across all executions for a given project

---

### Phase 3 — LLM-Driven Supervisor with Agent-as-Tool (Advanced / Optional)

This is the most architecturally significant change — and also the highest risk.

#### Current: keyword-based selection (deterministic)

```csharp
var isTrivial = TrivialKeywords.Any(k => text.Contains(k));
var hasTechSignals = TechnicalKeywords.Any(k => text.Contains(k));
// → invoked = ["clarity", "qa", "technical"]
```

#### Proposed: supervisor ChatClientAgent with specialists as tools

```csharp
// Each specialist agent exposed as a callable tool
var supervisorAgent = bedrockChatClient.AsAIAgent(
    instructions: SupervisorSystemPrompt,
    tools: [
        clarityAgent.AsAIFunction(),   // supervisor calls clarity when relevant
        qaAgent.AsAIFunction(),
        technicalAgent.AsAIFunction(),
        uxAgent.AsAIFunction(),
        complianceAgent.AsAIFunction(),
        AIFunctionFactory.Create(GetPastReviews),     // memory tool
        AIFunctionFactory.Create(GetProjectContext),  // project conventions tool
    ]);
```

The LLM-driven supervisor:
1. Reads the story
2. Optionally calls `GetPastReviews` to understand story history
3. Calls relevant specialist agents as tools (one or more, in its chosen order)
4. Synthesizes a final result

**Advantages:**
- Truly dynamic — the LLM can reason about which agents to call based on nuanced story content
- No keyword maintenance — the supervisor's intelligence scales with the model
- Can chain agents (e.g., call clarity first, then decide if QA is needed based on clarity's output)

**Risks:**
- **Non-deterministic** — the same story may invoke different agents on different runs
- **Demo predictability** — harder to explain "why did it skip X?" without a clear rule
- **Cost** — supervisory LLM calls are additional tokens
- **Latency** — LLM-driven tool calling is sequential by nature, not parallel

**Recommendation:** Keep Phase 3 as an **opt-in mode** (`supervisor_mode: "llm"` vs `"rules"`), not replacing the keyword approach. This makes it a learning demo for the tool-calling pattern without breaking the predictable demo flow.

---

## 5. Package Strategy

```xml
<!-- NuGet additions needed for Phase 1 -->
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.5.0" />
<PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="..." />
<!-- Microsoft.Extensions.AI is a transitive dependency of MAF -->
```

**No changes** to the Angular UI, API surface, JSONL logger, or domain models.

---

## 6. Bedrock Support Gap

MAF does **not** have a native Bedrock provider today (unlike Ollama, OpenAI, Azure OpenAI, Anthropic via Foundry). Options:

| Option | Effort | Notes |
|---|---|---|
| Keep custom `BedrockClient`, wrap as `IChatClient` | **Low** | Adapter pattern — implement `CompleteAsync` delegating to existing code |
| Use `AmazonBedrockRuntimeClient` directly as `IChatClient` | Medium | Would need full `IChatClient` implementation from scratch |
| Use Bedrock's Converse API via OpenAI-compatible endpoint | Medium | Some Bedrock models expose an OpenAI-compatible endpoint |

**Recommended:** wrap existing `BedrockClient` as a `BedrockChatClientAdapter : IChatClient`. This is a thin adapter — ~40 lines — and lets us stay on our tested Bedrock integration while participating in the MAF provider model.

---

## 7. Streaming to the Angular Frontend

Today the Angular UI polls `GET /executions/{id}` every 2 seconds. With Phase 1:

```csharp
// In ReviewEndpoints.cs (async start endpoint)
app.MapPost("/review-story/start", async (ReviewRequest req, ReviewWorkflowFactory factory, ...) =>
{
    var workflow = factory.Build();
    var executionId = GenerateId();

    _ = Task.Run(async () =>
    {
        var run = await InProcessExecution.RunStreamingAsync(workflow, req);
        await foreach (var evt in run.WatchStreamAsync())
        {
            // translate WorkflowEvent → ExecutionLogEvent → write to JSONL
            // the frontend keeps polling as today — but JSONL is written in real-time
            await logger.LogWorkflowEventAsync(executionId, evt);
        }
    });

    return Results.Accepted($"/executions/{executionId}", new { executionId });
});
```

**No change to the Angular polling pattern** in Phase 1 — the streaming improvement is on the backend (JSONL updated in real-time rather than all-at-once at completion).

**Future (Phase 2+):** Replace polling with `GET /executions/{id}/stream` using Server-Sent Events or SignalR, fed directly from `WatchStreamAsync`.

---

## 8. What NOT to Change

The following must remain intact regardless of phase:

| Component | Reason |
|---|---|
| All `GET`/`POST` API endpoints | Angular frontend depends on them |
| `ReviewResult`, `AgentResult` domain shapes | Angular models depend on the JSON contract |
| `IExecutionLogger` + JSONL format | History, compare, and mock-run features depend on it |
| Mock case system | Demo workflow depends on it |
| `AgentSelectionRules` keyword logic | Demo predictability — keep as default mode |
| `ConflictResolver` rules | Deterministic tension detection |
| Agent prompts (`.prompt.md` files) | Tuned prompt content is independent of orchestration |

---

## 9. Risks and Open Questions

### Risk 1 — MAF is still prerelease
`Microsoft.Agents.AI.Workflows` is stable (1.5.0, 882K downloads) but some features are marked experimental. The `[MessageHandler]` source generation requires `partial class` which is a minor refactor to agent wrappers.

### Risk 2 — Bedrock adapter correctness
Mapping our current `ModelRequest/ModelResponse` to `ChatMessage[]/ChatCompletion` needs careful validation, especially for models with different token formats (Nova vs Claude).

### Risk 3 — WorkflowContext state between executors
MAF's shared state API in `IWorkflowContext` is different from our `SharedFacts` dictionary. Need to confirm the API surface when context-providers docs are available (currently 404).

### Risk 4 — Phase 3 LLM supervisor is non-deterministic
If we move to LLM-driven tool calling, the demo becomes unpredictable. This must be guarded behind a mode flag.

### Open questions

1. Does `IWorkflowContext` support typed shared state between executors in the same run (for Clarity→Others sharing)?
2. Can we mix parallel edges and sequential edges in the same workflow (Phase A + Phase B dispatch)?
3. What is the exact behavior when an executor throws? Does the workflow continue or abort?
4. Is there a Bedrock `IChatClient` adapter in the AWS SDK for .NET ecosystem?

---

## 10. Proposed Next Steps

| Step | Phase | Effort | Value |
|---|---|---|---|
| 1. Create `BedrockChatClientAdapter : IChatClient` | 1 | Low | Unblocks MAF provider model |
| 2. Replace `OllamaClient` with `OllamaChatClient` | 1 | Low | Uses ecosystem-standard client |
| 3. Implement `AgentSelectionExecutor` + `AgentDispatchExecutor` + `AggregationExecutor` | 1 | Medium | Core migration |
| 4. Wire `WorkflowBuilder` in `ReviewWorkflowFactory` | 1 | Low | Ties workflow together |
| 5. Replace `ReviewSupervisor` calls in `ReviewEndpoints.cs` with `InProcessExecution` | 1 | Low | Completes Phase 1 |
| 6. Add cross-agent shared state via `IWorkflowContext` | 2 | Medium | Improves agent context quality |
| 7. Add `GetPastReviews` function tool | 2 | Medium | Story history awareness |
| 8. Prototype Phase 3 LLM supervisor behind a feature flag | 3 | High | Advanced learning demo |

---

## 11. Summary

| Dimension | Current | After Phase 1 | After Phase 2 | After Phase 3 |
|---|---|---|---|---|
| **Orchestration** | Manual `Task.WhenAll` | MAF `WorkflowBuilder` graph | Same | LLM-driven tool calls |
| **Agent selection** | Keyword heuristics | Same heuristics in executor | Same | LLM decides |
| **Provider abstraction** | Custom `IModelClient` | `IChatClient` (ecosystem standard) | Same | Same |
| **Streaming** | Polling (2s) | JSONL written in real-time | SSE / SignalR option | Same |
| **Memory** | None | Per-run `IWorkflowContext` | Cross-run + cross-agent | + project conventions |
| **Tools** | None | None | `GetPastReviews` tool | All agents as tools |
| **Frontend** | Unchanged | **Unchanged** | Streaming upgrade optional | Unchanged |
| **Demo predictability** | High | High | High | Lower (needs flag) |
