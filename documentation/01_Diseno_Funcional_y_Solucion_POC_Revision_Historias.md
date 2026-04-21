# POC Multiagente: Revision de Historias y Requerimientos
## Diseno Funcional y de Solucion

## 1. Objetivo

Disenar una prueba de concepto de **revision multiagente de historias de usuario y requerimientos** en la que un **agente supervisor** decide que especialistas invocar, coordina el flujo, resuelve conflictos entre hallazgos y genera una salida consolidada y accionable para producto, QA y desarrollo.

El sistema debe permitir:

- recibir una historia de usuario o requerimiento funcional/tecnico;
- decidir dinamicamente que agentes especializados vale la pena consultar;
- permitir elegir el proveedor de modelo para la ejecucion:
  - **Amazon Bedrock**, o
  - **modelo local via Ollama**;
- registrar un **log completo de conversaciones, decisiones y resultados**;
- ejecutar **casos mock de demo** para mostrar el funcionamiento de la arquitectura.

---

## 2. Problema que resuelve

En muchos equipos, las historias de usuario llegan a desarrollo con problemas de calidad como:

- ambiguedad funcional;
- criterios de aceptacion incompletos;
- riesgos tecnicos no detectados;
- impacto UX no considerado;
- implicancias de seguridad, privacidad o compliance omitidas.

La POC busca demostrar que una arquitectura multiagente mejora la revision inicial del requerimiento sin obligar a que todos los agentes participen siempre.

---

## 3. Objetivos de negocio y aprendizaje

### 3.1 Objetivos del negocio

- Mejorar la calidad inicial de historias y requerimientos.
- Reducir retrabajo por historias incompletas.
- Detectar riesgos antes de pasar a desarrollo.
- Estandarizar una revision preliminar repetible.

### 3.2 Objetivos de aprendizaje tecnico

- Practicar orquestacion multiagente.
- Implementar seleccion dinamica de agentes.
- Abstraer el uso de multiples proveedores LLM.
- Disenar trazabilidad de decisiones y conversaciones.
- Mostrar el valor del supervisor como capa de coordinacion real.

---

## 4. Alcance del POC

### 4.1 Incluye

- ingreso manual de una historia o requerimiento;
- evaluacion por multiples agentes especializados;
- seleccion de proveedor de modelo:
  - Bedrock,
  - Ollama;
- consolidacion de hallazgos por un supervisor;
- log detallado por ejecucion;
- set de casos mock para demo;
- salida estructurada con semaforo de preparacion.

### 4.2 No incluye

- integracion real con Jira/Azure DevOps;
- autenticacion corporativa;
- entrenamiento de modelos;
- base historica de aprendizaje;
- revision de archivos adjuntos complejos;
- workflow de aprobacion formal.

---

## 5. Usuarios del sistema

### 5.1 Product Owner / Analista funcional
Usa la herramienta para detectar faltantes o ambiguedades antes de refinar una historia.

### 5.2 QA
Usa la salida del sistema para derivar criterios de aceptacion y casos de prueba iniciales.

### 5.3 Lider tecnico / desarrollador
Revisa impacto tecnico, dependencias y riesgos tempranos.

### 5.4 Responsable de seguridad/compliance
Interviene en historias que tocan datos personales, trazabilidad, fraude o regulacion.

### 5.5 Equipo de demo / aprendizaje interno
Utiliza casos mock para mostrar la capacidad de seleccion dinamica de agentes y el log de decisiones.

---

## 6. Flujo funcional de alto nivel

1. El usuario ingresa una historia de usuario o requerimiento.
2. Elige proveedor de modelo:
   - **Bedrock**, o
   - **Ollama**.
3. El supervisor inspecciona la historia.
4. El supervisor decide que agentes invocar.
5. Cada agente devuelve hallazgos estructurados.
6. El supervisor resuelve conflictos y sintetiza.
7. El sistema guarda:
   - decisiones,
   - prompts/mensajes,
   - respuestas,
   - resultado final.
8. El usuario visualiza:
   - agentes invocados,
   - agentes omitidos,
   - observaciones,
   - nivel de riesgo/semaforizacion,
   - log completo de la ejecucion.

---

## 7. Agentes especializados

## 7.1 Agente de claridad funcional
**Proposito:** revisar si la historia es comprensible, especifica y accionable.

**Busca:**
- ambiguedades;
- definiciones faltantes;
- reglas de negocio implicitas;
- comportamientos no definidos.

**Ejemplos de hallazgos:**
- “No se aclara que ocurre si el email no existe.”
- “No se define si el reporte se genera online o asincronamente.”

**Cuando suele invocarse:** casi siempre, salvo cambios triviales muy acotados.

---

## 7.2 Agente de QA / Testabilidad
**Proposito:** detectar si la historia permite disenar pruebas y criterios de aceptacion.

**Busca:**
- ausencia de Given/When/Then;
- estados esperados no definidos;
- validaciones faltantes;
- cobertura de escenarios borde.

**Ejemplos de hallazgos:**
- “No estan definidos los estados de error.”
- “Faltan criterios de aceptacion para intentos invalidos.”

**Cuando suele invocarse:** en historias funcionales, backend o cambios con validaciones.

---

## 7.3 Agente tecnico
**Proposito:** analizar impacto tecnico, arquitectura, dependencias, performance y complejidad.

**Busca:**
- riesgos tecnicos;
- dependencias con otros sistemas;
- necesidad de asincronia;
- idempotencia;
- consistencia de datos;
- observabilidad.

**Ejemplos de hallazgos:**
- “Conviene cola o scheduler para reintentos.”
- “Puede haber duplicados si no se garantiza idempotencia.”

**Cuando suele invocarse:** cuando la historia tiene comportamiento backend, integraciones, estados o impacto operativo.

---

## 7.4 Agente UX
**Proposito:** revisar interaccion, mensajes, consistencia de interfaz y experiencia del usuario.

**Busca:**
- fricciones en UI;
- mensajes poco claros;
- pasos innecesarios;
- problemas de feedback visual;
- riesgos de usabilidad.

**Ejemplos de hallazgos:**
- “El mensaje no debe revelar si el email existe.”
- “Hace falta feedback de carga y confirmacion.”

**Cuando suele invocarse:** en historias con interaccion de usuario visible.

---

## 7.5 Agente de compliance / seguridad / privacidad
**Proposito:** revisar implicancias regulatorias, de seguridad o manejo de datos sensibles.

**Busca:**
- exposicion de PII;
- autorizacion insuficiente;
- auditoria/trazabilidad faltante;
- incumplimientos potenciales.

**Ejemplos de hallazgos:**
- “La descarga de datos personales requiere validacion del titular.”
- “El archivo generado deberia expirar.”

**Cuando suele invocarse:** solo cuando la historia toca datos personales, fraude, auditoria, regulacion o seguridad.

---

## 8. Rol del supervisor

El supervisor es la pieza central del sistema. No solo distribuye trabajo: toma decisiones.

### Responsabilidades

- detectar el tipo de historia;
- elegir el proveedor de modelo configurado para la ejecucion;
- decidir que agentes invocar y cuales omitir;
- controlar orden y contexto de invocacion;
- comparar hallazgos;
- resolver contradicciones;
- sintetizar una respuesta final con nivel de severidad.

### Ejemplos de decisiones del supervisor

- **Cambio textual simple:** invoca claridad funcional y, opcionalmente, UX.
- **Historia de backend:** invoca claridad, QA y tecnico; omite UX.
- **Historia con datos personales:** activa compliance ademas de otros agentes.
- **Conflicto UX vs tecnico:** prioriza factibilidad y riesgo antes de comodidad.

---

## 9. Seleccion dinamica de agentes

Uno de los objetivos de la POC es demostrar que **no siempre deben ejecutarse todos los agentes**.

### Reglas orientativas

- Si el requerimiento es un cambio minimo de texto:
  - usar **claridad**;
  - opcional **UX**;
  - omitir tecnico y compliance.
- Si hay reglas, estados o validaciones:
  - usar **claridad** y **QA**.
- Si hay reintentos, colas, integraciones o persistencia:
  - usar **tecnico**.
- Si hay pantallas, formularios o feedback al usuario:
  - usar **UX**.
- Si hay datos personales, seguridad, auditoria o regulacion:
  - usar **compliance**.

---

## 10. Seleccion del modelo: Bedrock u Ollama

La POC debe permitir elegir el motor LLM por ejecucion.

### Opcion 1: Amazon Bedrock
Uso recomendado cuando se quiere:
- mostrar integracion cloud gestionada;
- evaluar modelos empresariales;
- aprovechar configuracion centralizada.

### Opcion 2: Ollama local
Uso recomendado cuando se quiere:
- correr la demo localmente;
- evitar dependencia de red o cloud;
- experimentar rapido con modelos locales.

### Decision funcional
El usuario puede elegir:
- proveedor;
- modelo;
- temperatura;
- nivel de logging.

### Modo recomendado para POC
- una seleccion de proveedor por ejecucion;
- mismo proveedor para todos los agentes en esa corrida;
- opcionalmente dejar preparado un override por agente para futuras pruebas comparativas.

---

## 11. Logging y trazabilidad

La POC debe mostrar de forma explicita el recorrido interno del sistema.

## 11.1 Que debe registrarse

- identificador de ejecucion;
- historia enviada;
- proveedor/modelo elegido;
- agentes invocados;
- agentes omitidos y razon;
- mensajes/prompt enviados a cada agente;
- respuesta de cada agente;
- conflictos detectados;
- decision final del supervisor;
- timestamps y duracion.

## 11.2 Que se debe poder visualizar

- linea de tiempo de la ejecucion;
- arbol o secuencia de agentes;
- decision del supervisor;
- resultado final consolidado.

## 11.3 Valor del log en la demo

El log permite mostrar que:
- los agentes estan realmente diferenciados;
- el supervisor toma decisiones concretas;
- el sistema no ejecuta trabajo innecesario;
- la salida final tiene trazabilidad.

---

## 12. Resultado esperado

La salida final deberia incluir:

- semaforo:
  - **verde** = listo o casi listo;
  - **amarillo** = faltan definiciones;
  - **rojo** = riesgo alto o historia incompleta;
- resumen ejecutivo;
- agentes invocados;
- hallazgos clave;
- contradicciones o tensiones;
- recomendaciones accionables;
- proximos pasos sugeridos.

### Ejemplo de estructura

```json
{
  "executionId": "rev-001",
  "provider": "ollama",
  "model": "llama3.1",
  "status": "amarillo",
  "invokedAgents": ["clarity", "qa", "ux"],
  "skippedAgents": [
    {
      "agent": "compliance",
      "reason": "No se detectaron datos sensibles ni requisitos regulatorios"
    }
  ],
  "summary": "La historia es comprensible pero esta incompleta.",
  "issues": [
    "No se define comportamiento para email inexistente",
    "Faltan criterios de aceptacion",
    "No se aclara expiracion del enlace"
  ],
  "recommendations": [
    "Agregar reglas de expiracion",
    "Definir mensaje generico",
    "Incluir escenarios Given/When/Then"
  ]
}
```

---

## 13. Criterios de exito del POC

Se considera que el POC es exitoso si permite demostrar:

- seleccion dinamica de agentes;
- soporte para Bedrock y Ollama;
- trazabilidad completa por ejecucion;
- utilidad concreta para refinar historias;
- set de casos mock consistente para demo.

### Metricas orientativas

- tiempo de ejecucion por historia: menor a 10 segundos en entorno demo;
- estructura JSON valida en respuestas de agentes: mayor a 95%;
- al menos 4 casos mock ejecutables;
- log visible de supervisor y agentes por cada corrida.

---

## 14. Riesgos conocidos

- prompts demasiado abiertos pueden producir salidas inconsistentes;
- modelos locales pueden variar en calidad segun hardware y modelo;
- logging excesivo puede hacer ruido visual;
- si no se acota el scope, la POC puede parecer una suite completa en lugar de un demo.

---

## 15. Recomendacion de demo

Mostrar una secuencia con dificultad creciente:

1. **Cambio simple**  
   Se invocan pocos agentes.

2. **Historia funcional con UI**  
   Se ve claridad + QA + UX.

3. **Historia tecnica de backend**  
   Se ve claridad + QA + tecnico.

4. **Historia con datos personales**  
   Se ve activacion de compliance.

5. **Historia con tension UX vs tecnico**  
   Se muestra arbitraje del supervisor.

El detalle de los casos mock se documenta en el archivo `04_Casos_Mock_y_Guion_Demo.md`.
