CREATE TABLE IF NOT EXISTS user_profile (
    id            SERIAL PRIMARY KEY,
    name          VARCHAR(200),
    last_name     VARCHAR(200),
    phone         VARCHAR(50),
    email         VARCHAR(200),
    address       TEXT,
    linkedin_url  TEXT,
    website_url   TEXT,
    github_url    TEXT,
    junior        BOOLEAN DEFAULT true,
    presentation  TEXT
);

INSERT INTO user_profile (id, name, last_name, phone, email, address, linkedin_url, website_url, github_url, junior, presentation)
VALUES (1, '', '', '', '', '', '', '', '', true, '')
ON CONFLICT (id) DO NOTHING;