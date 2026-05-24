-- 36-seed-deepseek.sql
-- Seed data: DeepSeek AI service and models.
-- Schemas are file-based: backend/src/Services/Ai/Schemas/{functionality}.deepseek.json

-- ═══════════════════════════════════════════════════════════
-- DeepSeek Service
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_services (name, is_free_tier) VALUES
    ('DeepSeek', FALSE)
ON CONFLICT (name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- DeepSeek Models (real names from DeepSeek API docs, May 2026)
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_models (ai_service_id, name) VALUES
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-v4-flash'),
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-v4-pro')
ON CONFLICT (ai_service_id, name) DO NOTHING;
