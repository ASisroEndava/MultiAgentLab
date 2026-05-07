# Memory - Project Decisions and Progress

## Decisions Made

### 2026-04-20 - Implementation start

- **Stack chosen**: .NET 9 Web API with System.Text.Json (SDK installed: 9.0.312)
- **LLM Providers**: Amazon Bedrock (AWSSDK.BedrockRuntime 3.7.414.0) + local Ollama
- **Log persistence**: JSONL files (simplest option for POC)
- **Structure**: As per specification in doc 03 — `/src` with Api, Application, Domain, Infrastructure, Tests
- **Agents**: 5 specialized (Clarity, QA, Technical, UX, Compliance)
- **Supervisor**: Dynamic agent selection with keyword-based heuristics
- **Mock cases**: 5 predefined cases in `/mock_inputs`
- **Logging levels**: basic, standard, full
- **NuGet**: Created local `nuget.config` pointing only to nuget.org (the global one has a private Azure DevOps feed that fails with 401)

### 2026-04-20 - Windows execution issue

- **`dotnet run` fails** with "Access is denied" (FileLoadException) when loading the project DLL
- Cause: corporate security policy (likely WDAC or AppLocker) blocking DLL loading from the `dotnet run` host process
- **Solution**: run the compiled `.exe` directly: `.\src\Api\bin\Debug\net9.0\MultiAgentLab.Api.exe --urls "http://localhost:5050"`
- Also works from Visual Studio (F5)
- `dotnet build` works fine, only execution via `dotnet run` fails
- `dotnet test` has the same access denied issue; tests can be run from Visual Studio

### 2026-04-20 - Fix mock_inputs in csproj

- Changed `<None Update>` to `<Content Include>` so that mock_inputs JSON files are copied to the output directory

### 2026-04-21 - Swagger UI added

- Added `Swashbuckle.AspNetCore 7.3.1` for Swagger UI
- Swagger UI available at root `/` (empty RoutePrefix)
- Configured title "MultiAgentLab API" and description in SwaggerDoc

### 2026-04-21 - Dashboard and formatted logs

- Added endpoint `GET /executions/{id}/log/text` that returns the log as readable plain-text timeline
- Added endpoint `GET /dashboard` with a full HTML page to visualize and run mock cases
- The dashboard shows mock case cards, executes against the API, displays results with colored badges, and formatted logs with syntax highlighting
- The dashboard uses a dark theme with colors per event type (request=blue, agent=cyan, ok=green, fail=red, conflict=orange, result=yellow)

### 2026-04-21 - OllamaClient timeout increased

- HttpClient.Timeout increased to 5 minutes (previously 100s default)
- Local llama3.1 can take >100s on long prompts, especially on the first execution (model cold start)

### 2026-04-21 - Successful execution with real Ollama

- Mock-01 (change button text) executed successfully with llama3.1
- Result: **green**, 0 issues, 2 of 2 agents OK (clarity + ux)
- Correct dynamic selection: skipped qa, technical, compliance
- Note: use `127.0.0.1` instead of `localhost` for calls from PowerShell (DNS resolution issue in this environment)

### 2026-04-21 - Ollama model changed to qwen2.5:3b

- `llama3.1` was too slow (>100s per agent, frequent timeouts)
- Changed to `qwen2.5:3b` in all mock cases (faster, less memory)
- Can be installed with `ollama pull qwen2.5:3b` and old models removed with `ollama rm llama3.1`

### 2026-04-21 - Fix ObjectDisposedException in OllamaClient

- Added `using` to `HttpResponseMessage` to avoid `ObjectDisposedException` in VS output

### 2026-04-21 - Fix Data serialization in logs (JsonElement normalization)

- Log events were created with anonymous C# objects (`new { agent, prompt }`)
- When reading from memory, `log.Data as JsonElement?` returned null (they are anonymous objects, not JsonElement)
- Fix: `NormalizeToJsonElement()` in `JsonlExecutionLogger.LogAsync` serializes and deserializes `Data` to JsonElement before saving
- Now the log correctly shows prompts, responses, agent names, etc.

### 2026-04-21 - Parallel agent execution

- `ReviewSupervisor.ReviewAsync` now executes agents with `Task.WhenAll` instead of sequentially
- Accepts optional `preGeneratedExecutionId` for background execution support
- Reduces total execution time to that of the slowest agent

### 2026-04-21 - /start endpoint and live progress

- New endpoint `POST /mock-cases/{caseId}/start` starts execution in background and returns `executionId` immediately
- The dashboard polls `GET /executions/{id}/log` every second and shows live progress:
  - Selected agents with badges
  - Individual status: Pending → Starting → Waiting for LLM → Processing → Completed/Error
  - Animated spinner on agents in progress

### 2026-04-21 - Past execution listing

- `IExecutionLogger.GetAllExecutionIdsAsync()` reads IDs from memory + .jsonl files on disk
- `GET /executions` returns summary of each execution (title, status, time, event count)
- Dashboard "History" tab lists and allows viewing past results

### 2026-04-21 - LLM response parser improvement

- Problem: small models return JSON with common errors (missing commas between strings, issues as objects `{description, severity}` instead of strings)
- `RepairJson()`: regex that inserts missing commas between strings/objects in arrays
- `ExtractStringArray()`: supports arrays of strings and objects with `description` field
- Uses `JsonDocument.Parse` directly instead of deserializing to typed class (more tolerant)
- Attempt 1: original JSON. Attempt 2: repaired JSON. Fallback: parse_error with rawSummary

### 2026-04-21 - Enriched mock stories

- The original stories were too simple; the qwen2.5:3b model couldn't find issues
- Rewrote mock 02-05 texts with deliberate ambiguities and omissions
- Now agents detect real issues and scores reflect the story quality

### 2026-04-21 - Request visible in result and improved parse_error

- The `request_received` event now includes `storyText` in its data
- The result panel shows a "Request sent" box with the story title and text
- Agents with `parse_error` show an orange ⚠ icon and explanatory message instead of just "X score:0"

## Progress

- [x] Complete documentation reviewed (docs 01-04)
- [x] README.md created
- [x] memory.md created
- [x] .NET 9 project structure created (sln + Api + Tests)
- [x] Domain models implemented (ReviewRequest, ReviewResult, AgentResult, AgentContext, ProviderSelection, etc.)
- [x] LLM abstraction implemented (IModelClient, ModelRouter, BedrockClient, OllamaClient)
- [x] JSONL logger implemented (JsonlExecutionLogger + LogEvents)
- [x] 5 agents implemented (ClarityAgent, QaAgent, TechnicalAgent, UxAgent, ComplianceAgent)
- [x] Supervisor implemented (ReviewSupervisor, AgentSelectionRules, ConflictResolver)
- [x] API endpoints implemented (ReviewEndpoints with 5 routes)
- [x] Agent prompts created (.prompt.md)
- [x] Mock case JSONs created (5 files in /mock_inputs)
- [x] Tests created (SupervisorTests + MockCaseTests)
- [x] Successful build
- [x] API running and responding at http://localhost:5050
- [x] Verified mock cases load correctly (5 cases loaded OK)
- [x] Tested POST /review-story endpoint (full flow OK, Ollama unavailable but error handling correct)
- [x] Tested GET /executions/{id}/log endpoint (11 events traced OK)
- [x] Tested POST /mock-cases/mock-01/run with real Ollama (green result, 2/2 agents OK)
- [x] Swagger UI added (Swashbuckle)
- [x] HTML Dashboard with execution visualization and logs (/dashboard)
- [x] Plain text log endpoint (/executions/{id}/log/text)
- [x] OllamaClient timeout increased to 5 min
- [x] Model changed to qwen2.5:3b (faster than llama3.1)
- [x] Fix ObjectDisposedException in OllamaClient
- [x] Fix Data serialization in logs (JsonElement normalization)
- [x] Logs show complete LLM prompts and responses
- [x] Past execution listing (GET /executions + History tab)
- [x] Parallel agent execution (Task.WhenAll)
- [x] /start endpoint with live progress in dashboard
- [x] Robust LLM response parser (repair JSON, support objects with description)
- [x] Enriched mock stories with deliberate ambiguities
- [x] Request visible in result panel
- [x] Parse errors with orange visual indicator in dashboard
- [x] Mock-02 executed with qwen2.5:3b (yellow, 3 agents)
- [ ] Execute mock cases 03-05 with real Ollama
- [ ] Validate UX vs Technical conflicts in mock-05

## Technical Notes

- One provider per execution (same for all agents)
- Architecture prepared for per-agent override in the future
- Simple optional retry on agent failure; the supervisor continues with the rest
- If an agent fails, the result is marked as partial
- The supervisor uses keyword heuristics for agent selection (not LLM)
- BaseReviewAgent uses raw string literals with `$$` for variable interpolation while keeping JSON literal
- Agents parse JSON from the LLM response by finding the first `{` and last `}` (tolerant to extra text)
- If JSON fails, repair is attempted with regex (missing commas between strings/objects)
- Supports issues as `["string"]` or as `[{"description":"...","severity":"..."}]`
- Score is calculated heuristically based on issue count (0 issues=1.0, 1-2=0.7, 3-4=0.5, 5+=0.3)
- Final status: green (<3 issues, no conflicts), yellow (3-6 issues or 1 conflict), red (>6 issues or errors)

## Key Files

- `src/Api/Program.cs` — DI and configuration
- `src/Api/ReviewEndpoints.cs` — 9 REST endpoints + dashboard HTML
- `src/Api/Application/Supervisor/ReviewSupervisor.cs` — main orchestrator
- `src/Api/Application/Supervisor/AgentSelectionRules.cs` — selection heuristics
- `src/Api/Application/Supervisor/ConflictResolver.cs` — UX vs Tech vs Compliance tension detection
- `src/Api/Application/Agents/BaseReviewAgent.cs` — common agent logic (prompt, LLM call, parse)
- `src/Api/Infrastructure/LLM/ModelRouter.cs` — resolves Bedrock or Ollama
- `src/Api/Infrastructure/Logging/JsonlExecutionLogger.cs` — JSONL persistence + in-memory

## Access URLs

- **Swagger UI**: http://127.0.0.1:5050/
- **Dashboard**: http://127.0.0.1:5050/dashboard
- **Plain text log**: http://127.0.0.1:5050/executions/{executionId}/log/text
- **Ollama**: http://localhost:11434 (model: qwen2.5:3b)
