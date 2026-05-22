-- M2M: jobs <-> categories (replaces 1:N category_id on jobs)

CREATE TABLE IF NOT EXISTS job_categories (
    job_id      INTEGER REFERENCES jobs(id) ON DELETE CASCADE,
    category_id INTEGER REFERENCES categories(id) ON DELETE CASCADE,
    PRIMARY KEY (job_id, category_id)
);

CREATE INDEX IF NOT EXISTS idx_job_categories_category ON job_categories(category_id);
