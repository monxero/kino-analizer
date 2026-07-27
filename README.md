# 🎱 KinoAnalyzer

Aplicación web para analizar estadísticamente los datos históricos del Kino de Chile, con un agente de IA especializado que responde preguntas en lenguaje natural usando datos reales del historial de sorteos.

Proyecto desarrollado como práctica para consolidar conocimientos en .NET, Blazor y Python, cubriendo específicamente: .NET, Blazor, DevExpress, Git, SQL, JSON/XML y consumo de APIs.

---

## Funcionalidades

- **Dashboard** con los últimos sorteos y una grilla completa (DevExpress) de todo el historial, con filtro y paginación.
- **Estadísticas**: frecuencias, números atrasados, distribución por rangos.
- **Scraper** que obtiene resultados actualizados desde la fuente oficial y los guarda en la base de datos.
- **Gestión de usuarios** (ASP.NET Identity) para guardar combinaciones favoritas.
- **Exportación de combinaciones en XML.**
- **Agente conversacional** (Gemini + function calling) que responde preguntas estadísticas reales sobre el historial, sin necesidad de enviarle todo el contexto de antemano.

### Herramientas del agente disponibles hoy

- Consultar los últimos N sorteos.
- Frecuencia histórica de cada número.
- En qué sorteos recientes apareció un número específico.

### Pendiente / backlog (no bloqueante, documentado en `docs/spec-agente.md`)

- Herramientas adicionales de análisis (números atrasados, pares frecuentes, distribución por rangos, análisis de combinación, generación de combinaciones sugeridas).
- Capacidad de que el agente guarde combinaciones favoritas a pedido del usuario (con confirmación explícita).
- Ajuste visual: conflicto de estilos CSS entre Bootstrap y el tema de DevExpress en los controles de filtro de la grilla.

---

## Arquitectura

```
Usuario → Blazor (ChatBot.razor) → AgentService.cs (.NET)
        → HTTP POST → FastAPI (Python) → Gemini (function calling)
        → analyzer.py → SQLite (Data/kino.db)
```

El agente conversacional vive en un microservicio Python separado, porque el SDK oficial de Gemini para .NET no tiene soporte maduro de function calling, mientras que el SDK de Python sí. `AgentService.cs` no sabe cómo piensa el agente — solo le hace una petición HTTP y recibe una respuesta, igual que si consumiera cualquier API externa.

Ambos procesos (.NET y Python) acceden directamente a la misma base de datos SQLite, cada uno con su propia lógica de consulta — una decisión consciente de diseño (ver `docs/spec-agente.md`, sección 3).

---

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Frontend/Backend | ASP.NET Core 8, Razor Pages, Blazor Server |
| Componentes UI | DevExpress Blazor (`DxGrid`) |
| Base de datos | SQLite + Entity Framework Core 8 |
| Autenticación | ASP.NET Identity |
| Scraping | HtmlAgilityPack |
| Agente IA | Python, FastAPI, `google-genai` (Gemini `2.5-flash`, function calling) |
| Control de versiones | Git + GitHub |

---

## Instalación y ejecución

Este proyecto requiere **dos procesos corriendo en paralelo** (arquitectura de microservicios): la app .NET y el microservicio Python del agente.

### 1. Base de datos y app .NET

```bash
git clone git@github.com:monxero/kino-analizer.git
cd kino-analyzer
dotnet user-secrets set "Gemini:ApiKey" "TU_API_KEY"   # si aplica
dotnet run
```

### 2. Microservicio del agente (Python)

En otra terminal:

```bash
cd AgentePython
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt   # o: pip install fastapi uvicorn google-genai python-dotenv
```

Creá un archivo `.env` dentro de `AgentePython/` con tu clave de Gemini:

```
GEMINI_API_KEY=tu_clave_aca
```

Y levantá el servidor:

```bash
uvicorn main:app --reload
```

La app .NET va a estar disponible en `http://localhost:5000` (o el puerto configurado), y el microservicio del agente en `http://localhost:8000`. El chat solo funciona si ambos procesos están corriendo al mismo tiempo.

---

## Documentación técnica

El archivo [`docs/spec-agente.md`](docs/spec-agente.md) contiene la especificación completa del proyecto: arquitectura, decisiones de seguridad, funciones del agente (implementadas y pendientes), riesgos técnicos conocidos y el historial de decisiones tomadas durante el desarrollo.

---

## Nota sobre licencias

Los componentes DevExpress se usan bajo su versión de evaluación (trial de 30 días) — es esperable ver un aviso de "evaluation purposes only" al compilar y en la interfaz.