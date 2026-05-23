-- 26-drop-ai-schemas.sql
-- Remove ai_schemas table and schema_id FK columns.
-- Schemas are now stored as JSON files in backend/src/Services/Ai/Schemas/
-- Each provider (openai, google) has its own variant per functionality.

-- Drop FK columns from ai_prompts and ai_models
ALTER TABLE ai_prompts DROP COLUMN IF EXISTS schema_id;
ALTER TABLE ai_models DROP COLUMN IF EXISTS schema_id;

-- Drop the ai_schemas table
DROP TABLE IF EXISTS ai_schemas;
