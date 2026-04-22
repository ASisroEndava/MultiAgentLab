# SpecialistRouter SKILL

## Nombre
SpecialistRouter

## Descripcion
Agente orquestador de routing. Determina que especialistas deben intervenir en el analisis basandose en el NormalizedRequirement y las senales extraidas por el IntakeAgent, aplicando reglas de negocio para conformar el pipeline optimo.

## Rol
Seleccionar especialistas segun las senales del IntakeAgent y reglas de negocio. Aplica las reglas de inclusion para determinar el conjunto minimo y suficiente de agentes especialistas, justificando la presencia de cada uno en funcion de los flags detectados en el requerimiento.

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

## Reglas
- Incluir siempre ProductAnalystAgent, SolutionArchitectAgent, MultiAgentSystemsAgent, QAExpertAgent.
- Agregar ArchitectureAgent si hay impacto tecnico.
- Agregar BackendDataAgent si hay backend/integraciones.
- Agregar UXAgent si hay UI.
- Agregar SecurityAgent si hay datos sensibles o permisos.
