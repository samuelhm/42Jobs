-- 27-add-correlation-id.sql
-- Relaciona llamadas API con sus respuestas mediante un mismo correlation_id

ALTER TABLE admin_logs ADD COLUMN IF NOT EXISTS correlation_id TEXT NOT NULL DEFAULT '';

CREATE INDEX IF NOT EXISTS idx_admin_logs_correlation_id ON admin_logs (correlation_id);
