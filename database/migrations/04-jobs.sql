CREATE TABLE IF NOT EXISTS jobs (
    id               SERIAL PRIMARY KEY,
    linkedin_id      VARCHAR(50) NOT NULL UNIQUE,
    category_id      INTEGER REFERENCES categories(id) ON DELETE SET NULL,
    company_id       INTEGER REFERENCES companies(id) ON DELETE SET NULL,
    title            VARCHAR(500),
    location         VARCHAR(500),
    posted_date      DATE,
    salary           VARCHAR(200),
    benefits         TEXT,
    job_type         VARCHAR(200),
    experience_level VARCHAR(200),
    industry         VARCHAR(200),
    job_function     VARCHAR(200),
    applicants       VARCHAR(100),
    description      TEXT,
    job_url          TEXT,
    created_at       TIMESTAMP DEFAULT NOW(),
    updated_at       TIMESTAMP DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_jobs_category    ON jobs(category_id);
CREATE INDEX IF NOT EXISTS idx_jobs_company     ON jobs(company_id);
CREATE INDEX IF NOT EXISTS idx_jobs_posted_date ON jobs(posted_date);
