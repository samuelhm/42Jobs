-- 41-discarded-jobs-fix-unique.sql
-- Replaces (external_id, source) unique index with (external_id, source, category_name)
-- so a job discarded for one category is not blocked for others.

DROP INDEX IF EXISTS idx_discarded_jobs_external_source;

CREATE UNIQUE INDEX IF NOT EXISTS idx_discarded_jobs_external_source
    ON discarded_jobs(external_id, source, category_name);
