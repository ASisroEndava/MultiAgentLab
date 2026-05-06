---
name: QAAgent
description: Base QA agent, used as a fallback when QAExpertAgent is not available. Generates fundamental test cases and minimum acceptance criteria to validate the requirement.
tags: [qa, testing, fallback, regression]
---

# QAAgent Skill

## Role
Base QA for test cases and acceptance criteria. Produces essential test scenarios (positive, negative, and edge cases), identifies regression risks, and recommends basic automation strategies when QAExpertAgent is not part of the pipeline.

## Inputs
- NormalizedRequirement

## Outputs (JSON)
```json
{
  "agentName": "QAAgent",
  "summary": "...",
  "observations": ["scenarios"],
  "risks": ["regression"],
  "recommendations": ["automation"],
  "openQuestions": ["..."],
  "confidence": 0.0
}
```

## Rules
- Use when QAExpertAgent is not available.
