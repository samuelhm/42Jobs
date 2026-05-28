-- 43-add-supports-reasoning.sql
-- Parche de producción: añade columna supports_reasoning a ai_models
-- Relacionado con el fix para no enviar reasoning_effort a modelos que no lo soportan
-- Los modelos GPT-4.1 NO deben tener este flag activado
-- Los modelos GPT-5.4/5.5 SÍ deben tenerlo

ALTER TABLE ai_models ADD COLUMN IF NOT EXISTS supports_reasoning BOOLEAN NOT NULL DEFAULT FALSE;

-- Marcar modelos conocidos que SÍ soportan reasoning
-- (asume nombres estandar de modelos; ajustar si los nombres reales son distintos)
UPDATE ai_models SET supports_reasoning = TRUE 
WHERE name ILIKE '%gpt-5.4%' OR name ILIKE '%gpt-5.5%' OR name ILIKE '%deepseek%';
