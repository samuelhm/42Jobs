-- M2M: users <-> keywords with learning status per user

CREATE TABLE IF NOT EXISTS user_keywords (
    user_id         UUID REFERENCES users(id) ON DELETE CASCADE,
    keyword_id      INTEGER REFERENCES keywords(id) ON DELETE CASCADE,
    learning_status VARCHAR(50) DEFAULT 'not_learned'
        CHECK (learning_status IN (
            'not_learned',
            'learned_personal_project',
            'learned_in_school'
        )),
    created_at      TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (user_id, keyword_id)
);

CREATE INDEX IF NOT EXISTS idx_user_keywords_user ON user_keywords(user_id);
CREATE INDEX IF NOT EXISTS idx_user_keywords_kw   ON user_keywords(keyword_id);
