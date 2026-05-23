-- 24-seed-job-providers.sql
-- Seed data: default job providers

INSERT INTO job_providers (portal, provider_name, is_enabled, base_url, api_key) VALUES
    ('LinkedIn', 'RapidAPI', TRUE, NULL, NULL)
ON CONFLICT (portal, provider_name) DO NOTHING;
