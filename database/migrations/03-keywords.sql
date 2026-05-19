CREATE TABLE IF NOT EXISTS keywords (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(200) NOT NULL UNIQUE,
    learning_status VARCHAR(50) DEFAULT 'not_learned'
        CHECK (learning_status IN (
            'not_learned',
            'learned_personal_project',
            'learned_in_school'
        )),
    created_at      TIMESTAMP DEFAULT NOW()
);
