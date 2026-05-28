-- 44-add-reasoning-to-prompts.sql
-- Parche de producción: añade use_reasoning y reasoning_effort a ai_prompts
-- + seed con valores correctos para las tareas existentes

ALTER TABLE ai_prompts ADD COLUMN IF NOT EXISTS use_reasoning BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE ai_prompts ADD COLUMN IF NOT EXISTS reasoning_effort VARCHAR(20);

-- Tareas que usan reasoning sin effort específico (model default)
UPDATE ai_prompts SET use_reasoning = TRUE WHERE functionality = 'extract_keywords';

-- Tareas que usan reasoning con effort high
UPDATE ai_prompts SET use_reasoning = TRUE, reasoning_effort = 'high' WHERE functionality = 'cv_generation';
UPDATE ai_prompts SET use_reasoning = TRUE, reasoning_effort = 'high' WHERE functionality = 'analyze_github';
