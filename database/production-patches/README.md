# Production Patches

Este directorio contiene parches SQL que se aplican **exclusivamente en producción** mediante el script `scripts/apply-production-patches.sh`.

## Propósito

Cuando se necesita modificar datos en la base de datos de producción sin alterar el esquema mediante migraciones, se coloca aquí un archivo `.sql` con la consulta correspondiente.

Ejemplos de uso:
- Corregir prompts de IA mal configurados
- Normalizar datos (emails, formatos)
- Insertar seeds o templates que solo aplican al entorno productivo
- Ajustes puntuales que no justifican una migración completa

## Cómo funciona

El script `scripts/apply-production-patches.sh` se ejecuta automáticamente durante el deploy a producción. Itera sobre todos los archivos `.sql` en este directorio y los ejecuta contra la base de datos de producción usando `docker exec`.

Los archivos se ejecutan en orden alfabético, por lo que se recomienda usar un prefijo numérico (`01-`, `02-`, etc.).

## Importante

- Estos parches **se ejecutan automáticamente** en cada deploy a producción mediante `scripts/apply-production-patches.sh`
- Usan `docker exec` para ejecutar las queries SQL contra la base de datos
- Los archivos se procesan en orden alfabético, por lo que se recomienda usar un prefijo numérico (`01-`, `02-`, etc.)
- Una vez aplicados con éxito en producción, pueden eliminarse de este directorio
- No dependen de migraciones ni las migraciones dependen de ellos
