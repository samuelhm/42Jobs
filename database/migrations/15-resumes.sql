CREATE TABLE IF NOT EXISTS resumes (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    job_id     INTEGER REFERENCES jobs(id) ON DELETE SET NULL,
    cv_data    TEXT NOT NULL DEFAULT '',
    json_data  JSONB,
    template_id INTEGER REFERENCES cv_templates(id) ON DELETE SET NULL,
    prompt_id   INTEGER REFERENCES ai_prompts(id) ON DELETE SET NULL,
    model_id    INTEGER REFERENCES ai_models(id) ON DELETE SET NULL,
    model       VARCHAR(30) DEFAULT 'gpt-5.4-mini',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE (user_id, job_id)
);

CREATE INDEX IF NOT EXISTS idx_resumes_user ON resumes(user_id);
CREATE INDEX IF NOT EXISTS idx_resumes_job  ON resumes(job_id);
