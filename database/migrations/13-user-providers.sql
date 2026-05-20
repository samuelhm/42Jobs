CREATE TABLE IF NOT EXISTS user_providers (
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider      VARCHAR(50) NOT NULL,
    provider_id   VARCHAR(300) NOT NULL,
    access_token  TEXT,
    refresh_token TEXT,
    created_at    TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (provider, provider_id)
);
