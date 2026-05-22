-- M2M: jobs <-> categories
-- Replaces the 1:N category_id column on jobs

CREATE TABLE IF NOT EXISTS job_categories (
    job_id      INTEGER REFERENCES jobs(id) ON DELETE CASCADE,
    category_id INTEGER REFERENCES categories(id) ON DELETE CASCADE,
    PRIMARY KEY (job_id, category_id)
);

-- Migrate existing data from jobs.category_id
INSERT INTO job_categories (job_id, category_id)
SELECT id, category_id FROM jobs WHERE category_id IS NOT NULL
ON CONFLICT DO NOTHING;

-- Drop the old column and its index
DROP INDEX IF EXISTS idx_jobs_category;
ALTER TABLE jobs DROP COLUMN IF EXISTS category_id;
