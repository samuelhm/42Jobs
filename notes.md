# Notas técnicas — 42jobs

## Categorías huérfanas

El scheduler (`JobFetchService.RunSchedulerAsync`) busca jobs para **todas** las categorías existentes, independientemente de si tienen suscriptores o no. Esto significa que:

- Si un usuario crea una categoría, luego hace unfollow, y nadie más la sigue, el scheduler seguirá gastando créditos de LinkedIn RapidAPI buscando jobs para esa categoría cada 4 horas.
- La solución definitiva sería filtrar solo categorías con al menos un `UserCategory` asociado, o implementar un sistema de "cleanup" que elimine categorías huérfanas tras un tiempo sin suscriptores.
- **Decisión actual:** se deja así por simplicidad. Si el gasto de créditos se vuelve un problema, filtrar por `UserCategories.Count > 0` en `FetchAllCategoriesWithTokenAsync`.

## Error "Unexpected token '<' ... is not valid JSON" durante deploys

Cuando el backend se reinicia, nginx devuelve HTML de error 502/503 para las peticiones proxy `/api/`. El frontend llama `res.json()` sobre ese HTML, lo que produce `SyntaxError`. React Router lo captura en `ErrorPage` y muestra "Oops!".

### Solución pendiente (2 capas)

1. **nginx.conf** — devolver JSON en lugar de HTML para errores del proxy `/api/`:
   ```nginx
   location /api/ {
       ...
       error_page 502 503 504 = @api_error;
       proxy_intercept_errors on;
   }
   location @api_error {
       default_type application/json;
       return 503 '{"error":"Backend unavailable, retrying..."}';
   }
   ```

2. **fetchWithAuth.ts** — envolver `.json()` con try-catch para que respuestas no-JSON no crasheen,
   devolviendo un objeto de error controlado.

### Impacto
Solo visual durante deploys. No afecta datos.

## Deuda técnica — Bugs y mejoras pendientes (de análisis backend 2026-05-28)

### #7 — `CategoriesController.GetAll` carga excesiva de jobs

Usa `.Include(uc => uc.Category).ThenInclude(c => c.Jobs)` que carga TODOS los jobs en memoria
solo para contarlos. Reemplazar por un subquery SQL (`.Select(c => new { c.Id, JobCount = c.Jobs.Count(...) })`)
o usar `GroupBy` para evitar materializar datos innecesarios.

### #3 — `JobsController.Delete` usa HTTP DELETE pero solo hace hide

El endpoint `DELETE /api/jobs/{id}` marca `Status = "oculto"` en vez de borrar realmente.
Violación del contrato REST. Se hace a drede por ahora; a futuro implementar soft-delete real o renombrar a `POST .../hide`.

### #9 — `JobFetchService` tiene doble responsabilidad

`JobFetchService` actúa como `BackgroundService` (hosted service) y como `IJobFetchService`
(servicio inyectable para controllers). Esto acopla lógica de negocio con infraestructura de
background tasks. Idealmente separar en dos clases: `JobFetchBackgroundService` (solo scheduling)
y `JobFetchService` (lógica de enqueue + procesamiento).

### #14 — Validación de email restringida a `@student.42barcelona.com`

El registro solo acepta emails `@student.42barcelona.com`. Si se quiere abrir a otros campus
o usuarios externos, este check debería ser configurable (lista de dominios permitidos en config).

### #16 — Foto de perfil guardada en BD como base64

Se guarda `Photo` como `text` en la tabla `users`. Infla la BD innecesariamente.
Idealmente usar un storage externo (S3, MinIO, filesystem) y guardar solo la URL.

### #17 — `Login.cs` usa `FirstOrDefaultAsync` en vez de `SingleOrDefault`

Aunque el índice unique previene duplicados, `SingleOrDefault` sería más seguro semánticamente
para la búsqueda de email en login.
