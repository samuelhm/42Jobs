-- 27-fix-ai-models.sql
-- Delete fictional AI models and add missing real ones.
-- Real names confirmed from OpenAI & Google API docs (May 2026).

-- ═══════════════════════════════════════════════════════════
-- Step 1: Fix cv_generation prompt to use a real model before deleting fictional ones
-- ═══════════════════════════════════════════════════════════
UPDATE ai_prompts
SET default_model_id = (SELECT id FROM ai_models WHERE name = 'gpt-5.5')
WHERE functionality = 'cv_generation';

-- ═══════════════════════════════════════════════════════════
-- Step 2: Delete fictional models that don't exist
-- (ON DELETE SET NULL on ai_prompts.default_model_id, safe)
-- ═══════════════════════════════════════════════════════════
DELETE FROM ai_models WHERE name IN ('gpt-5.4-pro', 'gpt-5.5-mini', 'gpt-5.5-pro');

-- ═══════════════════════════════════════════════════════════
-- Step 3: Add missing real models
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_models (ai_service_id, name) VALUES
    -- OpenAI (ai_service_id = 2)
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5-mini'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1-mini'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4o'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4o-mini'),
    -- Google (ai_service_id = 1)
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-2.5-flash'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-2.5-pro')
ON CONFLICT (ai_service_id, name) DO NOTHING;
