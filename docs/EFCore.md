# Entity Framework Core — Decisiones de proyecto

## QuerySplittingBehavior global: por qué SplitQuery

### El problema: explosión cartesiana con múltiples `.Include()`

Cuando una query de EF Core carga varias colecciones con `.Include()`, el comportamiento por defecto
(`QuerySplittingBehavior.SingleQuery`) genera un solo `SELECT` con múltiples `LEFT JOIN`. Esto produce
un **producto cartesiano** entre todas las colecciones.

Ejemplo real: el endpoint `GET /api/profile` cargaba 5 colecciones:

```csharp
_db.Users
    .Include(u => u.Languages)           // ~3 filas
    .Include(u => u.Certifications)      // ~6 filas
    .Include(u => u.Educations)          // ~4 filas
    .Include(u => u.Projects)            // ~30 filas
        .ThenInclude(p => p.Keywords)    // ~15 keywords por proyecto
    .Include(u => u.WorkExperiences)     // ~4 filas
        .ThenInclude(w => w.Keywords)
```

Con SingleQuery, esto genera un JOIN masivo donde cada fila del resultset contiene columnas de
**todas** las tablas. El número de filas devueltas por PostgreSQL es:

```
1 × 3 × 6 × 4 × 30 × 4 = 8,640 filas base
× keywords (muchas más duplicadas por el cartesiano)
= decenas de miles de filas
```

EF Core recibe todo esto, lo deduplica en memoria y lo serializa a JSON. El resultado:
**~25 segundos de latencia** para una simple petición de perfil, con un payload de decenas de KB
en JSON de datos que el frontend no necesita en la mayoría de páginas.

Además, PostgreSQL emitía este warning:

```
Compiling a query which loads related collections for more than one collection
navigation, either via 'Include' or through projection, but no
'QuerySplittingBehavior' has been configured.
```

### La solución: SplitQuery

Con split queries, EF Core ejecuta **queries separadas** para cada colección:

```sql
SELECT * FROM users WHERE id = @id;                    -- 1 fila
SELECT * FROM languages WHERE user_id = @id;           -- 3 filas
SELECT * FROM certifications WHERE user_id = @id;      -- 6 filas
SELECT * FROM educations WHERE user_id = @id;          -- 4 filas
SELECT * FROM projects WHERE user_id = @id;            -- 30 filas
SELECT * FROM keywords k JOIN project_keywords pk ...;  -- solo keywords relevantes
SELECT * FROM work_experiences WHERE user_id = @id;    -- 4 filas
SELECT * FROM keywords k JOIN work_experience_keywords wk ...;
```

Resultado: 8 queries pequeñas y eficientes en lugar de 1 monstruosa. Latencia: de **25 segundos
a milisegundos**.

### Por qué global y no por query

Se configuró a nivel de `AppDbContext` en `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString!, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
           .UseSnakeCaseNamingConvention());
```

**Nota sobre EF Core 10:** a diferencia de versiones anteriores, en EF Core 10
`UseQuerySplittingBehavior` **no** está disponible como extensión directa sobre
`DbContextOptionsBuilder`. Debe llamarse dentro del callback del proveedor
(`NpgsqlDbContextOptionsBuilder` → `o` en el ejemplo), donde sí está expuesto como
método de `RelationalDbContextOptionsBuilder`.

Razones:

1. **Seguridad a futuro**: cualquier query nueva con múltiples `.Include()` hereda el comportamiento correcto sin que el desarrollador tenga que recordar añadir `.AsSplitQuery()`.
2. **Sin impacto negativo**: las queries con un solo `.Include()` (la mayoría en este proyecto) no se ven afectadas — solo aplica cuando hay ≥2 colecciones. Las queries con 2 colecciones pequeñas (ej. `Job.Company` + `Job.Keywords`) pasan de 1 SELECT a 3 SELECTs triviales, sin penalización medible.
3. **Consistencia**: todas las queries del proyecto siguen el mismo comportamiento.

### Tradeoff: consistencia vs rendimiento

SplitQuery no es el default de EF Core por una razón: **consistencia de lectura**.

- **SingleQuery**: una sola transacción atómica. Todos los datos son de la misma instantánea.
- **SplitQuery**: múltiples queries separadas. Si entre la query 1 y la query 8 otro proceso modifica datos, podrías leer un estado inconsistente.

En 42jobs, todas las operaciones con múltiples `.Include()` son **lecturas para mostrar datos**
(perfil, lista de jobs, administración). No hay escenarios de lectura-modificación-escritura
que dependan de consistencia entre colecciones. El tradeoff es aceptable.

### Cuándo NO usar SplitQuery

Si en el futuro se añade una operación que:
- Carga múltiples colecciones
- Modifica datos basándose en lo leído
- Necesita garantía de instantánea atómica

...esa query específica debería usar `.AsSingleQuery()` para anular el comportamiento global.

## Otras consideraciones de EF Core en el proyecto

### Migraciones

Las migraciones son archivos SQL planos en `database/migrations/`, NO migraciones de EF Core.
Se aplican alfabéticamente al crear el contenedor de PostgreSQL por primera vez. Esto se decidió
para tener control total sobre el SQL y evitar las limitaciones de las migraciones automáticas
de EF Core con esquemas complejos (M2M, índices parciales, etc.).

### Snake case naming

Configurado globalmente con `.UseSnakeCaseNamingConvention()`. Todas las tablas y columnas
en PostgreSQL usan `snake_case`, y el JSON de la API también.
