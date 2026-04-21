# Memory - Decisiones y Progreso del Proyecto

## Decisiones Tomadas

### 2026-04-20 - Inicio de implementacion

- **Stack elegido**: .NET 9 Web API con System.Text.Json (SDK instalado: 9.0.312)
- **Proveedores LLM**: Amazon Bedrock (AWSSDK.BedrockRuntime 3.7.414.0) + Ollama local
- **Persistencia de logs**: Archivos JSONL (opcion mas simple para POC)
- **Estructura**: Segun especificacion en doc 03 — `/src` con Api, Application, Domain, Infrastructure, Tests
- **Agentes**: 5 especializados (Clarity, QA, Technical, UX, Compliance)
- **Supervisor**: Seleccion dinamica de agentes con heuristicas por keywords
- **Mock cases**: 5 casos predefinidos en `/mock_inputs`
- **Logging levels**: basic, standard, full
- **NuGet**: Se creo `nuget.config` local que apunta solo a nuget.org (el global tiene un feed privado de Azure DevOps que falla con 401)

### 2026-04-20 - Problema de ejecucion en Windows

- **`dotnet run` falla** con "Access is denied" (FileLoadException) al cargar el DLL del proyecto
- Causa: politica de seguridad corporativa (probablemente WDAC o AppLocker) que bloquea la carga de DLLs desde el host process de `dotnet run`
- **Solucion**: ejecutar el `.exe` compilado directamente: `.\src\Api\bin\Debug\net9.0\MultiAgentLab.Api.exe --urls "http://localhost:5050"`
- Tambien funciona desde Visual Studio (F5)
- `dotnet build` funciona sin problemas, solo falla la ejecucion via `dotnet run`
- `dotnet test` tiene el mismo problema de acceso denegado; los tests se pueden correr desde Visual Studio

### 2026-04-20 - Fix mock_inputs en csproj

- Se cambio `<None Update>` a `<Content Include>` para que los archivos JSON de mock_inputs se copien al output directory

### 2026-04-21 - Swagger UI agregado

- Se agrego `Swashbuckle.AspNetCore 7.3.1` para tener Swagger UI
- Swagger UI disponible en la raiz `/` (RoutePrefix vacío)
- Se configuro titulo "MultiAgentLab API" y descripcion en SwaggerDoc

### 2026-04-21 - Dashboard y logs formateados

- Se agrego endpoint `GET /executions/{id}/log/text` que devuelve el log en texto plano formateado como timeline legible
- Se agrego endpoint `GET /dashboard` con una pagina HTML completa para visualizar y ejecutar mock cases
- El dashboard muestra tarjetas de mock cases, ejecuta contra la API, muestra resultado con badge de color, y el log formateado con syntax highlighting
- El dashboard usa dark theme con colores por tipo de evento (request=azul, agent=cyan, ok=verde, fail=rojo, conflict=naranja, result=amarillo)

### 2026-04-21 - Timeout de OllamaClient aumentado

- HttpClient.Timeout aumentado a 5 minutos (antes era 100s por defecto)
- llama3.1 local puede tardar >100s en prompts largos, especialmente en la primera ejecucion (cold start del modelo)

### 2026-04-21 - Ejecucion exitosa con Ollama real

- Mock-01 (cambiar texto de boton) ejecutado exitosamente con llama3.1
- Resultado: **verde**, 0 issues, 2 de 2 agentes OK (clarity + ux)
- Seleccion dinamica correcta: omitio qa, technical, compliance
- Nota: usar `127.0.0.1` en lugar de `localhost` para las llamadas desde PowerShell (hay un problema de resolucion DNS en este entorno)

### 2026-04-21 - Cambio de modelo Ollama a qwen2.5:3b

- `llama3.1` era muy lento (>100s por agente, timeouts frecuentes)
- Se cambio a `qwen2.5:3b` en todos los mock cases (mas rapido, menos memoria)
- Se puede instalar con `ollama pull qwen2.5:3b` y eliminar modelos viejos con `ollama rm llama3.1`

### 2026-04-21 - Fix ObjectDisposedException en OllamaClient

- Se agrego `using` a `HttpResponseMessage` para evitar `ObjectDisposedException` en el output de VS

### 2026-04-21 - Fix Data serialization en logs (JsonElement normalization)

- Los log events se creaban con objetos anonimos C# (`new { agent, prompt }`)
- Al leer desde memoria, `log.Data as JsonElement?` daba null (son objetos anonimos, no JsonElement)
- Fix: `NormalizeToJsonElement()` en `JsonlExecutionLogger.LogAsync` serializa y deserializa el `Data` a JsonElement antes de guardarlo
- Ahora el log muestra correctamente prompts, responses, nombres de agentes, etc.

### 2026-04-21 - Ejecucion paralela de agentes

- `ReviewSupervisor.ReviewAsync` ahora ejecuta agentes con `Task.WhenAll` en vez de secuencial
- Acepta `preGeneratedExecutionId` opcional para soporte de ejecucion en background
- Reduce el tiempo total de ejecucion al del agente mas lento

### 2026-04-21 - Endpoint /start y progreso en vivo

- Nuevo endpoint `POST /mock-cases/{caseId}/start` inicia la ejecucion en background y retorna `executionId` inmediatamente
- El dashboard pollea `GET /executions/{id}/log` cada segundo y muestra progreso en vivo:
  - Agentes seleccionados con badges
  - Estado individual: Pendiente → Iniciando → Esperando LLM → Procesando → Completado/Error
  - Spinner animado en agentes en progreso

### 2026-04-21 - Listado de ejecuciones pasadas

- `IExecutionLogger.GetAllExecutionIdsAsync()` lee IDs de memoria + archivos .jsonl en disco
- `GET /executions` devuelve resumen de cada ejecucion (titulo, status, tiempo, cantidad de eventos)
- Dashboard tab "Historial" lista y permite ver resultados pasados

### 2026-04-21 - Mejora del parser de respuestas LLM

- Problema: modelos chicos devuelven JSON con errores comunes (comas faltantes entre strings, issues como objetos `{description, severity}` en vez de strings)
- `RepairJson()`: regex que inserta comas faltantes entre strings/objetos en arrays
- `ExtractStringArray()`: soporta arrays de strings y de objetos con campo `description`
- Usa `JsonDocument.Parse` directo en vez de deserializar a clase tipada (mas tolerante)
- Intento 1: JSON original. Intento 2: JSON reparado. Fallback: parse_error con rawSummary

### 2026-04-21 - Historias mock enriquecidas

- Las historias originales eran muy simples; el modelo qwen2.5:3b no encontraba issues
- Se reescribieron los textos de mock 02-05 con ambiguedades y omisiones deliberadas
- Ahora los agentes detectan issues reales y los scores reflejan la calidad de la historia

### 2026-04-21 - Requerimiento visible en resultado y parse_error mejorado

- El evento `request_received` ahora incluye `storyText` en su data
- El panel de resultado muestra un box "Requerimiento enviado" con titulo y texto de la historia
- Los agentes con `parse_error` muestran icono ⚠ naranja y mensaje explicativo en vez de solo "X score:0"

## Progreso

- [x] Documentacion completa revisada (docs 01-04)
- [x] README.md creado
- [x] memory.md creado
- [x] Estructura de proyecto .NET 9 creada (sln + Api + Tests)
- [x] Modelos de dominio implementados (ReviewRequest, ReviewResult, AgentResult, AgentContext, ProviderSelection, etc.)
- [x] Abstraccion LLM implementada (IModelClient, ModelRouter, BedrockClient, OllamaClient)
- [x] Logger JSONL implementado (JsonlExecutionLogger + LogEvents)
- [x] 5 agentes implementados (ClarityAgent, QaAgent, TechnicalAgent, UxAgent, ComplianceAgent)
- [x] Supervisor implementado (ReviewSupervisor, AgentSelectionRules, ConflictResolver)
- [x] Endpoints API implementados (ReviewEndpoints con 5 rutas)
- [x] Prompts de agentes creados (.prompt.md)
- [x] Mock cases JSON creados (5 archivos en /mock_inputs)
- [x] Tests creados (SupervisorTests + MockCaseTests)
- [x] Build exitoso
- [x] API levantada y respondiendo en http://localhost:5050
- [x] Verificar que mock cases se cargan correctamente (5 casos cargados OK)
- [x] Probar endpoint POST /review-story (flujo completo OK, Ollama no disponible pero manejo de error correcto)
- [x] Probar endpoint GET /executions/{id}/log (11 eventos trazados OK)
- [x] Probar endpoint POST /mock-cases/mock-01/run con Ollama real (resultado verde, 2/2 agentes OK)
- [x] Swagger UI agregado (Swashbuckle)
- [x] Dashboard HTML con visualizacion de ejecuciones y logs (/dashboard)
- [x] Endpoint de log en texto plano (/executions/{id}/log/text)
- [x] Timeout de OllamaClient aumentado a 5 min
- [x] Modelo cambiado a qwen2.5:3b (mas rapido que llama3.1)
- [x] Fix ObjectDisposedException en OllamaClient
- [x] Fix serializacion de Data en logs (normalizacion a JsonElement)
- [x] Logs muestran prompts y respuestas LLM completas
- [x] Listado de ejecuciones pasadas (GET /executions + tab Historial)
- [x] Ejecucion paralela de agentes (Task.WhenAll)
- [x] Endpoint /start con progreso en vivo en dashboard
- [x] Parser robusto de respuestas LLM (repair JSON, soporte objetos con description)
- [x] Historias mock enriquecidas con ambiguedades deliberadas
- [x] Requerimiento visible en panel de resultado
- [x] Parse errors con indicador visual naranja en dashboard
- [x] Mock-02 ejecutado con qwen2.5:3b (amarillo, 3 agentes)
- [ ] Ejecutar mock cases 03-05 con Ollama real
- [ ] Validar conflictos UX vs Technical en mock-05

## Notas Tecnicas

- Un solo proveedor por ejecucion (mismo para todos los agentes)
- Arquitectura preparada para override por agente en el futuro
- Retry simple opcional ante fallo de un agente; el supervisor continua con el resto
- Si un agente falla, el resultado se marca como parcial
- El supervisor usa heuristicas de keywords para seleccion de agentes (no LLM)
- BaseReviewAgent usa raw string literals con `$$` para interpolar variables y mantener JSON literal
- Agentes parsean JSON de la respuesta LLM buscando primer `{` y ultimo `}` (tolerante a texto extra)
- Si el JSON falla, se intenta reparar con regex (comas faltantes entre strings/objetos)
- Soporta issues como `["string"]` o como `[{"description":"...","severity":"..."}]`
- Score se calcula heuristicamente segun cantidad de issues (0 issues=1.0, 1-2=0.7, 3-4=0.5, 5+=0.3)
- Status final: verde (<3 issues, sin conflictos), amarillo (3-6 issues o 1 conflicto), rojo (>6 issues o errores)

## Archivos Clave

- `src/Api/Program.cs` — DI y configuracion
- `src/Api/ReviewEndpoints.cs` — 9 endpoints REST + dashboard HTML
- `src/Api/Application/Supervisor/ReviewSupervisor.cs` — orquestador principal
- `src/Api/Application/Supervisor/AgentSelectionRules.cs` — heuristicas de seleccion
- `src/Api/Application/Supervisor/ConflictResolver.cs` — deteccion de tensiones UX vs Tech vs Compliance
- `src/Api/Application/Agents/BaseReviewAgent.cs` — logica comun de agentes (prompt, LLM call, parse)
- `src/Api/Infrastructure/LLM/ModelRouter.cs` — resuelve Bedrock u Ollama
- `src/Api/Infrastructure/Logging/JsonlExecutionLogger.cs` — persistencia JSONL + in-memory

## URLs de Acceso

- **Swagger UI**: http://127.0.0.1:5050/
- **Dashboard**: http://127.0.0.1:5050/dashboard
- **Log texto plano**: http://127.0.0.1:5050/executions/{executionId}/log/text
- **Ollama**: http://localhost:11434 (modelo: qwen2.5:3b)
