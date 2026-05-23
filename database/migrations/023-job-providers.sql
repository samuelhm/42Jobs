CREATE TABLE IF NOT EXISTS job_providers (
    id            SERIAL PRIMARY KEY,
    portal        VARCHAR(50) NOT NULL,
    provider_name VARCHAR(100) NOT NULL,
    is_active     BOOLEAN DEFAULT TRUE,
    is_enabled    BOOLEAN DEFAULT FALSE,
    base_url      VARCHAR(300),
    api_key       VARCHAR(500),
    config        JSONB,
    created_at    TIMESTAMP DEFAULT NOW(),
    updated_at    TIMESTAMP DEFAULT NOW(),
    UNIQUE (portal, provider_name)
);
