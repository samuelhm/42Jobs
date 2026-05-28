-- 29-add-supports-reasoning.sql
-- Añade columna supports_reasoning a ai_models
-- Los modelos que soportan reasoning (chain-of-thought) deben marcarse como TRUE
-- desde Admin > AI Models

ALTER TABLE ai_models ADD COLUMN IF NOT EXISTS supports_reasoning BOOLEAN NOT NULL DEFAULT FALSE;
