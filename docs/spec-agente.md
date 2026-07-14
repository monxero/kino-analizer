# KinoAnalyzer — Especificación del Agente IA

**Estado:** Fase de definición cerrada. Este documento es la referencia contra la cual se evalúa cualquier cambio de alcance a partir de ahora.

**Fecha de cierre de esta fase:** julio 2026

---

## 1. Objetivo del proyecto

Aprender .NET + Blazor + Python construyendo una app funcional de análisis del Kino de Chile, para postular a trabajos que pidan: .NET, Blazor, DevExpress, Git, SQL, JSON/XML, consumo de APIs.

No es un producto que vaya a mercado. La prioridad es completar el proyecto de punta a punta, con decisiones de arquitectura defendibles en una entrevista técnica.

---

## 2. Por qué existe un agente IA en este proyecto (y hasta dónde llega)

El agente no se agrega por moda. Se agrega porque el problema real —consultar patrones estadísticos con preguntas en lenguaje natural, y armar combinaciones sugeridas con fundamento— es genuinamente ambiguo: no tiene una única forma de preguntarse, y requiere interpretar intención.

**Principio central:** el agente puede hacer todo lo que el usuario ya podría hacer manualmente en la app (consultar estadísticas, guardar/editar/eliminar combinaciones vía `/MisCombinaciones`). El agente **nunca decide por su cuenta** ni tiene permisos que un botón de la UI no tenga ya.

**Lo que el agente NO hace, por decisión explícita:**
- No predice números ni asegura resultados futuros.
- No tiene autonomía para escribir en la base de datos sin confirmación explícita del usuario.
- No decide a qué usuario pertenece una acción — ese dato viene siempre del contexto de sesión autenticado (ASP.NET Identity), nunca de lo que el modelo interpreta del mensaje.

---

## 3. Arquitectura

```
Usuario → Blazor (ChatBot.razor) → AgentService.cs (.NET)
        → HTTP POST → FastAPI (Python) → Gemini (function calling)
        → analyzer.py → SQLite (../Data/kino.db)
```

**Por qué el agente vive en Python y no en .NET:** no existe SDK oficial de Gemini para .NET con soporte maduro de function calling. En Python sí, y ya estaba probado en el proyecto de referencia KINOBOT2. Se resolvió con una arquitectura de microservicios: .NET no sabe cómo piensa el agente, solo le manda una pregunta por HTTP y recibe una respuesta.

**Duplicación de lógica estadística (decisión consciente, no accidental):**
`analyzer.py` reimplementa consultas similares a las de `KinoStatsService.cs` (C#), en vez de que Python le pida los datos a .NET por HTTP. Se eligió así por simplicidad y porque el agente necesita respuestas rápidas sin depender de que .NET esté disponible. **Costo aceptado:** si se cambia una fórmula estadística, hay que actualizarla en ambos lugares. `KinoStatsService.cs` sigue siendo necesario — alimenta la página `/Estadisticas` directamente y no se toca.

---

## 4. Seguridad

### 4.1 Servicio a servicio (.NET ↔ Python)
- **Ahora:** FastAPI escucha solo en `127.0.0.1`, desarrollo local, sin autenticación entre servicios.
- **Diferido a propósito:** agregar una API key compartida entre .NET y Python cuando esto salga de la máquina local. No aporta protección real hoy (todo corre en el mismo entorno) y el diseño actual no bloquea agregarla después — es una línea de configuración, no un rediseño.

### 4.2 Identidad del usuario en acciones de escritura
El `user_id` **nunca** es un parámetro que Gemini elige o propone. Viaja desde la sesión autenticada de Blazor (ASP.NET Identity) hasta Python como dato de sistema, y se inyecta en la función de escritura después de que el modelo decide llamarla — no antes, no como parte del schema que el modelo controla. Esto evita que el modelo pueda ser manipulado (vía prompt injection) para asociar una acción al usuario equivocado.

### 4.3 Acciones de escritura requieren confirmación explícita (Opción C)
Ninguna función de escritura (`guardar_combinacion_favorita`) se ejecuta directo. El agente arma la propuesta de acción, la muestra en el chat como un mensaje con botones de confirmar/cancelar (sin salir del flujo conversacional, sin modal), y solo se ejecuta si el usuario confirma. Si algo se guarda mal, se corrige con el CRUD que ya existe en `/MisCombinaciones` — no se construye ninguna herramienta especial de "deshacer", porque el agente nunca tuvo un poder que la UI no tuviera ya.

### 4.4 Advertencia de responsabilidad (estadística vs. predicción)
El Kino es aleatorio; los sorteos son eventos independientes. Ningún análisis histórico predice el futuro. Esto se comunica en dos capas:
- Aviso fijo y breve en la interfaz del chat (una vez, no repetitivo).
- Tono integrado en las respuestas del agente cuando entrega combinaciones sugeridas (system prompt de `agent.py`), enmarcando todo como "patrón histórico", nunca como predicción.

---

## 5. Funciones de `analyzer.py`

| Función | Tipo | Parámetros | Devuelve |
|---|---|---|---|
| `obtener_ultimos_sorteos` | Lectura | `n: int = 10` | Lista de sorteos (fecha + números) |
| `frecuencia_numeros` | Lectura | ninguno | `{número: veces}` |
| `numeros_atrasados` | Lectura | ninguno | Lista `{número, sorteos_sin_salir}` |
| `numeros_juntos` | Lectura | `top_n: int = 10` | Pares `{a, b, veces_juntos}` (co-ocurrencia en el mismo sorteo) |
| `buscar_numero_en_sorteos` | Lectura | `numero: int`, `cantidad_sorteos: int` (sin default — el agente pregunta si falta) | Sorteos donde apareció |
| `distribucion_rangos` | Lectura | ninguno | Conteo bajo/medio/alto |
| `suma_por_sorteo` | Lectura | ninguno | Distribución de sumas totales por sorteo |
| `balance_pares_impares` | Lectura | ninguno | Proporción histórica par/impar |
| `analizar_combinacion` | Lectura | `numeros: list[int]` | `coincidencia_exacta`, `coincidencias_parciales`, `frecuencias_individuales` |
| `generar_combinacion_sugerida` | Lectura | `cantidad_numeros: int`, `criterio: str` | Combinación armada por código (no por el LLM) + datos que la fundamentan |
| `guardar_combinacion_favorita` | **Escritura, con confirmación** | `numeros: list[int]`, `nombre: str` | Propuesta de acción — no ejecuta directo |

**Regla de diseño aplicada:** todo lo que requiere exactitud matemática (cantidad de números, sin duplicados, rango válido) lo garantiza código Python determinístico, nunca la generación libre del LLM. El modelo elige criterios e interpreta intención; el código calcula y valida.

---

## 6. Explícitamente fuera de alcance (no se construye)

- Números "recomendados para jugar" o predicciones del próximo sorteo sin base estadística real.
- Autonomía del agente para ejecutar escrituras sin confirmación.
- Herramientas de "deshacer" acciones del agente (se resuelve con el CRUD existente).

---

## 7. Diferido a propósito (con razón explícita, no olvidado)

| Ítem | Por qué se difiere | Por qué no bloquea el diseño actual |
|---|---|---|
| API key entre .NET y Python | No aporta protección en desarrollo local | Se agrega como validación de header, sin rediseño |
| Escuchar solo en `127.0.0.1` → producción | No aplica aún, no hay deploy | Cambio de configuración de `uvicorn`, no de arquitectura |
| DevExpress (grilla en Dashboard) | No afecta al agente ni a la arquitectura | Feature de UI independiente |
| Exportar estadísticas en XML | No afecta al agente ni a la arquitectura | Feature de UI independiente |
| Página de perfil de usuario | No afecta al agente ni a la arquitectura | Feature de UI independiente |
| `test_aleatoriedad()` (chi-cuadrado) | Extensión opcional, no core | Se agrega como función nueva en `analyzer.py` sin tocar las demás |

---

## 8. Riesgos técnicos conocidos (identificados antes de programar)

| Riesgo | Mitigación decidida |
|---|---|
| Concurrencia de SQLite entre dos procesos (.NET vía EF Core y Python vía `sqlite3`) accediendo al mismo `kino.db` | A confirmar si EF Core ya usa modo WAL (probable, ya que `.gitignore` ignora `kino.db-wal`/`kino.db-shm`). Si no, activarlo — reduce bloqueos entre procesos. |
| Una consulta SQLite lenta dentro de una ruta `async def` de FastAPI podría congelar todo el servidor Python, no solo esa consulta | Las rutas de `main.py` se declaran como `def` normal, no `async def`. FastAPI las corre automáticamente en un hilo aparte, evitando el bloqueo, sin necesidad de manejar `async`/`await`. |

## 9. Regla de uso de este documento

Cualquier idea nueva que surja durante el desarrollo se compara primero contra este documento:

- **Si encaja dentro de lo ya definido** → se implementa.
- **Si no encaja** → se anota en la sección 7 como diferido, o se discute como una decisión de alcance nueva y consciente — nunca se agrega directo al código a mitad de una tarea distinta.

**Este documento se edita cuando cambia una decisión, no cuando cambia una implementación.** Corregir un bug no requiere editarlo; agregar, quitar o mover el alcance de una función sí.

---

## 10. Reprioridad MVP (oferta laboral real encontrada)

Se encontró una oferta laboral real que pide exactamente: .NET, Blazor, XML, DevExpress, Git, SQL, consumo de APIs. Se reordena el trabajo restante para cubrir esos requisitos lo antes posible, sin abandonar lo ya definido — solo se reordena la secuencia.

**Fase A (en curso) — Circuito mínimo Python↔.NET, no las 11 funciones:**
`obtener_ultimos_sorteos` (lista), `frecuencia_numeros`, `buscar_numero_en_sorteos`. Suficiente para demostrar el patrón completo de consumo de API y function calling con más de una herramienta.

**Fase B — Requisitos explícitos de la oferta, hoy en cero:**
DevExpress (grilla en Dashboard) — completado funcionalmente (datos reales, filtro, paginación funcionando). Pendiente de pulido: conflicto visual CSS entre Bootstrap y el tema Fluent de DevExpress (Shadow DOM), afecta el popup del calendario del filtro. Causa identificada, no bloqueante, diferido por bajo retorno vs. tiempo de resolución. Exportar estadísticas en XML.

**Fase C — Backlog opcional, no comprometido:**
Las 8 funciones restantes de `analyzer.py`, la función de escritura con confirmación (`guardar_combinacion_favorita`), página de perfil de usuario. Se retoma solo si sobra tiempo antes de postular — no es requisito de la oferta real encontrada (ver más abajo), que no menciona IA, agentes ni Python en ningún punto.

**Oferta real usada como referencia (2026-07-13):** Desarrollador Junior de Sistemas — requiere: lenguajes de programación, interfaces, modelamiento de datos, consumo de APIs/servicios, SQL, JSON y XML, Visual Studio .NET, Blazor, DevExpress, Git, seguridad de la información básica, usabilidad/UX básica. No menciona IA/agentes/Python — el agente es un diferenciador propio del candidato, no un requisito.

## 11. Historial de decisiones

- **2026-07-09:** definición inicial cerrada. Arquitectura microservicio .NET↔Python, 11 funciones de `analyzer.py` definidas, seguridad de escritura con confirmación (Opción C), autenticación entre servicios diferida a propósito.
- **2026-07-12:** agregada sección de riesgos técnicos conocidos (concurrencia SQLite, bloqueo async en FastAPI). Confirmada versión real de `google-genai` instalada (2.10.0). Se establece como práctica fija: antes de cerrar cualquier definición nueva, se investigan activamente riesgos técnicos del stack elegido, en vez de esperar a que aparezcan a mitad de desarrollo. `obtener_ultimos_sorteos` implementada y probada.
- **2026-07-13:** se encuentra oferta laboral real con los requisitos exactos del proyecto. Se reprioriza el trabajo restante en Fases A/B/C (ver sección 10) para cubrir primero los requisitos explícitos de la oferta, dejando el resto de las funciones del agente como perfeccionamiento posterior.
- **2026-07-13:** Fase A completada — circuito Python↔.NET funcionando de punta a punta (`analyzer.py` con 3 funciones, `agent.py` con function calling, `main.py` con `/chat`, `AgentService.cs` consumiendo el microservicio, `ChatBot.razor` con historial corregido). Probado desde el navegador con preguntas reales. Commit `b80e2bb`. Siguiente paso: Fase B (DevExpress, XML).