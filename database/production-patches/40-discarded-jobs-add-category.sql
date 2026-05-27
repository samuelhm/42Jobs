-- 40-discarded-jobs-add-category.sql
-- Adds category_name column to existing discarded_jobs table.

ALTER TABLE discarded_jobs
    ADD COLUMN IF NOT EXISTS category_name VARCHAR(100);
