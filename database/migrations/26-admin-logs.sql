-- 26-admin-logs.sql
-- Audit log for LLM calls and job provider requests/responses

CREATE TABLE IF NOT EXISTS admin_logs (
    id          SERIAL PRIMARY KEY,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    actor       TEXT NOT NULL,
    action      TEXT NOT NULL,
    payload1    JSONB,
    payload2    TEXT,
    payload3    TEXT
);

CREATE INDEX IF NOT EXISTS idx_admin_logs_created_at ON admin_logs (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_admin_logs_actor      ON admin_logs (actor);
CREATE INDEX IF NOT EXISTS idx_admin_logs_action     ON admin_logs (action);
