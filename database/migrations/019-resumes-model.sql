-- 019-resumes-model.sql
-- Add model column and change cv_data from JSONB to TEXT

ALTER TABLE resumes ADD COLUMN IF NOT EXISTS model VARCHAR(30) DEFAULT 'gpt-5.4-mini';
ALTER TABLE resumes ALTER COLUMN cv_data TYPE TEXT;
ALTER TABLE resumes ALTER COLUMN cv_data SET DEFAULT '';
