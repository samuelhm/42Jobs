CREATE TABLE IF NOT EXISTS education (
    id          SERIAL PRIMARY KEY,
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    degree      VARCHAR(200) NOT NULL,
    institution VARCHAR(200),
    start_year  INTEGER,
    end_year    INTEGER
);