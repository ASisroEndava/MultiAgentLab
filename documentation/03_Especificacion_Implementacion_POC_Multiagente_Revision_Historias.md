# Especificacion de Implementacion
## POC Multiagente de Revision de Historias y Requerimientos

## 1. Stack sugerido

### Opcion preferida para una POC corporativa rapida
- **Backend**: .NET 8 Web API o Azure Functions
- **Serializacion**: System.Text.Json
- **LLM providers**:
  - Amazon Bedrock
  - Ollama
- **Persistencia de logs**:
  - archivos JSONL,
  - o SQLite
- **UI de demo**:
  - consola,
  - minimal web UI,
  - o Postman

### Alternativa
- Python con FastAPI si se quiere iterar prompts mas rapido.

Este documento usa ejemplos orientados a **C#/.NET**.

---

## 2. Estructura sugerida del proyecto

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

## 3. Contratos de entrada y salida

## 3.1 Request principal

```json
{
  "storyId": "story-003",
  "title": "Reintentos de notificaciones",
  "storyText": "Como sistema, necesito reintentar automaticamente el envio de notificaciones fallidas hasta 3 veces antes de marcarlas como error definitivo.",
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

## 3.2 Response final

```json
{
  "executionId": "exec-003",
  "status": "amarillo",
  "summary": "El requerimiento tiene intencion clara pero faltan definiciones operativas.",
  "provider": "ollama",
  "model": "llama3.1",
  "invokedAgents": ["clarity", "qa", "technical"],
  "skippedAgents": [
    {
      "agent": "ux",
      "reason": "Historia orientada a backend sin interaccion de usuario visible"
    },
    {
      "agent": "compliance",
      "reason": "No se detectaron datos sensibles ni trazabilidad regulatoria"
    }
  ],
  "issues": [
    "No se define intervalo entre reintentos",
    "No se listan errores reintentables",
    "No se define estrategia de auditoria"
  ],
  "recommendations": [
    "Definir politica de reintentos",
    "Especificar errores transitorios vs permanentes",
    "Agregar metricas y trazabilidad"
  ],
  "conflicts": []
}
```

---

## 4. Contrato comun de agentes

Todos los agentes deben cumplir una interfaz comun.

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

## 5. Abstraccion del proveedor LLM

## 5.1 Interfaz comun

```csharp
public interface IModelClient
{
    Task<ModelResponse> GenerateAsync(
        ModelRequest request,
        CancellationToken cancellationToken = default);
}
```

## 5.2 Router de proveedor

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

### Regla clave
La logica del supervisor y de los agentes **no debe conocer detalles especificos de Bedrock u Ollama** mas alla de la configuracion recibida.

---

## 6. Orquestacion del supervisor

## 6.1 Responsabilidades
- analizar el texto;
- seleccionar agentes;
- ejecutar agentes en orden razonable;
- capturar resultados;
- resolver conflictos;
- producir salida final.

## 6.2 Pseudocodigo del flujo principal

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

## 7. Reglas de seleccion de agentes

## 7.1 Regla base
Siempre intentar usar **clarity**, salvo que el caso sea un cambio trivial preclasificado.

## 7.2 Heuristicas minimas sugeridas

### Invocar QA si:
- hay reglas de validacion;
- hay flujos con error;
- hay estados esperados;
- la historia requiere aceptacion formal.

### Invocar tecnico si:
- hay backend;
- hay integracion;
- hay reintentos;
- hay procesos asincronos;
- hay persistencia o consistencia.

### Invocar UX si:
- hay pantalla o formulario;
- hay mensajes al usuario;
- hay acciones visibles en UI;
- hay copy, botones, feedback o navegacion.

### Invocar compliance si:
- hay datos personales;
- hay descarga/exportacion;
- hay auditoria;
- hay autorizacion;
- hay riesgo regulatorio.

---

## 8. Reglas de resolucion de conflictos

El supervisor debe poder arbitrar cuando dos agentes recomiendan cosas distintas.

### Reglas sugeridas

1. **Compliance tiene prioridad sobre UX**  
   Si UX propone una simplificacion que compromete seguridad o privacidad, gana compliance.

2. **Factibilidad tecnica condiciona UX**  
   Si UX sugiere un comportamiento no viable con el alcance actual, el supervisor lo marca como mejora futura o lo condiciona.

3. **Falta de testabilidad degrada el estado global**  
   Si QA detecta ausencia severa de criterios de aceptacion, el estado final no deberia ser verde.

4. **Ambiguedad funcional contagia a los demas**  
   Si clarity encuentra huecos centrales, el supervisor debe reflejarlo aun si otros agentes pudieron opinar.

### Ejemplo
Historia: “editar direccion de envio desde perfil”.

- UX: “edicion inline inmediata”.
- Tecnico: “cambiar direccion puede impactar pedidos ya preparados”.

**Resolucion del supervisor:**
- permitir edicion solo para pedidos no despachados;
- mostrar restriccion en UI;
- marcar comportamiento alternativo para ordenes ya procesadas.

---

## 9. Diseno de prompts

Cada agente debe tener:

- rol fijo;
- salida JSON obligatoria;
- limites claros;
- tono analitico, no creativo;
- foco en observaciones utiles.

## 9.1 Prompt base del agente de claridad

```text
Eres un revisor funcional especializado en historias de usuario.
Analiza la historia y detecta ambiguedades, reglas faltantes, escenarios no definidos y preguntas necesarias.
Responde exclusivamente en JSON con esta forma:
{
  "issues": [],
  "recommendations": [],
  "questions": [],
  "rawSummary": ""
}
```

## 9.2 Prompt base del agente QA

```text
Eres un analista QA especializado en testabilidad.
Revisa si la historia permite construir criterios de aceptacion y casos de prueba.
Detecta validaciones faltantes, escenarios borde y estados de error no definidos.
Responde solo en JSON.
```

## 9.3 Prompt base del agente tecnico

```text
Eres un arquitecto/ingeniero de software especializado en impacto tecnico.
Detecta riesgos tecnicos, dependencias, asincronia, consistencia, duplicados, observabilidad y complejidad.
Responde solo en JSON.
```

## 9.4 Prompt base del agente UX

```text
Eres un especialista UX.
Revisa claridad de interaccion, feedback al usuario, consistencia de interfaz, mensajes y fricciones.
Responde solo en JSON.
```

## 9.5 Prompt base del agente compliance

```text
Eres un especialista en seguridad, privacidad y compliance.
Detecta exposicion de datos, problemas de autorizacion, trazabilidad faltante o riesgos regulatorios.
Responde solo en JSON.
```

---

## 10. Logging funcional y tecnico

## 10.1 Interfaz de logging

```csharp
public interface IExecutionLogger
{
    Task LogAsync(ExecutionLogEvent logEvent, CancellationToken cancellationToken = default);
}
```

## 10.2 Evento de log

```csharp
public sealed class ExecutionLogEvent
{
    public required string ExecutionId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string EventType { get; init; }
    public required object Data { get; init; }
}
```

## 10.3 Ejemplo JSONL

```json
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:00Z","eventType":"request_received","data":{"storyId":"story-004","provider":"bedrock","model":"example-model"}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:01Z","eventType":"selected_agents","data":{"invoked":["clarity","qa","technical","compliance"],"skipped":[{"agent":"ux","reason":"La interfaz no es el foco principal del requerimiento"}]}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:03Z","eventType":"agent_completed","data":{"agent":"clarity","issues":["No se define formato del archivo"]}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:04Z","eventType":"agent_completed","data":{"agent":"compliance","issues":["Debe validarse identidad del titular","El archivo debe expirar"]}}
{"executionId":"exec-004","timestamp":"2026-04-20T14:01:05Z","eventType":"final_result_generated","data":{"status":"rojo"}}
```

## 10.4 Que mostrar en la demo
- lista de agentes invocados;
- motivos de omision;
- hallazgos por agente;
- resolucion del supervisor;
- timeline.

---

## 11. Endpoints sugeridos

## 11.1 POST /review-story
Ejecuta una revision real sobre el texto recibido.

## 11.2 GET /executions/{executionId}
Devuelve el resultado final.

## 11.3 GET /executions/{executionId}/log
Devuelve el log completo de eventos.

## 11.4 GET /mock-cases
Lista los casos demo disponibles.

## 11.5 POST /mock-cases/{caseId}/run
Ejecuta un caso mock con el proveedor indicado.

---

## 12. Casos mock obligatorios para demo

Se recomienda incluir como minimo los siguientes:

1. **Cambio de label**
2. **Resetear contrasena**
3. **Reintentos automaticos**
4. **Descarga de datos personales**
5. **Editar direccion de envio**

El detalle completo se encuentra en `04_Casos_Mock_y_Guion_Demo.md`.

---

## 13. Pruebas

## 13.1 Pruebas unitarias
- seleccion de agentes;
- resolucion de conflictos;
- serializacion de resultados;
- normalizacion de Bedrock y Ollama.

## 13.2 Pruebas de integracion
- corrida completa con Bedrock;
- corrida completa con Ollama;
- generacion de logs;
- corrida de casos mock.

## 13.3 Criterios minimos de aceptacion
- todos los casos mock deben correr;
- debe verse claramente que agentes no fueron usados;
- debe existir un `executionId` trazable;
- debe poder consultarse el log;
- el proveedor debe ser intercambiable por configuracion.

---

## 14. Plan de implementacion sugerido (4 a 5 dias)

### Dia 1
- crear estructura base;
- definir contratos;
- implementar model router;
- conectar un proveedor primero.

### Dia 2
- implementar supervisor;
- implementar reglas de seleccion;
- implementar 2 agentes:
  - clarity,
  - QA.

### Dia 3
- implementar technical, UX y compliance;
- agregar logging JSONL;
- agregar endpoint de consulta de logs.

### Dia 4
- crear casos mock;
- ajustar prompts;
- validar Bedrock y Ollama;
- preparar demo.

### Dia 5 (opcional)
- UI simple para visualizar historia, agentes, log y resultado;
- mejorar formato de salida para presentacion.

---

## 15. Recomendacion final de implementacion

Para que la demo sea fuerte y no se vuelva demasiado grande:

- mantener el scope de agentes en 5;
- soportar un proveedor por ejecucion;
- usar JSONL para logging;
- preparar casos mock con expectativa clara;
- mostrar visualmente las decisiones del supervisor;
- destacar que no todos los agentes corren siempre.

Si se hace bien, esta POC permite ensenar en pocos minutos:
- especializacion de agentes;
- coordinacion real;
- abstraccion Bedrock/Ollama;
- trazabilidad de conversaciones y decisiones;
- valor practico para revision temprana de historias.
