CREATE TABLE IF NOT EXISTS blocked_keywords (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(200) NOT NULL UNIQUE,
    redirect_to INTEGER REFERENCES keywords(id) ON DELETE SET NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_blocked_keywords_redirect ON blocked_keywords(redirect_to);
