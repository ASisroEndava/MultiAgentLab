# MultiAgentLab - Multi-Agent POC for User Story and Requirements Review

## Description

Proof of concept of a multi-agent system that reviews user stories and functional/technical requirements. A **supervisor agent** coordinates specialists, resolves conflicts between findings, and generates a consolidated and actionable output.

## Architecture

```
User -> Review API -> Supervisor -> Specialized Agents -> LLM (Bedrock/Ollama)
                                 -> Execution Logger (JSONL)
```

### Specialized Agents

| Agent | Purpose |
|-------|---------|
| **Clarity** | Detects ambiguities, missing rules, and incomplete definitions |
| **QA** | Evaluates testability, acceptance criteria, and edge scenarios |
| **Technical** | Analyzes technical impact, dependencies, performance, and complexity |
| **UX** | Reviews interaction, messages, interface consistency, and usability |
| **Compliance** | Detects security, privacy, and regulatory risks |

### Dynamic Selection

The supervisor decides which agents to invoke based on the story content. Not all agents are executed every time.

### Parallel Execution

Selected agents run in parallel (`Task.WhenAll`), reducing total time to that of the slowest agent.

### LLM Providers

- **Amazon Bedrock** — managed cloud integration
- **Ollama** — local execution with no network dependency (recommended model: `qwen2.5:3b`)

## Technology Stack

- .NET 9 Web API
- System.Text.Json
- Amazon Bedrock / Ollama
- JSONL for logging
- Swagger UI (Swashbuckle)
- Embedded HTML Dashboard

## Project Structure

```
/src
  /Api                  - REST Endpoints
  /Application
    /Supervisor         - Orchestration, agent selection, conflict resolution
    /Agents             - 5 specialized agents
    /Prompts            - Agent prompts (.prompt.md)
  /Domain               - Domain models
  /Infrastructure
    /LLM                - Provider abstraction (Bedrock, Ollama)
    /Logging            - JSONL Logger
    /Mocks              - Mock case loader
  /Tests                - Unit and integration tests
/mock_inputs            - JSON files for demo cases
```

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/review-story` | Executes review on a story |
| GET | `/executions` | Lists all past executions with summary |
| GET | `/executions/{executionId}` | Returns final result |
| GET | `/executions/{executionId}/log` | Returns complete event log (JSON) |
| GET | `/executions/{executionId}/log/text` | Returns formatted log as timeline (plain text) |
| GET | `/mock-cases` | Lists available demo cases |
| POST | `/mock-cases/{caseId}/run` | Runs mock case (synchronous, waits for result) |
| POST | `/mock-cases/{caseId}/start` | Starts background execution, returns executionId |
| GET | `/dashboard` | Interactive visual dashboard |

## Running

### Prerequisites

- .NET 9 SDK
- (Optional) Ollama running locally at `http://localhost:11434` with model `qwen2.5:3b`
- (Optional) AWS credentials configured for Bedrock

### Run the application

**Option 1: From Visual Studio**

Open `MultiAgentLab.sln` and press F5.

**Option 2: From command line**

```bash
dotnet build
.\src\Api\bin\Debug\net9.0\MultiAgentLab.Api.exe --urls "http://localhost:5050"
```

> **Note**: `dotnet run` may fail in environments with corporate security policies (WDAC/AppLocker). Use the `.exe` directly or Visual Studio.

The API starts at `http://127.0.0.1:5050`.

### Access

- **Swagger UI**: http://127.0.0.1:5050/
- **Dashboard**: http://127.0.0.1:5050/dashboard
- **Visual log**: http://127.0.0.1:5050/executions/{executionId}/log/text

### Usage example

```bash
curl -X POST http://127.0.0.1:5050/review-story \
  -H "Content-Type: application/json" \
  -d '{
    "storyId": "story-001",
    "title": "Reset password",
    "storyText": "As a user, I want to be able to reset my password from the login screen.",
    "provider": {
      "type": "ollama",
      "model": "qwen2.5:3b",
      "endpoint": "http://localhost:11434",
      "temperature": 0.2
    },
    "logging": { "level": "full", "includePrompts": true, "includeResponses": true }
  }'
```

## Mock Cases for Demo

1. **Label change** — simple case, few agents (expected: green)
2. **Reset password** — story with deliberate gaps in UI flow (expected: yellow)
3. **Automatic retries** — backend story with technical ambiguities (expected: yellow)
4. **Personal data download** — sensitive data without authentication or audit (expected: red)
5. **Edit shipping address** — UX vs technical tension with external API (expected: yellow)

> Mock stories include deliberate ambiguities and omissions so that even small models detect issues.

### Dashboard

The dashboard (`/dashboard`) includes:
- **Execute tab**: mock case cards with background execution and **live progress** (real-time status of each agent)
- **History tab**: list of past executions with access to results and logs
- Shows the **request sent** to agents in the result panel
- Visual indicators for `parse_error` (when the LLM returns invalid JSON)

## Documentation

- `documentation/01_Functional_Design_and_Solution_POC_Story_Review.md`
- `documentation/02_Technical_Architecture_POC_Multiagent_Story_Review.md`
- `documentation/03_Implementation_Specification_POC_Multiagent_Story_Review.md`
- `documentation/04_Mock_Cases_and_Demo_Script.md`

## License

Internal POC — for learning and demonstration purposes only.
