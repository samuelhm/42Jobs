CREATE TABLE IF NOT EXISTS ai_services (
    id           SERIAL PRIMARY KEY,
    name         VARCHAR(50) NOT NULL UNIQUE,
    api_key      VARCHAR(500),
    is_active    BOOLEAN DEFAULT TRUE,
    is_free_tier BOOLEAN DEFAULT FALSE,
    created_at   TIMESTAMP DEFAULT NOW(),
    updated_at   TIMESTAMP DEFAULT NOW()
);
