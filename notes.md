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
