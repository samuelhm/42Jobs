# AGENTS.md — BimJobsNet (bimjobsnet)

## Propósito del proyecto

Este es un proyecto **personal de aprendizaje**. El objetivo principal no es entregar rápido, sino **entender cada línea de código** que se escribe y cada decisión que se toma.

## Reglas de oro para la IA

1. **Nunca hacer cambios grandes de golpe.** Todo cambio debe ser pequeño, atómico y explicado.
2. **Preguntar antes de implementar.** Antes de escribir una sola línea de código, la IA debe explicar qué va a hacer, cómo lo va a hacer y por qué. El usuario debe dar el visto bueno.
3. **Explicar cada cambio.** Después de cada modificación, la IA debe explicar qué se ha hecho de forma clara y concisa.
4. **El usuario debe entenderlo todo.** Si algo es complejo, se desglosa. Si el usuario no entiende algo, la IA debe ser capaz de explicarlo con otros ejemplos o analogías.
5. **Nada de código mágico ni patrones oscuros.** Código limpio, legible, bien estructurado y comentado solo cuando sea necesario para clarificar algo no obvio.
6. **Siempre consultar AGENTS.md y roadmap.md** al comenzar una sesión para saber en qué punto del proyecto estamos.

## Stack tecnológico

| Capa | Tecnología | Detalles |
|------|-----------|----------|
| Backend | .NET 9 (ASP.NET Core) | Web API MVC, C# |
| Base de datos | PostgreSQL 16 | Migraciones SQL en `database/migrations/` |
| Frontend actual | HTML/CSS/JS vanilla | Temporal, en `frontend/public/` |
| Frontend futuro | React, Vue o JS vanilla escalable | A decidir por el usuario |
| Infraestructura | Docker + Docker Compose | Dev y prod con override files |
| APIs externas | LinkedIn RapidAPI, Google Gemini / OpenAI | Para búsqueda de empleos y generación de CV |

## Estructura del proyecto

```
bimjobsnet/
├── AGENTS.md              ← Este archivo
├── roadmap.md             ← Punto actual del proyecto y siguientes pasos
├── Makefile               ← Orquestación (dev-up, prod-up, etc.)
├── docker-compose.yml     ← Base (db, backend, frontend)
├── docker-compose.override.yml ← Overrides de desarrollo
├── docker-compose.prod.yml     ← Overrides de producción
├── backend/
│   ├── Dockerfile
│   └── src/
│       ├── src.csproj         ← Proyecto .NET 9 vacío
│       ├── Program.cs         ← Entry point (solo "Hello World!")
│       └── appsettings.json
├── database/
│   └── migrations/            ← 11 archivos SQL (categorías, keywords, jobs, perfil...)
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── package.json
│   └── public/
│       ├── index.html
│       └── js/                ← api.js, ui.js, profile.js (frontend temporal)
└── examples/                  ← Ejemplos de respuestas de API de LinkedIn
```

## Estado actual del proyecto

1. **Base de datos:** ✅ Las migraciones SQL están listas y son funcionales. Las tablas cubren: categorías, empresas, keywords, jobs, perfil de usuario, idiomas, certificaciones, educación, proyectos y experiencias laborales.
2. **Backend:** ❌ Vacío. Solo tiene `Hello World!` en `Program.cs`. No hay controladores, modelos, Entity Framework ni autenticación.
3. **Frontend:** ⚠️ Temporal. Es una SPA en vanilla JS que asume que existen ~25 endpoints REST que aún no están implementados en el backend. El usuario quiere reemplazarlo por React, Vue o JS vanilla escalable (a decidir).

## Próximos pasos (visión general)

1. Levantar el backend .NET con una API MVC funcional (controladores, modelos, servicios).
2. Conectar el backend a PostgreSQL vía Entity Framework Core.
3. Implementar autenticación de usuarios.
4. Implementar los endpoints que espera el frontend.
5. Decidir y construir el frontend definitivo (React / Vue / JS vanilla escalable).
6. Integrar APIs externas (LinkedIn, LLMs).

## Cómo trabajar

- Al iniciar una tarea, **leer `roadmap.md`** para saber en qué checkpoint estamos.
- Proponer el siguiente paso pequeño al usuario.
- Esperar confirmación antes de tocar código.
- Al completar un paso, actualizar `roadmap.md` para reflejar el progreso.
- Usar `Makefile` para levantar/bajar la infraestructura Docker (`make dev-up`, `make dev-down`).
