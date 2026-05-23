-- 024-seed-job-providers.sql
-- Seed data: default job providers

INSERT INTO job_providers (portal, provider_name, is_enabled, base_url) VALUES
    ('LinkedIn', 'RapidAPI', TRUE, NULL)
ON CONFLICT (portal, provider_name) DO NOTHING;
