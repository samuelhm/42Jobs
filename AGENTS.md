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
| Backend | .NET 10 (ASP.NET Core) | Web API MVC, C#, EF Core, JWT |
| Base de datos | PostgreSQL 16 | Migraciones SQL en `database/migrations/` |
| Frontend | React + React Router (Vite) + TypeScript | Sin framework CSS, estilos propios |
| Package manager | pnpm | Más seguro que npm, estricto en dependencias |
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
│       ├── src.csproj              ← Proyecto .NET 10 con EF Core, Npgsql, JWT, BCrypt
│       ├── Program.cs              ← Entry point (JWT, DbContext, servicios, JSON snake_case)
│       ├── appsettings.json
│       ├── Controllers/            ← 11 controladores (Users, Categories, Profile, CRUDs...)
│       ├── Data/AppDbContext.cs    ← EF Core DbContext (Fluent API, 15 entidades)
│       ├── Models/                 ← 15 modelos C# + DTOs
│       ├── Services/               ← JWT, LinkedIn, Gemini, JobFetchOrchestrator
│       └── Utils/                  ← DatabaseUrlParser
├── database/
│   └── migrations/            ← 17 archivos SQL (categorías, keywords, jobs, perfil, user_categories...)
├── frontend/
│   ├── Dockerfile              ← Multi-stage: dev (vite) + prod (nginx + build)
│   ├── nginx.conf              ← Proxy /api -> backend, sirve estáticos de dist/
│   ├── package.json            ← react, react-router-dom, vite, chart.js
│   ├── pnpm-lock.yaml
│   ├── tsconfig.json           ← Config TypeScript para src/
│   ├── tsconfig.node.json      ← Config TypeScript para vite.config.ts
│   ├── vite.config.ts          ← Dev server con proxy /api, build output a dist/
│   ├── index.html              ← Entry point Vite
│   ├── public/
│   │   └── resources/          ← Imágenes y estáticos
│   └── src/
│       ├── main.tsx
│       ├── App.tsx             ← React Router (/ y /profile)
│       ├── pages/              ← Dashboard, Profile
│       ├── components/         ← JobCard, KeywordsChart, dialogs, etc.
│       └── index.css
└── examples/                  ← Ejemplos de respuestas de API de LinkedIn
```

## Estado actual del proyecto

1. **Base de datos:** ✅ Migraciones SQL con 17 archivos. Tablas: categorías, empresas, keywords, jobs, perfil de usuario, idiomas, certificaciones, educación, proyectos, experiencias, user_providers, user_jobs, user_categories, resumes, y tablas M2M (job_keywords, project_keywords, work_experience_keywords).
2. **Backend:** ✅ Funcional. 11 controladores REST con autenticación JWT vía cookie (`bimbajobs_auth`). EF Core con snake_case naming convention. Servicios: LinkedIn RapidAPI, Gemini (filtro + keywords), background job queue con Channel<T> para fetch de trabajos con rate-limiting de 3h.
3. **Frontend:** ⚠️ En construcción. React + React Router con Vite. Formato de respuesta `{ success, data }` con snake_case. Dev: Vite con HMR. Prod: nginx sirviendo build estático.

## Próximos pasos (visión general)

1. ✅ Backend .NET con API MVC funcional (controladores, modelos, servicios).
2. ✅ Conexión a PostgreSQL vía Entity Framework Core.
3. ✅ Autenticación de usuarios (JWT + cookies).
4. ✅ Endpoints del frontend (17/19 implementados).
5. ✅ React + React Router + Vite elegido. En construcción.
6. ✅ APIs externas (LinkedIn RapidAPI + Gemini filtro/keywords).

## Cómo trabajar

- Al iniciar una tarea, **leer `roadmap.md`** para saber en qué checkpoint estamos.
- Proponer el siguiente paso pequeño al usuario.
- Esperar confirmación antes de tocar código.
- Al completar un paso, actualizar `roadmap.md` para reflejar el progreso.
- Usar `Makefile` para levantar/bajar la infraestructura Docker (`make dev-up`, `make dev-down`).
- Para instalar dependencias del frontend, usar siempre `pnpm install` (nunca `npm install`).
