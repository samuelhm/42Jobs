CREATE TABLE IF NOT EXISTS user_jobs (
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    job_id     INTEGER NOT NULL REFERENCES jobs(id) ON DELETE CASCADE,
    saved_at   TIMESTAMP DEFAULT NOW(),
    notes      TEXT,
    status            VARCHAR(30) NOT NULL DEFAULT 'saved'
                      CHECK (status IN ('saved', 'cv_enviado', 'entrevista_conseguida', 'empleo_conseguido', 'rechazado', 'oculto')),
    status_updated_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (user_id, job_id)
);

CREATE INDEX IF NOT EXISTS idx_user_jobs_user ON user_jobs(user_id);
CREATE INDEX IF NOT EXISTS idx_user_jobs_job  ON user_jobs(job_id);
