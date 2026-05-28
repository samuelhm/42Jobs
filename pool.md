# Arquitectura del Pool de Conexiones — 42jobs

## Visión general

42jobs usa **dos mecanismos de control de concurrencia** que trabajan juntos:

| Capa | Qué controla | Dónde está configurado |
|------|-------------|----------------------|
| **Pool de PostgreSQL** | Conexiones simultáneas a la BD | `DatabaseUrlParser.cs` |
| **Semáforo de jobs** | Jobs procesados en paralelo por fetch | `JobFetchService.ProcessFetch.cs` |
| **Rate limiter de LinkedIn** | Peticiones por minuto a la API externa | `LinkedInRapidApiProvider.cs` |
| **Channel bounded** | Fetch requests en cola de espera | `JobFetchService.cs` |

---

## 1. Pool de conexiones PostgreSQL

```csharp
// DatabaseUrlParser.cs
$"Host={host};Port={port};Database={db};Username={user};Password={pw};
  MinPoolSize=1;MaxPoolSize=60;Keepalive=30;Connection Idle Lifetime=120"
```

### Parámetros

| Parámetro | Valor | Explicación |
|-----------|-------|-------------|
| `MinPoolSize` | 1 | Al menos 1 conexión siempre lista. No desperdiciamos RAM con conexiones innecesarias |
| `MaxPoolSize` | 60 | Tope máximo. Si se alcanza, las nuevas peticiones esperan o crashean |
| `Timeout` | 15s (default) | Si no hay conexión libre en 15 segundos, lanza `Pool exhausted` |
| `Keepalive` | 30s | Envía paquetes keepalive cada 30s para evitar que proxies/firewalls maten conexiones inactivas |
| `Connection Idle Lifetime` | 120s | Conexiones inactivas más de 2 minutos se cierran y reciclan |

### ¿Por qué 60?

Cada job en el pipeline abre su propio `DbContext` (1 conexión). Con `SemaphoreSlim(10)`, un fetch ocupa hasta 10 conexiones. Si hay 3 fetches simultáneos, son 30 conexiones para el pipeline. Las 30 restantes son margen para:

- Peticiones del frontend (dashboard, offers, profile...)
- Admin panel (logs, AI services, discarded jobs...)
- Otras tareas de fondo (scheduler, admin log service...)

Si el pool fuese más pequeño, el frontend se quedaría sin conexiones durante un fetch y los usuarios verían errores 500 o timeouts.

### ¿Por qué no un número gigante (ej. 200)?

- **RAM**: cada conexión PostgreSQL consume ~2-4 MB. 200 conexiones = 400-800 MB solo en idle
- **PostgreSQL**: `max_connections` suele ser 100. El pool no puede excederlo
- **Context switching**: PostgreSQL con 100+ conexiones activas gasta más CPU alternando entre queries que ejecutándolas
- **No acelera el pipeline**: el cuello de botella real es LinkedIn (8 req/min), no la BD

---

## 2. Semáforo de concurrencia por fetch

```csharp
// JobFetchService.ProcessFetch.cs
var semaphore = new SemaphoreSlim(10);
```

Cada `ProcessFetchAsync` crea **su propio semáforo local**. Esto significa que si hay 3 fetch requests encolados (3 categorías distintas), cada uno puede procesar hasta 10 jobs en paralelo. Total máximo: 30 jobs concurrentes entre todos los fetches activos.

### ¿Por qué 10 y no 50?

El límite real es la API de LinkedIn RapidAPI: **8 peticiones por minuto**. Cada job necesita 1 llamada `GET /job/{id}` para obtener los detalles. Con 50 jobs concurrentes, 42 se quedarían bloqueados en el rate limiter ocupando una conexión de BD sin hacer nada útil. 10 jobs es suficiente para saturar el rate limiter sin desperdiciar recursos.

---

## 3. Rate limiter de LinkedIn

```csharp
// LinkedInRapidApiProvider.cs
private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(1);
private static readonly int MaxRequestsPerWindow = 8;
```

Funciona con una ventana deslizante de 1 minuto. Guarda un timestamp por cada petición. Si hay 8 timestamps en los últimos 60 segundos, la petición 9 espera hasta que el más antiguo caduque.

```csharp
private async Task WaitForRateLimitAsync(CancellationToken ct)
{
    lock (_rateLock)
    {
        // Limpia timestamps fuera de la ventana
        while (_requestTimestamps.TryPeek(out var ts) && ts < cutoff)
            _requestTimestamps.TryDequeue(out _);

        if (_requestTimestamps.Count < MaxRequestsPerWindow)
        {
            _requestTimestamps.Enqueue(now);
            return; // hay hueco, pasa
        }

        // Calcula cuánto esperar hasta que se libere un hueco
        var waitMs = (oldest + RateWindow - now).TotalMilliseconds;
        Task.Delay(waitMs, ct).GetAwaiter().GetResult();
    }
}
```

### ¿Por qué un lock síncrono dentro de async?

Es intencionado: el rate limiter debe ser secuencial y determinista. Si varias tasks intentan pasar a la vez, solo una lo hace; las demás esperan su turno con `Task.Delay` dentro del lock. No es ideal para throughput, pero LinkedIn RapidAPI penaliza duramente los bursts con 429.

---

## 4. Channel bounded para fetch requests

```csharp
private readonly Channel<FetchRequest> _channel = Channel.CreateBounded<FetchRequest>(100);
```

Los fetch requests (manuales o del scheduler) se encolan en este channel. El `BackgroundService.ExecuteAsync` los consume uno a uno en un bucle `await foreach`.

Si el channel se llena (100 requests pendientes), `TryWrite` devuelve `false` y la request se descarta. En la práctica esto nunca pasa porque:

- El scheduler procesa secuencialmente (espera a que termine un fetch antes de encolar el siguiente)
- Los usuarios solo pueden crear categorías de una en una
- El admin fetch-all también es secuencial

El límite de 100 es puramente defensivo.

---

## 5. Evitar colisiones: `_categoryInProgress`

```csharp
private readonly ConcurrentDictionary<int, Guid> _categoryInProgress = new();

public Guid? Enqueue(int categoryId, ...)
{
    if (_categoryInProgress.TryGetValue(categoryId, out var existingJobId))
        return existingJobId; // ya hay un fetch en curso para esta categoría
    ...
}
```

Si un fetch para la categoría 5 ya está corriendo, un segundo intento de encolar la misma categoría devuelve el `jobId` del fetch en curso. Esto evita:

- Duplicar peticiones a LinkedIn para la misma categoría
- Doble gasto de IA (filter + keywords duplicados)
- Carreras de escritura en BD (dos procesos guardando los mismos jobs)

### Limitación actual

La clave es solo `categoryId`, no `(categoryId, location)`. Si alguien intenta fetchear la misma categoría con dos ubicaciones distintas a la vez, la segunda se traga el `jobId` de la primera y no se ejecuta para la ubicación nueva. **Actualmente no es un problema** porque el scheduler procesa secuencialmente por ubicación.

---

## 6. Evitar colisiones: `_fetchAllRunning`

```csharp
private int _fetchAllRunning;

if (Interlocked.CompareExchange(ref _fetchAllRunning, 1, 0) != 0)
{
    _logger.LogWarning("Fetch all already running, skipping");
    return;
}
```

Protege `FetchAllCategoriesWithTokenAsync` contra ejecución simultánea. Si el scheduler (cada 4h) y el admin (manual) intentan lanzar un fetch-all a la vez, el segundo recibe HTTP 409.

`Interlocked.CompareExchange` es atómico a nivel CPU — no hay race condition posible.

---

## 7. Flujo completo de un fetch

```
POST /api/categories { name: "Backend" }
  │
  ├─► Enqueue(categoryId, "Backend", { location: "Barcelona", limit: 10, datePosted: "past-week" })
  │     │
  │     ├─ ¿_categoryInProgress tiene categoryId? → no
  │     ├─ Crea FetchRequest → Channel.TryWrite() → ✅
  │     └─ Devuelve jobId
  │
  └─► BackgroundService.ExecuteAsync
        │
        └─► ProcessFetchAsync
              │
              ├─► GetEnabledProvidersAsync() → [LinkedInRapidApiProvider]
              ├─► provider.SearchAsync("Backend", "Barcelona", ...)
              │     └─► Rate limiter: máx 8 req/min
              │
              ├─► DeduplicateJobs() → 15 jobs únicos
              │
              └─► SemaphoreSlim(10) → 10 jobs en paralelo
                    │
                    └─► Para cada job (×10 concurrentes):
                          ├─► Check Jobs table (external_id + source) → ¿ya existe?
                          ├─► Check DiscardedJobs (external_id + source + category) → ¿ya descartado?
                          ├─► provider.GetDetailsAsync(externalId)
                          │     └─► Rate limiter: máx 8 req/min (compartido con Search)
                          ├─► ai.FilterJobRelevanceAsync(keyword, title, description)
                          │     └─► Lee prompt + modelo de BD (sin caché)
                          ├─► ¿relevant=no o juniorFriendly=no?
                          │     └─► Guarda en DiscardedJobs → skip
                          ├─► UpsertCompany → SaveChanges
                          ├─► Save Job → SaveChanges
                          ├─► INSERT job_categories
                          └─► ai.ExtractKeywordsAsync → SaveChanges
```

---

## 8. Resumen de por qué no hay cuellos de botella

| Recurso | Límite | Quién lo impone | Por qué es suficiente |
|---------|--------|----------------|----------------------|
| **Conexiones BD** | 60 pool | Npgsql | 30 para pipeline + 30 margen para frontend |
| **Jobs concurrentes** | 10 por fetch | SemaphoreSlim | El rate limit de LinkedIn (8/min) es más restrictivo |
| **API LinkedIn** | 8 req/min | Rate limiter propio | Impuesto por RapidAPI, no podemos cambiarlo |
| **Fetch requests en cola** | 100 | BoundedChannel | Defensivo, nunca se alcanza en práctica |
| **Fetch-all simultáneos** | 1 | Interlocked flag | Evita colisiones scheduler vs admin |
| **Misma categoría duplicada** | 1 | ConcurrentDictionary | Evita doble gasto de API + IA |
