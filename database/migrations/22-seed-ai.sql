-- 22-seed-ai.sql
-- Seed data: AI services, models, schemas and prompts

-- ═══════════════════════════════════════════════════════════
-- AI Services
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_services (name, is_free_tier) VALUES
    ('Google', TRUE),
    ('OpenAI', FALSE)
ON CONFLICT (name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Models
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_models (ai_service_id, name) VALUES
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3-flash-preview'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.1-pro-preview'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.5-flash'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.1-flash-lite'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-mini'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1-mini'),
ON CONFLICT (ai_service_id, name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Schemas
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_schemas (name, description, json_schema) VALUES

('job_filter', 'Filters job relevance and junior suitability', '{"type": "object", "required": ["error", "relevant", "junior_friendly"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if the model cannot determine the result."}, "relevant": {"type": "string", "description": "\"yes\" if the offer is clearly relevant for the profile, \"no\" if it clearly is not, \"unknown\" if uncertain."}, "junior_friendly": {"type": "string", "description": "\"no\" if the offer explicitly requires a senior profile or more than 4 years of experience. \"yes\" otherwise."}}, "additionalProperties": false}'),

('keyword_extraction', 'Extracts technologies and company type from job offers', '{"type": "object", "required": ["error", "skills", "company_type"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if extraction fails."}, "skills": {"type": "array", "items": {"type": "string"}, "description": "Exhaustive list of ALL technologies, languages, frameworks, tools, and soft skills mentioned in the offer."}, "company_type": {"type": "string", "description": "Company type: Multinational, Startup, SME, Consultancy, or \"Not identified\"."}}, "additionalProperties": false}'),

('github_projects', 'Extracts project info from GitHub repositories', '{"type": "object", "required": ["error", "projects"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if analysis fails."}, "projects": {"type": "array", "items": {"type": "object", "required": ["name", "type", "keywords", "description"], "properties": {"name": {"type": "string"}, "type": {"enum": ["personal", "school"], "type": "string"}, "keywords": {"type": "array", "items": {"type": "string"}}, "description": {"type": "string"}}, "additionalProperties": false}}}, "additionalProperties": false}'),

('keyword_dedup', 'Groups duplicate/similar keywords', '{"type": "object", "required": ["error", "groups"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if dedup fails."}, "groups": {"type": "array", "items": {"type": "array", "items": {"type": "string"}}}}, "additionalProperties": false}'),

('experience_parse', 'Parses LinkedIn experience text into structured data', '{"type": "object", "required": ["error", "experiences"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if parsing fails."}, "experiences": {"type": "array", "items": {"type": "object", "required": ["company", "end_date", "position", "start_date", "description"], "properties": {"company": {"type": "string"}, "end_date": {"type": "string"}, "position": {"type": "string"}, "start_date": {"type": "string"}, "description": {"type": "string"}}, "additionalProperties": false}}}, "additionalProperties": false}'),

('education_parse', 'Parses LinkedIn education text into structured data', '{"type": "object", "required": ["error", "education"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if parsing fails."}, "education": {"type": "array", "items": {"type": "object", "required": ["degree", "end_year", "start_year", "institution"], "properties": {"degree": {"type": "string"}, "end_year": {"type": "number"}, "start_year": {"type": "number"}, "institution": {"type": "string"}}, "additionalProperties": false}}}, "additionalProperties": false}'),

('cv_generation', 'Generates structured CV content from user profile and job offer', '{"type": "object", "required": ["error", "skills", "profile", "projects", "experiences"], "properties": {"error": {"type": "string", "description": "Null if successful. Error description if generation fails."}, "skills": {"type": "array", "items": {"type": "object", "required": ["items", "category"], "properties": {"items": {"type": "array", "items": {"type": "string"}}, "category": {"type": "string"}}, "additionalProperties": false}, "description": "4 skill categories, at least 8 skills each. Most relevant for this job."}, "profile": {"type": "string", "description": "3-4 line professional summary tailored to this specific job offer."}, "projects": {"type": "array", "items": {"type": "object", "required": ["name", "highlights", "description"], "properties": {"name": {"type": "string"}, "highlights": {"type": "array", "items": {"type": "string"}, "description": "2-4 key technical achievements or features."}, "description": {"type": "string"}}, "additionalProperties": false}, "description": "1 to 3 most relevant projects."}, "experiences": {"type": "array", "items": {"type": "object", "required": ["company", "end_date", "position", "highlights", "start_date"], "properties": {"company": {"type": "string"}, "end_date": {"type": "string"}, "position": {"type": "string"}, "highlights": {"type": "array", "items": {"type": "string"}, "description": "3-5 bullet points highlighting achievements relevant to this job."}, "start_date": {"type": "string"}}, "additionalProperties": false}, "description": "1 to 3 most relevant experiences, descriptions enhanced for this job."}}, "additionalProperties": false}')
ON CONFLICT (name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Prompts
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_prompts (functionality, name, description, system_prompt, user_prompt_template, schema_id, default_model_id) VALUES

('filter_jobs', 'Filter job relevance', 'Determines if a job offer is relevant and junior-friendly',
'You are a job offer filter specialized in Software Engineering profiles.',
'Your task is:
1. Determine if a job offer is RELEVANT for a Software Engineer specialized in "{{keyword}}".
2. Determine if the offer is SUITABLE FOR A JUNIOR PROFILE.

RELEVANCE CRITERIA:
- Positions directly related such as "{{keyword}} Engineer", "{{keyword}} Developer", etc. are relevant.
- Positions in adjacent disciplines such as Firmware, Embedded Systems, Hardware, IoT, RTOS, etc. (depending on the keyword) are relevant.
- Completely unrelated positions such as "Sales Manager", "Backend Developer" (if keyword is Embedded), "Recruiter", etc. are NOT relevant.
- If unsure, respond "unknown" in the relevant field.

JUNIOR PROFILE CRITERIA (junior_friendly):
- Respond "no" if the offer EXPLICITLY requires: "Senior", "Senior Software Engineer", "Lead", "Principal", "Staff Engineer", "Tech Lead", "Engineering Manager", or more than 4 years of experience.
- Respond "yes" if the offer mentions "Junior", "Internship", "Intern", "Graduate", "Entry Level", "0-2 years", "1-3 years", or does not specify seniority level.
- If the offer asks for "3-4 years" or "Mid-level" or similar, respond "yes" (borderline but acceptable for junior).
- If nothing is mentioned about seniority or years of experience, respond "yes".

Offer: "{{title}}"
Description: "{{description}}"',
(SELECT id FROM ai_schemas WHERE name = 'job_filter'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('extract_keywords', 'Extract keywords from job offers', 'Extracts technologies, skills and company type from a job description',
'You are a job offer analyzer. You extract technologies, skills, and company type.',
'Analyze this job offer and extract technologies, languages, tools, frameworks, technical concepts AND soft skills mentioned (communication, leadership, teamwork, etc.). Also determine the company type.

Offer: "{{text}}"',
(SELECT id FROM ai_schemas WHERE name = 'keyword_extraction'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('analyze_github', 'Analyze GitHub repositories', 'Extracts structured project information from GitHub repos',
'You are a GitHub project analyzer. Your task is to analyze a user''s repositories and extract structured information from each one.',
'Analyze each project. For each one you must:
1. Extract a descriptive name (clean, no hyphens, max 60 characters).
2. Generate a description in English (2-4 sentences) explaining the purpose, technologies used, and scope of the project.
3. Determine if it is a PERSONAL project or a SCHOOL/BOOTCAMP project (type: "personal" or "school"). If the README mentions "42", "42 School", "42 Barcelona", "cursus", "bootcamp" -> it is school. If it cannot be determined -> personal.
4. Extract an EXHAUSTIVE list of technologies, languages, frameworks, libraries, tools, and technical concepts (skills). Include EVERYTHING you see in the README, package.json, requirements.txt, Makefile, CMakeLists, docker-compose, etc. Be very thorough.

Projects to analyze:
{{input}}',
(SELECT id FROM ai_schemas WHERE name = 'github_projects'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('dedup_keywords', 'Deduplicate keywords', 'Groups equivalent/similar keywords into clusters',
'You are a technical keyword deduplicator. Your task is to group keywords that mean the same concept or area.',
'Group the following keywords. Rules:
- Group terms that refer to the same concept or area, even if they are not exact synonyms.
- Examples of valid groups: ui + ui/ux + ui/ux design + user interface, ai + artificial intelligence + machine learning/ai, aws + amazon web services, docker + docker compose + containerization, react + react.js, node + node.js, python + python3, c# + csharp + .net.
- Do not group clearly different technologies (e.g., react and vue: NO).
- Each group must have words in lowercase.
- If a word has no equivalents, it goes in its own group of 1 element.
- Return an array of groups, where each group is an array of equivalent strings.

Keywords to analyze:
{{keywords}}',
(SELECT id FROM ai_schemas WHERE name = 'keyword_dedup'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('parse_experience', 'Parse LinkedIn experience', 'Extracts structured work experience from LinkedIn raw text',
'You are a LinkedIn data extractor. You convert work experience text to structured JSON.',
'Extract work experiences to JSON. The date line ALWAYS has this exact format: "month. year - month. year · X years/months".

Example date line: "sept. 2023 - ene. 2024 · 5 months"
-> start_date: "2023-09-01", end_date: "2024-01-01"

IGNORE the "· X years/months" part. ONLY extract the two dates from that line.
Months: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Fields: company, position, start_date, end_date, description

{{raw_text}}',
(SELECT id FROM ai_schemas WHERE name = 'experience_parse'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('parse_education', 'Parse LinkedIn education', 'Extracts structured education from LinkedIn raw text',
'You are a LinkedIn data extractor. You convert education text to structured JSON.',
'Extract education to JSON. The date line has format: "month. year – month. year".

Ex: "ene. 2024 – may. 2025" -> start_year:2024, end_year:2025
Ex: "sept. 2009 – jun. 2011" -> start_year:2009, end_year:2011
Only extract the year (4 digits).

Fields: institution, degree, start_year, end_year.
Ignore "Aptitudes:", "Actividades y grupos:".

{{raw_text}}',
(SELECT id FROM ai_schemas WHERE name = 'education_parse'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('cv_generation', 'Generate CV', 'Generates structured CV content tailored to a job offer',
'You are a professional CV writer optimized for ATS (Applicant Tracking Systems). Generate structured CV content tailored to a specific job offer.',
'Generate CV content in the same language as the job offer. Return ONLY the JSON object as specified in the schema.

JOB OFFER:
Title: {{job_title}}
Company: {{company}}
Description: {{job_description}}
Offer keywords: {{job_keywords}}

USER BACKGROUND:
{{user_presentation}}
Languages: {{user_languages}}

EXPERIENCE:
{{user_experiences}}

EDUCATION:
{{user_education}}

PROJECTS:
{{user_projects}}

USER SKILLS: {{user_keywords}}

RULES:
1. PROFILE: 3-4 lines in the offer''s language. Highlight the most relevant experience and skills for this specific job. Use offer keywords naturally.
2. EXPERIENCES: Select the 1 to 3 most relevant positions. Enhance descriptions to match the offer''s keywords and requirements. 3-5 highlights each, achievement-oriented.
3. PROJECTS: Select the 1 to 3 most relevant projects. Enhance descriptions and highlights to emphasize technologies and skills matching the offer.
4. SKILLS: 4 categories, at least 8 skills per category. Pick the most relevant categories for the job (e.g. Backend, Frontend, Databases, DevOps, AI, Tools, Soft Skills...). Use the user''s known skills and infer additional ones if needed. NEVER invent nonsense technologies. All lowercase except proper nouns. If the offer mentions soft skills, include a Soft Skills category.
5. COHERENCE: Everything must be consistent. Experiences, projects, and skills should align with each other and with the profile summary.',
(SELECT id FROM ai_schemas WHERE name = 'cv_generation'),
(SELECT id FROM ai_models WHERE name = 'gpt-5.4'))
ON CONFLICT (functionality) DO NOTHING;
