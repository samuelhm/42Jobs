# Roadmap — 42jobs

## Checkpoint actual

**Fase 6 — Refinamiento y estabilidad (en curso)**

---

## Fases del proyecto

### Fase 0 — Setup inicial (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| Docker Compose + Dockerfiles | ✅ |
| Migraciones SQL (32 archivos) | ✅ |
| Proyecto .NET 10 vacío | ✅ |

### Fase 1 — Backend base con Entity Framework Core (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| Paquetes NuGet (EF Core, Npgsql, BCrypt, JWT) | ✅ |
| Modelos C# (21 entidades + 7 DTOs) | ✅ |
| DbContext + mapeo Fluent API | ✅ |
| Connection string + DatabaseUrlParser | ✅ |
| EFCore.NamingConventions (snake_case) | ✅ |

### Fase 2 — Endpoints del backend (COMPLETADA ✅)

14 controladores, 78 endpoints:

| Controlador | Endpoints |
|-------------|-----------|
| UsersController | POST, GET, PATCH, DELETE, login, logout, register, me |
| CategoriesController | POST create, GET all, GET {id}/jobs, GET {id}/keywords, GET available, DELETE unfollow |
| ProfileController | GET, PUT, GET preferences |
| LanguagesController | GET, POST, PUT, DELETE |
| CertificationsController | GET, POST, PUT, DELETE |
| EducationController | GET, POST, PUT, DELETE, import-linkedin |
| ProjectsController | GET, POST, PUT, DELETE, import-github |
| ExperiencesController | GET, POST, PUT, DELETE, import-linkedin |
| KeywordsController | GET all, PATCH status |
| JobsController | PATCH title, PATCH notes, DELETE |
| TrackingController | GET all, PATCH status |
| ResumesController | POST generate, POST regenerate, GET by job, GET templates |
| AdminController | AI services CRUD, models CRUD, prompts CRUD, templates CRUD, job providers CRUD, dedup keywords, clean keywords, categories CRUD, logs |
| HealthController | GET /, GET /db-test |

### Fase 3 — Autenticación de usuarios (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| JWT + cookie auth (`42jobs_auth`) | ✅ |
| Registro (BCrypt hash, @student.42barcelona.com restriction) | ✅ |
| Login (JWT generation) | ✅ |
| Logout (cookie deletion) | ✅ |
| [Authorize] en todos los controladores | ✅ |
| GetUserId() desde JWT claims | ✅ |

### Fase 4 — Frontend definitivo (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| React 19 + React Router 7 + Vite | ✅ |
| Data router (`createBrowserRouter`) con loaders/actions | ✅ |
| Barrel pattern en todas las carpetas | ✅ |
| CSS modular (13 archivos por responsabilidad) | ✅ |
| Dockerfile multi-stage (dev: vite, prod: nginx) | ✅ |
| Auth flow (login, register, RequireAuth) | ✅ |
| Dashboard (Offers, Tracking, Keywords, CategoriesBar) | ✅ |
| Profile (info, education, experiences, projects, languages, certs, LinkedIn import, GitHub import) | ✅ |
| CV generation + preview + template selection | ✅ |
| Admin panel (8 páginas) | ✅ |
| AI readiness modal (AiNotConfiguredModal) | ✅ |

### Fase 5 — APIs externas y AI (COMPLETADA ✅)

| Checkpoint | Estado |
|------------|--------|
| LinkedIn RapidAPI (búsqueda + detalles con rate limiting 8/min) | ✅ |
| AI filtering (filter_jobs — relevancia + junior-friendly) | ✅ |
| AI keyword extraction (extract_keywords — skills + company type) | ✅ |
| AI CV generation (cv_generation — ATS-optimized) | ✅ |
| AI GitHub analysis (analyze_github) | ✅ |
| AI LinkedIn parsing (parse_experience, parse_education) | ✅ |
| AI dedup + clean keywords | ✅ |
| Background job queue (Channel<T> via JobFetchService) | ✅ |
| Scheduler (8:00, 12:00, 16:00, 20:00 UTC — por location × category) | ✅ |
| Pipeline de reintentos (provider 10, AI filter 3, AI keywords 3, paginación 3) | ✅ |
| Validación de readiness (AiReadinessService) en todos los endpoints | ✅ |
| 3 providers AI: Google Gemini, OpenAI, DeepSeek | ✅ |
| Pluggable architecture (IAiProvider, IJobProvider) | ✅ |
| API key encryption at rest (EncryptionService + Data Protection) | ✅ |

### Fase 6 — Refinamiento y estabilidad (EN CURSO ⬚)

| Checkpoint | Estado |
|------------|--------|
| Location-based job fetching & filtering | ✅ |
| Auto-update sin intervención del usuario | ✅ |
| Proactive AI readiness validation | ✅ |
| Resiliencia con reintentos en toda la pipeline | ✅ |
| DeepSeek como provider por defecto | ✅ |
| Google free-tier desactivado por defecto | ✅ |
| Migraciones autocontenidas (no dependen de production-patches) | ✅ |
| Detección de ofertas cerradas (sin scraping) | ⬚ |

**Nota sobre detección de ofertas cerradas:** La API de LinkedIn RapidAPI no expone ningún campo que indique si una oferta ya no acepta solicitudes (ni en search ni en getDetails). Se necesita una alternativa sin scraping. Opciones a explorar:

- **Heurística por antigüedad:** Si `posted_date > 30 días`, marcarla como probablemente cerrada y moverla a `discarded_jobs`. Simple pero inexacta.
- **Verificación vía jobUrl con HEAD/GET:** Hacer un GET ligero al `job_url` y revisar si el status code es 404 o si el body contiene "no longer accepting". Esto es scraping mínimo, pero más fiable.
- **Re-fetch periódico con getDetails:** Si el endpoint `/job/{id}` devuelve `success: false` o datos vacíos para una oferta antes válida, asumir que fue retirada. Esto aprovecha la API existente sin scraping externo.

---

## Servicios implementados

| Servicio | Archivo | Tipo |
|----------|---------|------|
| AiReadinessService | `Services/AiReadinessService.cs` | Scoped |
| AiService | `Services/Ai/AiService.cs` (+ 9 partials) | Scoped |
| JobFetchService | `Services/Jobs/JobFetchService.cs` (+ 2 partials) | Singleton / BackgroundService |
| GithubImportService | `Services/GithubImportService.cs` | Singleton / BackgroundService |
| JwtService | `Services/JwtService.cs` | Singleton |
| EncryptionService | `Services/EncryptionService.cs` | Singleton |
| AdminLogService | `Services/AdminLogService.cs` | Singleton |
| GeminiProvider | `Services/Ai/Providers/Gemini/` | Singleton |
| OpenAiProvider | `Services/Ai/Providers/OpenAI/` | Singleton |
| DeepSeekProvider | `Services/Ai/Providers/DeepSeek/` | Singleton |
| LinkedInRapidApiProvider | `Services/Jobs/Providers/LinkedIn/RapidApi/` | Singleton |

---

## Próximos pasos

1. Email notifications for new matching jobs
2. More job providers (InfoJobs, Indeed)
3. UI tests with Playwright
4. Light mode
5. Public demo instance
