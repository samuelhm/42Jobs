CREATE TABLE IF NOT EXISTS ai_schemas (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    json_schema JSONB NOT NULL,
    created_at  TIMESTAMP DEFAULT NOW(),
    updated_at  TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ai_prompts (
    id                    SERIAL PRIMARY KEY,
    functionality         VARCHAR(100) NOT NULL UNIQUE,
    name                  VARCHAR(200) NOT NULL,
    description           TEXT,
    system_prompt         TEXT NOT NULL,
    user_prompt_template  TEXT NOT NULL,
    schema_id             INTEGER REFERENCES ai_schemas(id) ON DELETE SET NULL,
    default_model_id      INTEGER REFERENCES ai_models(id) ON DELETE SET NULL,
    is_active             BOOLEAN DEFAULT TRUE,
    created_at            TIMESTAMP DEFAULT NOW(),
    updated_at            TIMESTAMP DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ai_prompts_schema ON ai_prompts(schema_id);
CREATE INDEX IF NOT EXISTS idx_ai_prompts_model ON ai_prompts(default_model_id);
