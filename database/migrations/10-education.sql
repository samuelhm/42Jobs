CREATE TABLE IF NOT EXISTS education (
    id          SERIAL PRIMARY KEY,
    degree      VARCHAR(200) NOT NULL,
    institution VARCHAR(200),
    start_year  INTEGER,
    end_year    INTEGER
);