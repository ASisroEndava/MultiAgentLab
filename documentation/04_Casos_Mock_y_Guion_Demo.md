# Casos Mock y Guion de Demo
## POC Multiagente de Revision de Historias y Requerimientos

## 1. Proposito

Este documento define los casos mock de demo para mostrar:

- seleccion dinamica de agentes;
- uso opcional de **Amazon Bedrock** o **Ollama**;
- registro del log de conversaciones y decisiones;
- consolidacion final del supervisor;
- resolucion de tensiones entre agentes.

La idea es que la demo tenga dificultad creciente y muestre claramente que **no todos los agentes se ejecutan siempre**.

---

## 2. Estructura sugerida de la demo

Para cada caso conviene mostrar:

1. historia de entrada;
2. proveedor elegido:
   - Bedrock u Ollama;
3. decision del supervisor:
   - agentes invocados,
   - agentes omitidos;
4. hallazgos por agente;
5. resultado final;
6. log resumido.

---

## 3. Caso 1 - Cambio simple de texto

### Historia
**Titulo:** Cambiar texto del boton  
**Texto:** Como usuario, quiero que el boton “Guardar” diga “Confirmar”.

### Objetivo de la demo
Mostrar un caso simple donde el supervisor evita sobreejecutar agentes.

### Agentes esperados
**Invocados:**
- clarity
- ux

**Omitidos:**
- qa
- technical
- compliance

### Motivo esperado
Se trata de un cambio de copy/UI de bajo impacto.

### Hallazgos esperados
**Clarity**
- confirmar si el cambio aplica en una pantalla o en todas;
- validar si “Confirmar” es el termino correcto del negocio.

**UX**
- revisar consistencia con el resto de la interfaz;
- verificar que el nuevo texto no genere ambiguedad.

### Resultado esperado
**Estado:** verde o amarillo bajo.

### Que mostrar del log
- selected_agents con solo clarity y ux;
- skipped_agents con razones;
- resultado final breve.

---

## 4. Caso 2 - Resetear contrasena

### Historia
**Titulo:** Resetear contrasena  
**Texto:** Como usuario, quiero poder resetear mi contrasena desde la pantalla de login para recuperar acceso a mi cuenta.

### Objetivo de la demo
Mostrar una historia funcional de UI donde no hace falta compliance fuerte ni analisis tecnico profundo.

### Agentes esperados
**Invocados:**
- clarity
- qa
- ux

**Omitidos:**
- technical (opcional)
- compliance

### Hallazgos esperados
**Clarity**
- no se aclara que pasa si el email no existe;
- no se define expiracion del enlace;
- no se define si hay limite de intentos.

**QA**
- faltan criterios de aceptacion;
- faltan escenarios invalidos;
- falta comportamiento ante enlace expirado.

**UX**
- el mensaje debe ser generico;
- hace falta feedback de confirmacion;
- debe haber retorno sencillo al login.

### Resultado esperado
**Estado:** amarillo.

### Ejemplo de consolidacion del supervisor
- la historia es util pero incompleta;
- agregar expiracion del enlace;
- definir respuesta para email inexistente;
- incluir criterios Given/When/Then.

### Que mostrar del log
- el supervisor omite compliance por ausencia de datos/regulacion explicita;
- se ve la razon de no invocar tecnico o de invocarlo en modo opcional.

---

## 5. Caso 3 - Reintentos automaticos de notificaciones

### Historia
**Titulo:** Reintentos automaticos  
**Texto:** Como sistema, necesito reintentar automaticamente el envio de notificaciones fallidas hasta 3 veces antes de marcarlas como error definitivo.

### Objetivo de la demo
Mostrar una historia backend donde UX no participa.

### Agentes esperados
**Invocados:**
- clarity
- qa
- technical

**Omitidos:**
- ux
- compliance

### Hallazgos esperados
**Clarity**
- no se define intervalo entre reintentos;
- no se aclara que errores son reintentables;
- no se define que significa error definitivo.

**QA**
- probar 1er, 2do y 3er reintento;
- probar error transitorio vs permanente;
- probar que no haya duplicados.

**Technical**
- posible uso de scheduler/cola;
- riesgo de duplicacion;
- necesidad de idempotencia;
- necesidad de metricas y monitoreo.

### Resultado esperado
**Estado:** amarillo.

### Que mostrar del log
- supervisor marca “historia de backend”;
- UX omitido con razon explicita;
- technical aporta valor diferencial.

---

## 6. Caso 4 - Descarga de datos personales

### Historia
**Titulo:** Descargar reporte personal  
**Texto:** Como cliente, quiero descargar un reporte con mis datos personales y transacciones del ultimo ano.

### Objetivo de la demo
Mostrar el disparo del agente de compliance y una salida de mayor severidad.

### Agentes esperados
**Invocados:**
- clarity
- qa
- technical
- compliance

**Omitidos:**
- ux (opcional segun implementacion)

### Hallazgos esperados
**Clarity**
- no se define formato del archivo;
- no se define si la generacion es inmediata o asincrona;
- no se aclara rango exacto configurable.

**QA**
- validar autorizacion;
- probar contenido esperado;
- probar rangos, volumen y errores.

**Technical**
- evaluar generacion asincrona;
- almacenamiento temporal;
- expiracion del archivo;
- performance con grandes volumenes.

**Compliance**
- validar identidad del titular;
- no exponer datos a terceros;
- registrar auditoria;
- definir caducidad del artefacto generado.

### Resultado esperado
**Estado:** rojo o amarillo alto, segun severidad que quieras mostrar.

### Que mostrar del log
- supervisor detecta senales de datos sensibles;
- compliance activado con razon explicita;
- posible conflicto entre rapidez de UX y seguridad/compliance.

---

## 7. Caso 5 - Editar direccion de envio

### Historia
**Titulo:** Editar direccion de envio  
**Texto:** Como usuario, quiero editar mi direccion de envio desde mi perfil.

### Objetivo de la demo
Mostrar resolucion de conflicto entre UX y tecnico.

### Agentes esperados
**Invocados:**
- clarity
- qa
- technical
- ux

**Omitidos:**
- compliance (salvo que quieras endurecer el caso)

### Hallazgos esperados
**Clarity**
- no se aclara si aplica a pedidos ya generados;
- no se define momento limite de edicion.

**QA**
- probar pedidos no despachados;
- probar pedidos en preparacion;
- probar cambios invalidos por pais/codigo postal.

**Technical**
- cambiar direccion puede impactar ordenes ya en proceso;
- hay que definir reglas por estado del pedido.

**UX**
- idealmente la edicion deberia ser simple e inmediata;
- el usuario debe recibir feedback claro sobre restricciones.

### Conflicto esperado
- UX quiere edicion rapida;
- tecnico exige restriccion por estado del pedido.

### Resolucion esperada del supervisor
- permitir edicion solo para pedidos no despachados;
- mostrar una restriccion visible en UI;
- en pedidos avanzados, sugerir contacto con soporte.

### Resultado esperado
**Estado:** amarillo.

### Que mostrar del log
- evento `conflict_detected`;
- evento `supervisor_resolution`;
- resultado consolidado final.

---

## 8. Guion sugerido para una demo de 10 minutos

## Bloque 1 - Introduccion (1 minuto)
Explicar:
- hay varios agentes especializados;
- el supervisor decide a quien consultar;
- se puede correr con Bedrock o con Ollama;
- se guarda log de toda la ejecucion.

## Bloque 2 - Caso simple (2 minutos)
Ejecutar **Caso 1**:
- mostrar que solo se invocan 2 agentes;
- destacar que el sistema evita trabajo innecesario.

## Bloque 3 - Caso funcional con UI (2 minutos)
Ejecutar **Caso 2**:
- mostrar clarity + QA + UX;
- revisar hallazgos;
- mostrar log de seleccion.

## Bloque 4 - Caso backend (2 minutos)
Ejecutar **Caso 3**:
- mostrar que UX no se usa;
- mostrar valor del agente tecnico.

## Bloque 5 - Caso sensible o conflictivo (2 minutos)
Elegir uno:
- **Caso 4** para activar compliance, o
- **Caso 5** para mostrar conflicto entre agentes.

## Bloque 6 - Cierre (1 minuto)
Resaltar:
- especializacion real;
- seleccion dinamica;
- trazabilidad;
- proveedores intercambiables;
- utilidad para refinar historias.

---

## 9. Ejemplo de salida resumida para mostrar

```json
{
  "executionId": "exec-demo-005",
  "provider": "bedrock",
  "model": "example-model",
  "status": "amarillo",
  "invokedAgents": ["clarity", "qa", "technical", "ux"],
  "skippedAgents": [
    {
      "agent": "compliance",
      "reason": "No se detectaron datos personales ni requisitos regulatorios"
    }
  ],
  "issues": [
    "No se define el punto limite para editar la direccion",
    "No se aclara el comportamiento en pedidos ya preparados"
  ],
  "conflicts": [
    "UX propone edicion inmediata; tecnico solicita restriccion por estado del pedido"
  ],
  "resolution": [
    "Permitir edicion solo para pedidos no despachados",
    "Mostrar restriccion en UI",
    "Redirigir a soporte si el pedido ya esta en preparacion"
  ]
}
```

---

## 10. Recomendacion practica

Si vas a mostrar el sistema en vivo:

- correr **Caso 1** con Ollama;
- correr **Caso 4 o 5** con Bedrock;

asi tambien demostras que la arquitectura soporta ambos proveedores sin cambiar el flujo funcional.

---

## 11. Archivos mock sugeridos

En la carpeta `mock_inputs/` de este paquete se incluyen ejemplos JSON para:

- `01_cambio_label.json`
- `02_reset_password.json`
- `03_reintentos_notificaciones.json`
- `04_descarga_datos_personales.json`
- `05_editar_direccion_envio.json`

Estos archivos sirven para correr la demo de forma repetible.
