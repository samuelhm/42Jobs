CREATE TABLE IF NOT EXISTS projects (
    id          SERIAL PRIMARY KEY,
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name        VARCHAR(300) NOT NULL,
    description TEXT,
    type        VARCHAR(20) NOT NULL CHECK (type IN ('personal', 'school')),
    UNIQUE(user_id, name)
);

CREATE TABLE IF NOT EXISTS project_keywords (
    project_id INTEGER REFERENCES projects(id) ON DELETE CASCADE,
    keyword_id INTEGER REFERENCES keywords(id) ON DELETE CASCADE,
    PRIMARY KEY (project_id, keyword_id)
);