CREATE TABLE IF NOT EXISTS user_categories (
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    category_id INTEGER NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    created_at  TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (user_id, category_id)
);

CREATE INDEX IF NOT EXISTS idx_user_categories_user     ON user_categories(user_id);
CREATE INDEX IF NOT EXISTS idx_user_categories_category ON user_categories(category_id);
