# MultiAgentLab - POC Multiagente de Revision de Historias y Requerimientos

## Descripcion

Prueba de concepto de un sistema multiagente que revisa historias de usuario y requerimientos funcionales/tecnicos. Un **agente supervisor** coordina especialistas, resuelve conflictos entre hallazgos y genera una salida consolidada y accionable.

## Arquitectura

```
Usuario -> Review API -> Supervisor -> Agentes Especializados -> LLM (Bedrock/Ollama)
                                    -> Execution Logger (JSONL)
```

### Agentes Especializados

| Agente | Proposito |
|--------|-----------|
| **Clarity** | Detecta ambiguedades, reglas faltantes y definiciones incompletas |
| **QA** | Evalua testabilidad, criterios de aceptacion y escenarios borde |
| **Technical** | Analiza impacto tecnico, dependencias, performance y complejidad |
| **UX** | Revisa interaccion, mensajes, consistencia de interfaz y usabilidad |
| **Compliance** | Detecta riesgos de seguridad, privacidad y regulatorios |

### Seleccion Dinamica

El supervisor decide que agentes invocar segun el contenido de la historia. No todos los agentes se ejecutan siempre.

### Ejecucion en Paralelo

Los agentes seleccionados se ejecutan en paralelo (`Task.WhenAll`), reduciendo el tiempo total al del agente mas lento.

### Proveedores LLM

- **Amazon Bedrock** — integracion cloud gestionada
- **Ollama** — ejecucion local sin dependencia de red (modelo recomendado: `qwen2.5:3b`)

## Stack Tecnologico

- .NET 9 Web API
- System.Text.Json
- Amazon Bedrock / Ollama
- JSONL para logging
- Swagger UI (Swashbuckle)
- Dashboard HTML integrado

## Estructura del Proyecto

```
/src
  /Api                  - Endpoints REST
  /Application
    /Supervisor         - Orquestacion, seleccion de agentes, resolucion de conflictos
    /Agents             - 5 agentes especializados
    /Prompts            - Prompts de cada agente (.prompt.md)
  /Domain               - Modelos de dominio
  /Infrastructure
    /LLM                - Abstraccion de proveedores (Bedrock, Ollama)
    /Logging            - Logger JSONL
    /Mocks              - Cargador de casos mock
  /Tests                - Pruebas unitarias y de integracion
/mock_inputs            - Archivos JSON de casos demo
```

## Endpoints

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| POST | `/review-story` | Ejecuta revision sobre una historia |
| GET | `/executions` | Lista todas las ejecuciones pasadas con resumen |
| GET | `/executions/{executionId}` | Devuelve resultado final |
| GET | `/executions/{executionId}/log` | Devuelve log completo de eventos (JSON) |
| GET | `/executions/{executionId}/log/text` | Devuelve log formateado como timeline (texto plano) |
| GET | `/mock-cases` | Lista casos demo disponibles |
| POST | `/mock-cases/{caseId}/run` | Ejecuta caso mock (sincrono, espera resultado) |
| POST | `/mock-cases/{caseId}/start` | Inicia ejecucion en background, retorna executionId |
| GET | `/dashboard` | Dashboard visual interactivo |

## Ejecucion

### Prerequisitos

- .NET 9 SDK
- (Opcional) Ollama corriendo localmente en `http://localhost:11434` con modelo `qwen2.5:3b`
- (Opcional) Credenciales AWS configuradas para Bedrock

### Correr la aplicacion

**Opcion 1: Desde Visual Studio**

Abrir `MultiAgentLab.sln` y presionar F5.

**Opcion 2: Desde linea de comandos**

```bash
dotnet build
.\src\Api\bin\Debug\net9.0\MultiAgentLab.Api.exe --urls "http://localhost:5050"
```

> **Nota**: `dotnet run` puede fallar en entornos con politicas de seguridad corporativas (WDAC/AppLocker). Usar el `.exe` directamente o Visual Studio.

La API se levanta en `http://127.0.0.1:5050`.

### Acceso

- **Swagger UI**: http://127.0.0.1:5050/
- **Dashboard**: http://127.0.0.1:5050/dashboard
- **Log visual**: http://127.0.0.1:5050/executions/{executionId}/log/text

### Ejemplo de uso

```bash
curl -X POST http://127.0.0.1:5050/review-story \
  -H "Content-Type: application/json" \
  -d '{
    "storyId": "story-001",
    "title": "Resetear contrasena",
    "storyText": "Como usuario, quiero poder resetear mi contrasena desde la pantalla de login.",
    "provider": {
      "type": "ollama",
      "model": "qwen2.5:3b",
      "endpoint": "http://localhost:11434",
      "temperature": 0.2
    },
    "logging": { "level": "full", "includePrompts": true, "includeResponses": true }
  }'
```

## Casos Mock para Demo

1. **Cambio de label** — caso simple, pocos agentes (esperado: verde)
2. **Resetear contrasena** — historia con gaps deliberados en flujo de UI (esperado: amarillo)
3. **Reintentos automaticos** — historia backend con ambiguedades tecnicas (esperado: amarillo)
4. **Descarga de datos personales** — datos sensibles sin autenticacion ni auditoria (esperado: rojo)
5. **Editar direccion de envio** — tension UX vs tecnico con API externa (esperado: amarillo)

> Las historias mock incluyen ambiguedades y omisiones deliberadas para que incluso modelos chicos detecten issues.

### Dashboard

El dashboard (`/dashboard`) incluye:
- **Tab Ejecutar**: tarjetas de mock cases con ejecucion en background y **progreso en vivo** (estado de cada agente en tiempo real)
- **Tab Historial**: lista de ejecuciones pasadas con acceso a resultados y logs
- Muestra el **requerimiento enviado** a los agentes en el panel de resultado
- Indicadores visuales para `parse_error` (cuando el LLM devuelve JSON invalido)

## Documentacion

- `documentation/01_Diseno_Funcional_y_Solucion_POC_Revision_Historias.md`
- `documentation/02_Arquitectura_Tecnica_POC_Multiagente_Revision_Historias.md`
- `documentation/03_Especificacion_Implementacion_POC_Multiagente_Revision_Historias.md`
- `documentation/04_Casos_Mock_y_Guion_Demo.md`

## Licencia

POC interno — uso exclusivo para aprendizaje y demostracion.
