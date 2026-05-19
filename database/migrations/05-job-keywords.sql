CREATE TABLE IF NOT EXISTS job_keywords (
    job_id     INTEGER NOT NULL REFERENCES jobs(id) ON DELETE CASCADE,
    keyword_id INTEGER NOT NULL REFERENCES keywords(id) ON DELETE CASCADE,
    PRIMARY KEY (job_id, keyword_id)
);

CREATE INDEX IF NOT EXISTS idx_job_keywords_keyword ON job_keywords(keyword_id);
CREATE INDEX IF NOT EXISTS idx_job_keywords_job     ON job_keywords(job_id);