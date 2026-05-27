-- 39-discarded-jobs.sql
-- Stores jobs filtered out by AI (not relevant or senior-only) to avoid
-- re-fetching details and re-running AI on subsequent searches.

CREATE TABLE IF NOT EXISTS discarded_jobs (
    id               SERIAL PRIMARY KEY,
    external_id      VARCHAR(100) NOT NULL,
    source           VARCHAR(50)  NOT NULL DEFAULT 'linkedin',
    title            VARCHAR(500),
    company_name     VARCHAR(500),
    location         VARCHAR(500),
    posted_date      DATE,
    salary           VARCHAR(200),
    benefits         TEXT,
    job_url          TEXT,
    description      TEXT,
    job_type         VARCHAR(200),
    experience_level VARCHAR(200),
    industry         VARCHAR(200),
    job_function     VARCHAR(200),
    applicants       VARCHAR(100),
    filter_reasons   TEXT,                         -- JSON: {"relevant":"no","juniorFriendly":"no"}
    category_name    VARCHAR(100),                 -- category that triggered the discard
    raw_data         JSONB,                       -- complete job_details response
    created_at       TIMESTAMP DEFAULT NOW()
);
