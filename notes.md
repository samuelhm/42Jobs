# Notas técnicas — 42jobs

## Categorías huérfanas

El scheduler (`JobFetchService.RunSchedulerAsync`) busca jobs para **todas** las categorías existentes, independientemente de si tienen suscriptores o no. Esto significa que:

- Si un usuario crea una categoría, luego hace unfollow, y nadie más la sigue, el scheduler seguirá gastando créditos de LinkedIn RapidAPI buscando jobs para esa categoría cada 4 horas.
- La solución definitiva sería filtrar solo categorías con al menos un `UserCategory` asociado, o implementar un sistema de "cleanup" que elimine categorías huérfanas tras un tiempo sin suscriptores.
- **Decisión actual:** se deja así por simplicidad. Si el gasto de créditos se vuelve un problema, filtrar por `UserCategories.Count > 0` en `FetchAllCategoriesWithTokenAsync`.
