# Implementation Specification
## Multi-Agent POC for User Story and Requirements Review

## 1. Suggested stack

### Preferred option for a quick corporate POC
- **Backend**: .NET 8 Web API or Azure Functions
- **Serialization**: System.Text.Json
- **LLM providers**:
  - Amazon Bedrock
  - Ollama
- **Log persistence**:
  - JSONL files,
  - or SQLite
- **Demo UI**:
  - console,
  - minimal web UI,
  - or Postman

### Alternative
- Python with FastAPI if you want to iterate prompts faster.

This document uses **C#/.NET** oriented examples.

---

## 2. Suggested project structure

```text
/src
  /Api
    Program.cs
    ReviewEndpoints.cs
  /Application
    /Supervisor
      ReviewSupervisor.cs
      AgentSelectionRules.cs
      ConflictResolver.cs
    /Agents
      ClarityAgent.cs
      QaAgent.cs
      TechnicalAgent.cs
      UxAgent.cs
      ComplianceAgent.cs
    /Prompts
      clarity.prompt.md
      qa.prompt.md
      technical.prompt.md
      ux.prompt.md
      compliance.prompt.md
  /Domain
    ReviewRequest.cs
    ReviewResult.cs
    AgentResult.cs
    ExecutionLogEvent.cs
    ModelProviderOptions.cs
  /Infrastructure
    /LLM
      IModelClient.cs
      ModelRouter.cs
      BedrockClient.cs
      OllamaClient.cs
    /Logging
      IExecutionLogger.cs
      JsonlExecutionLogger.cs
    /Mocks
      MockCaseLoader.cs
  /Tests
    SupervisorTests.cs
    MockCaseTests.cs
```

---

## 3. Input and output contracts

## 3.1 Main request

```json
{
  "storyId": "story-003",
  "title": "Notification retries",
  "storyText": "As a system, I need to automatically retry sending failed notifications up to 3 times before marking them as a definitive error.",
  "provider": {
    "type": "ollama",
    "model": "llama3.1",
    "endpoint": "http://localhost:11434",
    "temperature": 0.1
  },
  "logging": {
    "level": "full",
    "includePrompts": true,
    "includeResponses": true
  }
}
```

## 3.2 Final response

```json
{
  "executionId": "exec-003",
  "status": "yellow",
  "summary": "The requirement has clear intent but operational definitions are missing.",
  "provider": "ollama",
  "model": "llama3.1",
  "invokedAgents": ["clarity", "qa", "technical"],
  "skippedAgents": [
    {
      "agent": "ux",
      "reason": "Backend-oriented story with no visible user interaction"
    },
    {
      "agent": "compliance",
      "reason": "No sensitive data or regulatory traceability detected"
    }
  ],
  "issues": [
    "Retry interval is not defined",
    "Retryable errors are not listed",
    "Audit strategy is not defined"
  ],
  "recommendations": [
    "Define retry policy",
    "Specify transient vs permanent errors",
    "Add metrics and traceability"
  ],
  "conflicts": []
}
```

---

## 4. Common agent contract

All agents must comply with a common interface.

```csharp
public interface IReviewAgent
{
    string Name { get; }

    Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken = default);
}
```

### AgentContext

```csharp
public sealed class AgentContext
{
    public required string ExecutionId { get; init; }
    public required string StoryId { get; init; }
    public required string Title { get; init; }
    public required string StoryText { get; init; }
    public required ProviderSelection Provider { get; init; }
    public required LoggingOptions Logging { get; init; }
    public Dictionary<string, object> SharedFacts { get; init; } = new();
}
```

### AgentResult

```csharp
public sealed class AgentResult
{
    public required string Agent { get; init; }
    public required string Status { get; init; }
    public double Score { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public List<string> Questions { get; init; } = new();
    public string? RawSummary { get; init; }
}
```

---

## 5. LLM provider abstraction

## 5.1 Common interface

```csharp
public interface IModelClient
{
    Task<ModelResponse> GenerateAsync(
        ModelRequest request,
        CancellationToken cancellationToken = default);
}
```

## 5.2 Provider router

```csharp
public interface IModelRouter
{
    IModelClient Resolve(ProviderSelection providerSelection);
}
```

## 5.3 ProviderSelection

```csharp
public sealed class ProviderSelection
{
    public required string Type { get; init; }   // bedrock | ollama
    public required string Model { get; init; }
    public string? Region { get; init; }         // Bedrock
    public string? Endpoint { get; init; }       // Ollama
    public double Temperature { get; init; } = 0.2;
    public int? MaxTokens { get; init; }
}
```

### Key rule
The supervisor and agent logic **must not know specific details of Bedrock or Ollama** beyond the received configuration.

---

## 6. Supervisor orchestration

## 6.1 Responsibilities
- analyze the text;
- select agents;
- execute agents in reasonable order;
- capture results;
- resolve conflicts;
- produce final output.

## 6.2 Main flow pseudocode

```csharp
public async Task<ReviewResult> ReviewAsync(ReviewRequest request)
{
    var executionId = _idGenerator.NewExecutionId();
    await _logger.LogAsync(LogEvents.RequestReceived(executionId, request));

    var selectedAgents = _agentSelectionRules.Select(request);
    await _logger.LogAsync(LogEvents.SelectedAgents(executionId, selectedAgents));

    var context = AgentContextFactory.Create(executionId, request);

    var results = new List<AgentResult>();

    foreach (var agent in selectedAgents.Invoked)
    {
        try
        {
            await _logger.LogAsync(LogEvents.AgentStarted(executionId, agent.Name));
            var result = await agent.ExecuteAsync(context);
            results.Add(result);
            await _logger.LogAsync(LogEvents.AgentCompleted(executionId, result));
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(LogEvents.AgentFailed(executionId, agent.Name, ex.Message));
        }
    }

    var conflicts = _conflictResolver.Detect(results);
    if (conflicts.Count > 0)
    {
        await _logger.LogAsync(LogEvents.ConflictsDetected(executionId, conflicts));
    }

    var final = _supervisorComposer.Compose(
        executionId,
        request,
        selectedAgents,
        results,
        conflicts);

    await _logger.LogAsync(LogEvents.FinalResultGenerated(executionId, final));
    return final;
}
```

---

## 7. Agent selection rules

## 7.1 Base rule
Always try to use **clarity**, unless the case is a pre-classified trivial change.

## 7.2 Minimum suggested heuristics

### Invoke QA if:
- there are validation rules;
- there are error flows;
- there are expected states;
- the story requires formal acceptance.

### Invoke technical if:
- there is backend;
- there is integration;
- there are retries;
- there are asynchronous processes;
- there is persistence or consistency.

### Invoke UX if:
- there is a screen or form;
- there are messages to the user;
- there are visible UI actions;
- there is copy, buttons, feedback, or navigation.

### Invoke compliance if:
- there is personal data;
- there is download/export;
- there is audit;
- there is authorization;
- there is regulatory risk.

---

## 8. Conflict resolution rules

The supervisor must be able to arbitrate when two agents recommend different things.

### Suggested rules

1. **Compliance takes priority over UX**  
   If UX proposes a simplification that compromises security or privacy, compliance wins.

2. **Technical feasibility conditions UX**  
   If UX suggests a behavior that is not viable with the current scope, the supervisor marks it as a future improvement or conditions it.

3. **Lack of testability degrades the global status**  
   If QA detects severe absence of acceptance criteria, the final status should not be green.

4. **Functional ambiguity spreads to others**  
   If clarity finds central gaps, the supervisor must reflect this even if other agents could give input.

### Example
Story: "edit shipping address from profile".

- UX: "immediate inline editing".
- Technical: "changing the address may impact orders already prepared".

**Supervisor resolution:**
- allow editing only for non-dispatched orders;
- show restriction in UI;
- mark alternative behavior for already processed orders.

---

## 9. Prompt design

Each agent must have:

- fixed role;
- mandatory JSON output;
- clear limits;
- analytical tone, not creative;
- focus on useful observations.

## 9.1 Clarity agent base prompt

```text
You are a functional reviewer specialized in user stories.
Analyze the story and detect ambiguities, missing rules, undefined scenarios, and necessary questions.
Respond exclusively in JSON with this format:
{
  "issues": [],
  "recommendations": [],
  "questions": [],
  "rawSummary": ""
}
```

## 9.2 QA agent base prompt

```text
You are a QA analyst specialized in testability.
Review whether the story allows building acceptance criteria and test cases.
Detect missing validations, edge scenarios, and undefined error states.
Respond only in JSON.
```

## 9.3 Technical agent base prompt

```text
You are a software architect/engineer specialized in technical impact.
Detect technical risks, dependencies, asynchrony, consistency, duplicates, observability, and complexity.
Respond only in JSON.
```

## 9.4 UX agent base prompt

```text
You are a UX specialist.
Review interaction clarity, user feedback, interface consistency, messages, and friction points.
Respond only in JSON.
```

## 9.5 Compliance agent base prompt

```text
You are a security, privacy, and compliance specialist.
Detect data exposure, authorization issues, missing traceability, or regulatory risks.
Respond only in JSON.
```

---

## 10. Functional and technical logging

## 10.1 Logging interface

```csharp
public interface IExecutionLogger
{
    Task LogAsync(ExecutionLogEvent logEvent, CancellationToken cancellationToken = default);
}
```

## 10.2 Log event

```csharp
public sealed class ExecutionLogEvent
{
    public required string ExecutionId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string EventType { get; init; }
    public required object Data { get; init; }
}
```

## 10.3 JSONL example

```json
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:00Z","eventType":"request_received","data":{"storyId":"story-004","provider":"bedrock","model":"example-model"}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:01Z","eventType":"selected_agents","data":{"invoked":["clarity","qa","technical","compliance"],"skipped":[{"agent":"ux","reason":"The interface is not the main focus of the requirement"}]}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:03Z","eventType":"agent_completed","data":{"agent":"clarity","issues":["File format is not defined"]}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:04Z","eventType":"agent_completed","data":{"agent":"compliance","issues":["Holder identity must be validated","The file must expire"]}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:05Z","eventType":"final_result_generated","data":{"status":"red"}}
```

## 10.4 What to show in the demo
- list of invoked agents;
- skip reasons;
- findings per agent;
- supervisor resolution;
- timeline.

---

## 11. Suggested endpoints

## 11.1 POST /review-story
Executes a real review on the received text.

## 11.2 GET /executions/{executionId}
Returns the final result.

## 11.3 GET /executions/{executionId}/log
Returns the complete event log.

## 11.4 GET /mock-cases
Lists available demo cases.

## 11.5 POST /mock-cases/{caseId}/run
Executes a mock case with the specified provider.

---

## 12. Required mock cases for demo

It is recommended to include at least the following:

1. **Label change**
2. **Reset password**
3. **Automatic retries**
4. **Personal data download**
5. **Edit shipping address**

The complete details can be found in `04_Mock_Cases_and_Demo_Script.md`.

---

## 13. Tests

## 13.1 Unit tests
- agent selection;
- conflict resolution;
- result serialization;
- Bedrock and Ollama normalization.

## 13.2 Integration tests
- full run with Bedrock;
- full run with Ollama;
- log generation;
- mock case execution.

## 13.3 Minimum acceptance criteria
- all mock cases must run;
- it must be clearly visible which agents were not used;
- a traceable `executionId` must exist;
- the log must be queryable;
- the provider must be interchangeable via configuration.

---

## 14. Suggested implementation plan (4 to 5 days)

### Day 1
- create base structure;
- define contracts;
- implement model router;
- connect one provider first.

### Day 2
- implement supervisor;
- implement selection rules;
- implement 2 agents:
  - clarity,
  - QA.

### Day 3
- implement technical, UX, and compliance;
- add JSONL logging;
- add log query endpoint.

### Day 4
- create mock cases;
- adjust prompts;
- validate Bedrock and Ollama;
- prepare demo.

### Day 5 (optional)
- simple UI to visualize story, agents, log, and result;
- improve output format for presentation.

---

## 15. Final implementation recommendation

To make the demo strong without making it too large:

- keep the agent scope at 5;
- support one provider per execution;
- use JSONL for logging;
- prepare mock cases with clear expectations;
- visually show the supervisor's decisions;
- highlight that not all agents run every time.

If done well, this POC allows teaching in a few minutes:
- agent specialization;
- real coordination;
- Bedrock/Ollama abstraction;
- conversation and decision traceability;
- practical value for early story review.
