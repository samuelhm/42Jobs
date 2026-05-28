-- 30-add-reasoning-to-prompts.sql
-- Añade use_reasoning y reasoning_effort a ai_prompts
-- Permite configurar por tarea si se usa reasoning y con qué esfuerzo

ALTER TABLE ai_prompts ADD COLUMN IF NOT EXISTS use_reasoning BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE ai_prompts ADD COLUMN IF NOT EXISTS reasoning_effort VARCHAR(20);
