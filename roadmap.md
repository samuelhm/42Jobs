# Roadmap — BimbaJobs (bimjobsnet)

## Checkpoint actual

**Fase 1 — Backend base con Entity Framework Core**

- [x] 0.1 Setup Docker (docker-compose, Dockerfiles, Makefile)
- [x] 0.2 Migraciones SQL de base de datos (11 tablas)
- [x] 0.3 Frontend temporal vanilla JS (index.html, api.js, ui.js, profile.js)
- [x] 0.4 Proyecto .NET 10 vacío (Program.cs con "Hello World!")
- [x] 1.1 Instalar paquetes NuGet necesarios (EF Core, Npgsql, etc.)
- [x] 1.2 Crear modelos C# para cada tabla de la base de datos (14 tablas)
- [x] 1.3 Crear DbContext con configuración de relaciones y mapeo (Fluent API)
- [x] 1.4 Configurar connection string + parser de DATABASE_URL
- [x] 1.5 Registrar DbContext en Program.cs (DI container)
- [ ] 1.6 Verificar conexión a PostgreSQL y scaffolding

---

## Fases del proyecto

### Fase 0 — Setup inicial (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| Docker Compose + Dockerfiles | ✅ Listo |
| Migraciones SQL (11 archivos) | ✅ Listo |
| Frontend temporal vanilla JS | ✅ Listo |
| Proyecto .NET 9 vacío | ✅ Listo |
| AGENTS.md | ✅ Creado |
| roadmap.md | ✅ Creado |

### Fase 1 — Backend base con Entity Framework Core (EN PROGRESO 🔄)

| Checkpoint | Estado |
|------------|--------|
| Paquetes NuGet (EF Core, Npgsql) | ✅ Listo |
| Modelos C# (tablas DB) | ✅ Listo |
| DbContext + mapeo (Fluent API) | ✅ Listo |
| Connection string + DI | ✅ Listo |
| Verificar conexión | ⬚ Pendiente |

### Fase 2 — Endpoints del backend

| Checkpoint | Estado |
|------------|--------|
| GET /api/categories | ⬚ Pendiente |
| GET /api/categories/{id}/jobs | ⬚ Pendiente |
| GET /api/categories/{id}/keywords | ⬚ Pendiente |
| GET /api/keywords | ⬚ Pendiente |
| PATCH /api/keywords/{id} | ⬚ Pendiente |
| GET /api/jobs | ⬚ Pendiente |
| PATCH /api/jobs/{id}/notes | ⬚ Pendiente |
| PATCH /api/jobs/{id}/refresh | ⬚ Pendiente |
| DELETE /api/jobs/{id} | ⬚ Pendiente |
| POST /api/jobs/manual | ⬚ Pendiente |
| GET /api/profile | ⬚ Pendiente |
| PUT /api/profile | ⬚ Pendiente |
| CRUD /api/languages | ⬚ Pendiente |
| CRUD /api/certifications | ⬚ Pendiente |
| CRUD /api/education | ⬚ Pendiente |
| CRUD /api/projects | ⬚ Pendiente |
| CRUD /api/experiences | ⬚ Pendiente |
| POST /api/cv/generate/{jobId} | ⬚ Pendiente |

### Fase 3 — Autenticación de usuarios

| Checkpoint | Estado |
|------------|--------|
| Diseñar esquema de auth (JWT) | ⬚ Pendiente |
| Registro y login | ⬚ Pendiente |
| Proteger endpoints | ⬚ Pendiente |

### Fase 4 — Frontend definitivo

| Checkpoint | Estado |
|------------|--------|
| Decidir tecnología (React / Vue / JS vanilla escalable) | ⬚ Pendiente |
| Re-implementar con la tecnología elegida | ⬚ Pendiente |

### Fase 5 — APIs externas

| Checkpoint | Estado |
|------------|--------|
| GET /api/fetchLinkedinSimple | ⬚ Pendiente |
| Integración LLM para generación de CV | ⬚ Pendiente |

---

## Convención de actualización

Cada vez que se complete un checkpoint:
1. Marcarlo como `[x]` en su fase correspondiente
2. Actualizar la sección **Checkpoint actual** al inicio de este archivo
3. Si se añaden nuevos checkpoints, reflejarlos en la fase adecuada
