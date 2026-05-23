CREATE TABLE IF NOT EXISTS companies (
    id           SERIAL PRIMARY KEY,
    name         VARCHAR(500) NOT NULL UNIQUE,
    linkedin_url TEXT,
    company_type VARCHAR(50)
        CHECK (company_type IN ('Multinacional', 'Startup', 'Pyme', 'Consultora'))
);