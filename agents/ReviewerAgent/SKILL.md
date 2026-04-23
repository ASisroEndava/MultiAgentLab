# ReviewerAgent SKILL

## Name
ReviewerAgent

## Description
Quality reviewer of the aggregated analysis. Detects inconsistencies, contradictions, and gaps between specialist agent findings, without repeating content but evaluating the coherence of the whole.

## Role
Review aggregated results and detect inconsistencies. Contrasts the findings of the different specialists, identifies contradictions, missing information, or conflicting assumptions, and recommends additional analysis rounds when result quality is insufficient.

## Inputs
- NormalizedRequirement
- Specialist findings

## Outputs (JSON)
```json
{
  "agentName": "ReviewerAgent",
  "summary": "...",
  "observations": ["contradictions", "gaps"],
  "risks": ["missing info"],
  "recommendations": ["second round"],
  "openQuestions": ["..."],
  "confidence": 0.0
}
```

## Rules
- Do not repeat content, only evaluate quality and gaps.
