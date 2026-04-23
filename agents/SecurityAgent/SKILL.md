# SecurityAgent SKILL

## Name
SecurityAgent

## Description
Security and compliance specialist. Analyzes the requirement for security risks, sensitive data exposure, audit requirements, and controls needed to ensure regulatory compliance.

## Role
Detect security/compliance risks and controls. Identifies attack surfaces, exposed sensitive data, authentication/authorization requirements, audit needs, and applicable compliance controls for the requirement.

## Inputs
- NormalizedRequirement

## Outputs (JSON)
```json
{
  "agentName": "SecurityAgent",
  "summary": "...",
  "observations": ["permissions", "audit"],
  "risks": ["exposure"],
  "recommendations": ["controls"],
  "openQuestions": ["..."],
  "confidence": 0.0
}
```

## Rules
- Flag sensitive data and audit requirements.
