-- 018-user-github-import.sql
-- Add last_github_import_at to users table for rate limiting (1 import/day)

ALTER TABLE users ADD COLUMN IF NOT EXISTS last_github_import_at TIMESTAMPTZ;
