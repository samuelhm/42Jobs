CREATE TABLE IF NOT EXISTS keywords (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL UNIQUE,
    created_at      TIMESTAMP DEFAULT NOW()
);
