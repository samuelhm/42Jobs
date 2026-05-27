CREATE TABLE IF NOT EXISTS users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email           VARCHAR(300) NOT NULL UNIQUE,
    password_hash   VARCHAR(300),
    name            VARCHAR(200),
    last_name       VARCHAR(200),
    phone           VARCHAR(50),
    address         TEXT,
    linkedin_url    TEXT,
    website_url     TEXT,
    github_url      TEXT,
    junior          BOOLEAN DEFAULT true,
    presentation    TEXT,
    avatar_url      TEXT,
    role            VARCHAR(20) NOT NULL DEFAULT 'User'
                    CHECK (role IN ('Admin', 'User')),
    created_at      TIMESTAMP DEFAULT NOW(),
    updated_at      TIMESTAMP DEFAULT NOW(),
    last_github_import_at TIMESTAMPTZ,
    preferred_location      VARCHAR(200)
);
