# Multi-Agent POC: User Story and Requirements Review
## Functional and Solution Design

## 1. Objective

Design a proof of concept for **multi-agent review of user stories and requirements** in which a **supervisor agent** decides which specialists to invoke, coordinates the flow, resolves conflicts between findings, and generates a consolidated and actionable output for product, QA, and development.

The system must allow:

- receiving a user story or functional/technical requirement;
- dynamically deciding which specialized agents are worth consulting;
- choosing the model provider for execution:
  - **Amazon Bedrock**, or
  - **local model via Ollama**;
- recording a **complete log of conversations, decisions, and results**;
- executing **demo mock cases** to demonstrate the architecture's operation.

---

## 2. Problem it solves

In many teams, user stories reach development with quality issues such as:

- functional ambiguity;
- incomplete acceptance criteria;
- undetected technical risks;
- unconsidered UX impact;
- omitted security, privacy, or compliance implications.

The POC aims to demonstrate that a multi-agent architecture improves the initial requirement review without requiring all agents to participate every time.

---

## 3. Business and learning objectives

### 3.1 Business objectives

- Improve the initial quality of stories and requirements.
- Reduce rework due to incomplete stories.
- Detect risks before passing to development.
- Standardize a repeatable preliminary review.

### 3.2 Technical learning objectives

- Practice multi-agent orchestration.
- Implement dynamic agent selection.
- Abstract the use of multiple LLM providers.
- Design traceability of decisions and conversations.
- Demonstrate the value of the supervisor as a real coordination layer.

---

## 4. POC Scope

### 4.1 Includes

- manual input of a story or requirement;
- evaluation by multiple specialized agents;
- model provider selection:
  - Bedrock,
  - Ollama;
- consolidation of findings by a supervisor;
- detailed log per execution;
- set of mock cases for demo;
- structured output with readiness traffic light.

### 4.2 Does not include

- real integration with Jira/Azure DevOps;
- corporate authentication;
- model training;
- historical learning base;
- review of complex attached files;
- formal approval workflow.

---

## 5. System users

### 5.1 Product Owner / Functional analyst
Uses the tool to detect missing elements or ambiguities before refining a story.

### 5.2 QA
Uses the system's output to derive initial acceptance criteria and test cases.

### 5.3 Technical lead / developer
Reviews technical impact, dependencies, and early risks.

### 5.4 Security/compliance officer
Intervenes in stories that touch personal data, traceability, fraud, or regulation.

### 5.5 Demo / internal learning team
Uses mock cases to demonstrate the capability of dynamic agent selection and the decision log.

---

## 6. High-level functional flow

1. The user enters a user story or requirement.
2. Chooses the model provider:
   - **Bedrock**, or
   - **Ollama**.
3. The supervisor inspects the story.
4. The supervisor decides which agents to invoke.
5. Each agent returns structured findings.
6. The supervisor resolves conflicts and synthesizes.
7. The system saves:
   - decisions,
   - prompts/messages,
   - responses,
   - final result.
8. The user views:
   - invoked agents,
   - skipped agents,
   - observations,
   - risk level/traffic light,
   - complete execution log.

---

## 7. Specialized agents

## 7.1 Functional clarity agent
**Purpose:** review whether the story is understandable, specific, and actionable.

**Looks for:**
- ambiguities;
- missing definitions;
- implicit business rules;
- undefined behaviors.

**Example findings:**
- "It is not clarified what happens if the email does not exist."
- "It is not defined whether the report is generated online or asynchronously."

**When typically invoked:** almost always, except for very limited trivial changes.

---

## 7.2 QA / Testability agent
**Purpose:** detect whether the story allows designing tests and acceptance criteria.

**Looks for:**
- absence of Given/When/Then;
- undefined expected states;
- missing validations;
- edge scenario coverage.

**Example findings:**
- "Error states are not defined."
- "Acceptance criteria for invalid attempts are missing."

**When typically invoked:** in functional stories, backend, or changes with validations.

---

## 7.3 Technical agent
**Purpose:** analyze technical impact, architecture, dependencies, performance, and complexity.

**Looks for:**
- technical risks;
- dependencies with other systems;
- need for asynchrony;
- idempotency;
- data consistency;
- observability.

**Example findings:**
- "A queue or scheduler is recommended for retries."
- "There may be duplicates if idempotency is not guaranteed."

**When typically invoked:** when the story has backend behavior, integrations, states, or operational impact.

---

## 7.4 UX agent
**Purpose:** review interaction, messages, interface consistency, and user experience.

**Looks for:**
- UI friction;
- unclear messages;
- unnecessary steps;
- visual feedback issues;
- usability risks.

**Example findings:**
- "The message should not reveal whether the email exists."
- "Loading and confirmation feedback is needed."

**When typically invoked:** in stories with visible user interaction.

---

## 7.5 Compliance / security / privacy agent
**Purpose:** review regulatory implications, security, or sensitive data handling.

**Looks for:**
- PII exposure;
- insufficient authorization;
- missing audit/traceability;
- potential non-compliance.

**Example findings:**
- "Downloading personal data requires holder identity verification."
- "The generated file should expire."

**When typically invoked:** only when the story touches personal data, fraud, audit, regulation, or security.

---

## 8. Supervisor role

The supervisor is the central piece of the system. It does not just distribute work: it makes decisions.

### Responsibilities

- detect the story type;
- choose the model provider configured for the execution;
- decide which agents to invoke and which to skip;
- control invocation order and context;
- compare findings;
- resolve contradictions;
- synthesize a final response with severity level.

### Examples of supervisor decisions

- **Simple text change:** invokes functional clarity and, optionally, UX.
- **Backend story:** invokes clarity, QA, and technical; skips UX.
- **Story with personal data:** activates compliance in addition to other agents.
- **UX vs technical conflict:** prioritizes feasibility and risk over convenience.

---

## 9. Dynamic agent selection

One of the POC's objectives is to demonstrate that **not all agents need to be executed every time**.

### Guiding rules

- If the requirement is a minimal text change:
  - use **clarity**;
  - optional **UX**;
  - skip technical and compliance.
- If there are rules, states, or validations:
  - use **clarity** and **QA**.
- If there are retries, queues, integrations, or persistence:
  - use **technical**.
- If there are screens, forms, or user feedback:
  - use **UX**.
- If there is personal data, security, audit, or regulation:
  - use **compliance**.

---

## 10. Model selection: Bedrock or Ollama

The POC must allow choosing the LLM engine per execution.

### Option 1: Amazon Bedrock
Recommended use when you want to:
- demonstrate managed cloud integration;
- evaluate enterprise models;
- leverage centralized configuration.

### Option 2: Local Ollama
Recommended use when you want to:
- run the demo locally;
- avoid network or cloud dependency;
- experiment quickly with local models.

### Functional decision
The user can choose:
- provider;
- model;
- temperature;
- logging level.

### Recommended mode for POC
- one provider selection per execution;
- same provider for all agents in that run;
- optionally prepare a per-agent override for future comparative tests.

---

## 11. Logging and traceability

The POC must explicitly show the system's internal journey.

## 11.1 What must be recorded

- execution identifier;
- submitted story;
- chosen provider/model;
- invoked agents;
- skipped agents and reason;
- prompts/messages sent to each agent;
- response from each agent;
- detected conflicts;
- supervisor's final decision;
- timestamps and duration.

## 11.2 What must be viewable

- execution timeline;
- agent tree or sequence;
- supervisor's decision;
- consolidated final result.

## 11.3 Value of the log in the demo

The log shows that:
- agents are truly differentiated;
- the supervisor makes concrete decisions;
- the system does not execute unnecessary work;
- the final output has traceability.

---

## 12. Expected result

The final output should include:

- traffic light:
  - **green** = ready or nearly ready;
  - **yellow** = missing definitions;
  - **red** = high risk or incomplete story;
- executive summary;
- invoked agents;
- key findings;
- contradictions or tensions;
- actionable recommendations;
- suggested next steps.

### Structure example

```json
{
  "executionId": "rev-001",
  "provider": "ollama",
  "model": "llama3.1",
  "status": "yellow",
  "invokedAgents": ["clarity", "qa", "ux"],
  "skippedAgents": [
    {
      "agent": "compliance",
      "reason": "No sensitive data or regulatory requirements detected"
    }
  ],
  "summary": "The story is understandable but incomplete.",
  "issues": [
    "Behavior for non-existent email is not defined",
    "Acceptance criteria are missing",
    "Link expiration is not clarified"
  ],
  "recommendations": [
    "Add expiration rules",
    "Define a generic message",
    "Include Given/When/Then scenarios"
  ]
}
```

---

## 13. POC success criteria

The POC is considered successful if it demonstrates:

- dynamic agent selection;
- support for Bedrock and Ollama;
- complete traceability per execution;
- concrete utility for refining stories;
- consistent set of mock cases for demo.

### Indicative metrics

- execution time per story: less than 10 seconds in demo environment;
- valid JSON structure in agent responses: greater than 95%;
- at least 4 executable mock cases;
- visible supervisor and agent log for each run.

---

## 14. Known risks

- overly open prompts can produce inconsistent outputs;
- local models can vary in quality depending on hardware and model;
- excessive logging can create visual noise;
- if the scope is not bounded, the POC may look like a complete suite instead of a demo.

---

## 15. Demo recommendation

Show a sequence with increasing difficulty:

1. **Simple change**  
   Few agents are invoked.

2. **Functional story with UI**  
   Clarity + QA + UX are shown.

3. **Technical backend story**  
   Clarity + QA + technical are shown.

4. **Story with personal data**  
   Compliance activation is shown.

5. **Story with UX vs technical tension**  
   Supervisor arbitration is demonstrated.

Mock case details are documented in the file `04_Mock_Cases_and_Demo_Script.md`.
