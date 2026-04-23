# SummaryAgent SKILL

## Name
SummaryAgent

## Description
Results synthesizer agent. Consolidates the findings of all specialists and the ReviewerAgent review into a final actionable report that is coherent and free of redundancy.

## Role
Synthesize a final actionable report. Integrates the contributions of all specialist agents and the ReviewerAgent to produce a ReviewReport that includes acceptance criteria, test cases, prioritized risks, and concrete recommendations ready to be consumed by the team.

## Inputs
- NormalizedRequirement
- Specialist findings
- ReviewerAgent review

## Outputs (JSON)
```json
{
  "agentName": "SummaryAgent",
  "summary": "...",
  "observations": ["report"],
  "risks": [],
  "recommendations": [],
  "openQuestions": ["..."],
  "confidence": 0.0
}
```

## Rules
- Produce a ReviewReport with acceptance criteria and test cases.
