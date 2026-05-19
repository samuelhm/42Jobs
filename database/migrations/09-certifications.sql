CREATE TABLE IF NOT EXISTS certifications (
    id            SERIAL PRIMARY KEY,
    name          VARCHAR(200) NOT NULL,
    entity        VARCHAR(200),
    date_obtained DATE
);