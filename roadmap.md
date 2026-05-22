# Roadmap — BimbaJobs (bimjobsnet)

## Checkpoint actual

**Fase 2 — Endpoints del backend (casi completo) y Fase 5 — APIs externas (completo)**

---

## Fases del proyecto

### Fase 0 — Setup inicial (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| Docker Compose + Dockerfiles | ✅ |
| Migraciones SQL (17 archivos) | ✅ |
| Frontend temporal vanilla JS | ✅ |
| Proyecto .NET 10 vacío | ✅ |

### Fase 1 — Backend base con Entity Framework Core (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| Paquetes NuGet (EF Core, Npgsql, BCrypt, JWT) | ✅ |
| Modelos C# (15 entidades + DTOs) | ✅ |
| DbContext + mapeo Fluent API (15 entidades) | ✅ |
| Connection string + DatabaseUrlParser | ✅ |
| EFCore.NamingConventions (snake_case) | ✅ |
| Migraciones aplicadas y verificadas | ✅ |

### Fase 2 — Endpoints del backend (17/25 completados)

| Endpoint | Estado | Controlador |
|----------|--------|-------------|
| POST /api/users | ✅ | UsersController.Create |
| POST /api/users/login | ✅ | UsersController.Login |
| POST /api/users/logout | ✅ | UsersController.Logout |
| GET /api/users/{id} | ✅ | UsersController.Get |
| PATCH /api/users/{id} | ✅ | UsersController.Patch |
| DELETE /api/users/{id} | ✅ | UsersController.Delete |
| POST /api/categories | ✅ | CategoriesController.Create |
| DELETE /api/categories/{id}/follow | ✅ | CategoriesController.Unfollow |
| POST /api/categories/{id}/fetch | ✅ | CategoriesController.FetchJobs |
| GET /api/categories/{id}/fetch/{jobId} | ✅ | CategoriesController.GetFetchStatus |
| GET /api/categories | ⬚ | (list with job counts) |
| GET /api/categories/{id}/jobs | ⬚ | (jobs for category) |
| GET /api/categories/{id}/keywords | ⬚ | (keywords for category) |
| GET /api/profile | ✅ | ProfileController.Get |
| PUT /api/profile | ✅ | ProfileController.Update |
| GET /api/languages | ✅ | LanguagesController.GetAll |
| POST /api/languages | ✅ | LanguagesController.Create |
| PUT /api/languages/{id} | ✅ | LanguagesController.Update |
| DELETE /api/languages/{id} | ✅ | LanguagesController.Delete |
| GET /api/certifications | ✅ | CertificationsController.GetAll |
| POST /api/certifications | ✅ | CertificationsController.Create |
| PUT /api/certifications/{id} | ✅ | CertificationsController.Update |
| DELETE /api/certifications/{id} | ✅ | CertificationsController.Delete |
| GET /api/education | ✅ | EducationController.GetAll |
| POST /api/education | ✅ | EducationController.Create |
| PUT /api/education/{id} | ✅ | EducationController.Update |
| DELETE /api/education/{id} | ✅ | EducationController.Delete |
| GET /api/projects | ✅ | ProjectsController.GetAll |
| POST /api/projects | ✅ | ProjectsController.Create |
| PUT /api/projects/{id} | ✅ | ProjectsController.Update |
| DELETE /api/projects/{id} | ✅ | ProjectsController.Delete |
| GET /api/experiences | ✅ | ExperiencesController.GetAll |
| POST /api/experiences | ✅ | ExperiencesController.Create |
| PUT /api/experiences/{id} | ✅ | ExperiencesController.Update |
| DELETE /api/experiences/{id} | ✅ | ExperiencesController.Delete |
| GET /api/keywords | ✅ | KeywordsController.GetAll |
| PATCH /api/keywords/{id} | ✅ | KeywordsController.UpdateStatus |
| PATCH /api/jobs/{id}/notes | ⬚ | (update job notes) |
| DELETE /api/jobs/{id} | ⬚ | (delete job) |
| PATCH /api/jobs/{id}/refresh | ⬚ | (re-fetch job details) |
| POST /api/jobs/manual | ⬚ | (create manual job) |
| POST /api/cv/generate/{jobId} | ⬚ | (AI CV generation) |

### Fase 3 — Autenticación de usuarios (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| JWT + cookie auth (`bimbajobs_auth`) | ✅ |
| Registro (BCrypt hash) | ✅ |
| Login (JWT generation) | ✅ |
| Logout (cookie deletion) | ✅ |
| [Authorize] en todos los controladores | ✅ |
| GetUserId() desde JWT claims | ✅ |

### Fase 4 — Frontend definitivo (EN CONSTRUCCIÓN ⬚)

| Checkpoint | Estado |
|------------|--------|
| Decidir tecnología (React + React Router + Vite) | ✅ |
| Scaffold Vite + React + React Router | ✅ |
| Configurar Dockerfile (dev: vite, prod: nginx) | ✅ |
| docker-compose override (HMR con volumes) | ✅ |
| Portar estilos CSS | ⬚ |
| Portar componentes (Dashboard, Profile, dialogs) | ⬚ |

### Fase 5 — APIs externas (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| LinkedIn RapidAPI (búsqueda + detalles) | ✅ LinkedInApiService |
| Gemini LLM (filtro relevancia + junior) | ✅ GeminiService |
| Gemini LLM (extracción keywords) | ✅ GeminiService |
| Background job queue (Channel<T>) | ✅ JobFetchOrchestrator |
| Procesamiento paralelo (SemaphoreSlim ×3) | ✅ |
| Rate limiting (3 horas por categoría) | ✅ last_fetched_at |
| Progress polling (GET status) | ✅ |
| Auto-follow al hacer fetch | ✅ |
| Categorías compartidas (sin duplicar API calls) | ✅ |
| OpenAI CV generation | ⬚ |

---

## Resumen de controladores implementados

| # | Controlador | Archivo | Métodos |
|---|-------------|---------|---------|
| 1 | HealthController | `Controllers/HealthController.cs` | GET /, GET /db-test |
| 2 | UsersController (partial) | `Controllers/Users/*.cs` (7 files) | POST, GET, PATCH, DELETE, login, logout |
| 3 | CategoriesController | `Controllers/CategoriesController.cs` | POST, DELETE follow, POST fetch, GET status |
| 4 | ProfileController | `Controllers/ProfileController.cs` | GET, PUT |
| 5 | LanguagesController | `Controllers/LanguagesController.cs` | GET, POST, PUT, DELETE |
| 6 | CertificationsController | `Controllers/CertificationsController.cs` | GET, POST, PUT, DELETE |
| 7 | EducationController | `Controllers/EducationController.cs` | GET, POST, PUT, DELETE |
| 8 | ProjectsController | `Controllers/ProjectsController.cs` | GET, POST, PUT, DELETE |
| 9 | ExperiencesController | `Controllers/ExperiencesController.cs` | GET, POST, PUT, DELETE |
| 10 | KeywordsController | `Controllers/KeywordsController.cs` | GET, PATCH |

## Servicios implementados

| # | Servicio | Archivo | Tipo |
|---|----------|---------|------|
| 1 | JwtService | `Services/JwtService.cs` | Singleton |
| 2 | LinkedInApiService | `Services/LinkedInApiService.cs` | Typed HttpClient |
| 3 | GeminiService | `Services/GeminiService.cs` | Typed HttpClient |
| 4 | JobFetchOrchestrator | `Services/JobFetchOrchestrator.cs` | BackgroundService |

## Próximos pasos

1. Completar endpoints de jobs (notes, refresh, delete, manual)
2. Implementar GET /api/categories (con job counts)
3. Implementar GET /api/categories/{id}/jobs y /keywords
4. Implementar POST /api/cv/generate/{jobId}
5. Decidir y construir el frontend definitivo
