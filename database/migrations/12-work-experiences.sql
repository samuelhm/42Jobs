CREATE TABLE IF NOT EXISTS work_experiences (
    id          SERIAL PRIMARY KEY,
    company     VARCHAR(200) NOT NULL,
    position    VARCHAR(200),
    start_date  DATE,
    end_date    DATE,
    description TEXT
);

CREATE TABLE IF NOT EXISTS work_experience_keywords (
    experience_id INTEGER REFERENCES work_experiences(id) ON DELETE CASCADE,
    keyword_id    INTEGER REFERENCES keywords(id) ON DELETE CASCADE,
    PRIMARY KEY (experience_id, keyword_id)
);