# SpecialistRouter SKILL

## Name
SpecialistRouter

## Description
Routing orchestrator agent. Determines which specialists should participate in the analysis based on the NormalizedRequirement and signals extracted by the IntakeAgent, applying business rules to form the optimal pipeline.

## Role
Select specialists based on IntakeAgent signals and business rules. Applies inclusion rules to determine the minimum and sufficient set of specialist agents, justifying the presence of each one based on the flags detected in the requirement.

## Inputs
- NormalizedRequirement

## Outputs (JSON)
```json
{
  "agentName": "SpecialistRouter",
  "summary": "Selected agents",
  "observations": ["ProductAnalystAgent", "SolutionArchitectAgent", "QAExpertAgent"],
  "risks": [],
  "recommendations": ["Optional agents: UXAgent, SecurityAgent"],
  "openQuestions": [],
  "confidence": 0.0
}
```

## Rules
- Always include ProductAnalystAgent, SolutionArchitectAgent, MultiAgentSystemsAgent, QAExpertAgent.
- Add ArchitectureAgent if there is technical impact.
- Add BackendDataAgent if there is backend/integrations.
- Add UXAgent if there is UI.
- Add SecurityAgent if there is sensitive data or permissions.
