CREATE TABLE IF NOT EXISTS certifications (
    id            SERIAL PRIMARY KEY,
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name          VARCHAR(200) NOT NULL,
    entity        VARCHAR(200),
    date_obtained DATE
);