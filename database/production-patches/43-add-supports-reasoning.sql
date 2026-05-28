-- 43-add-supports-reasoning.sql
-- Parche de producción: añade columna supports_reasoning a ai_models
-- Controla si se envía reasoning/effort al llamar a la API del modelo

ALTER TABLE ai_models ADD COLUMN IF NOT EXISTS supports_reasoning BOOLEAN NOT NULL DEFAULT FALSE;

-- Modelos que SÍ soportan reasoning (chain-of-thought):
--   OpenAI: GPT-5.x, GPT-5.4, GPT-5.5 (todos con reasoning_effort)
--   Google: TODOS los Gemini (2.5 con thinkingBudget, 3.x con thinkingLevel)
--   DeepSeek: V4-pro, V4-flash (thinking mode + reasoning_effort)
UPDATE ai_models SET supports_reasoning = TRUE
WHERE name ILIKE '%gpt-5%'    -- gpt-5, gpt-5-mini, gpt-5-nano, gpt-5.4, gpt-5.4-*, gpt-5.5
   OR name ILIKE '%gemini%'   -- gemini-2.5-*, gemini-3*, gemini-3.1-*, gemini-3.5-*
   OR name ILIKE '%deepseek%';

-- Modelos que NO soportan reasoning (generación estándar):
--   OpenAI: gpt-4.1, gpt-4.1-mini, gpt-4.1-nano, gpt-4o, gpt-4o-mini
--   → se quedan con supports_reasoning = FALSE (default)
