CREATE TABLE IF NOT EXISTS ai_services (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(50) NOT NULL UNIQUE,
    api_key    VARCHAR(500),
    is_active  BOOLEAN DEFAULT TRUE,
    is_free_tier BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ai_models (
    id             SERIAL PRIMARY KEY,
    ai_service_id  INTEGER NOT NULL REFERENCES ai_services(id) ON DELETE CASCADE,
    name           VARCHAR(100) NOT NULL,
    is_active      BOOLEAN DEFAULT TRUE,
    created_at     TIMESTAMP DEFAULT NOW(),
    UNIQUE (ai_service_id, name)
);

CREATE INDEX IF NOT EXISTS idx_ai_models_service ON ai_models(ai_service_id);
