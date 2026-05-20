CREATE TABLE IF NOT EXISTS user_jobs (
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    job_id     INTEGER NOT NULL REFERENCES jobs(id) ON DELETE CASCADE,
    saved_at   TIMESTAMP DEFAULT NOW(),
    notes      TEXT,
    applied    BOOLEAN DEFAULT FALSE,
    applied_at TIMESTAMP,
    PRIMARY KEY (user_id, job_id)
);

CREATE INDEX IF NOT EXISTS idx_user_jobs_user ON user_jobs(user_id);
CREATE INDEX IF NOT EXISTS idx_user_jobs_job  ON user_jobs(job_id);
