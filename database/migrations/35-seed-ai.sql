-- 35-seed-ai.sql
-- Seed data: AI services, models and prompts.
-- Schemas are file-based: backend/src/Services/Ai/Schemas/{functionality}.{provider}.json

-- ═══════════════════════════════════════════════════════════
-- AI Services
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_services (name, is_free_tier) VALUES
    ('Google', TRUE),
    ('OpenAI', FALSE)
ON CONFLICT (name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Models (real names confirmed from API docs, May 2026)
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_models (ai_service_id, name) VALUES
    -- Google / Gemini
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.5-flash'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.1-pro-preview'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.1-flash-lite'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3-flash-preview'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-2.5-flash'),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-2.5-pro'),
    -- OpenAI
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.5'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-mini'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5-mini'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1-mini'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4.1-nano'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4o'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4o-mini')
ON CONFLICT (ai_service_id, name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Prompts
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_prompts (functionality, name, description, system_prompt, user_prompt_template, default_model_id) VALUES

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
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('extract_keywords', 'Extract keywords from job offers', 'Extracts technologies, tools, hard skills and soft skills from a job description for ATS matching',
'You are a job offer keyword extractor for an ATS (Applicant Tracking System). The keywords you extract will be used to match candidate profiles against job offers. Every keyword must be useful for candidate matching and filtering.',
'Extract keywords from this job offer, organized by category:

1. TECHNOLOGIES AND TOOLS: programming languages, frameworks, libraries, databases, cloud services, CI/CD, containerization, operating systems, testing tools, IDEs, build tools, version control, etc.
2. HARD SKILLS: specific technical abilities mentioned (e.g., "api design", "database optimization", "unit testing", "system architecture", "agile methodologies").
3. SOFT SKILLS: only include if explicitly mentioned in the offer (e.g., "communication", "leadership", "teamwork", "problem-solving").
4. COMPANY TYPE: Multinational, Startup, SME, Consultancy, or "Not identified".

IMPORTANT RULES:
- Each keyword must pass this test: "Would a recruiter or ATS search for this skill when looking for candidates?" If NO, exclude it.
- Do NOT include: generic filler words (e.g., "experience", "knowledge", "ability"), job titles or company names, or anything that does not represent a concrete skill.
- Use lowercase. Be precise (e.g., "react" not "react.js", "postgresql" not "postgres", "aws" not "amazon web services"). Prefer the most widely recognized name.
- Avoid duplicates. If the offer mentions the same skill multiple times, include it once.
- Limit to truly relevant keywords. More is not always better — quality over quantity.

Offer: "{{text}}"',
(SELECT id FROM ai_models WHERE name = 'gemini-3.1-flash-lite')),

('analyze_github', 'Analyze GitHub repositories', 'Extracts structured project information from GitHub repos for ATS profile matching',
'You are a GitHub project analyzer for an ATS (Applicant Tracking System). Your task is to analyze a user''s repositories and extract structured information that will help match their profile to job offers. The keywords you extract will be used by recruiters and ATS systems to find candidates.',
'Analyze each project. For each one you must:
1. Extract a descriptive name (clean, no hyphens, max 60 characters).
2. Generate a description in English (2-4 sentences) explaining the purpose, technologies used, and scope of the project.
3. Determine if it is a PERSONAL project or a SCHOOL/BOOTCAMP project (type: "personal" or "school"). If the README mentions "42", "42 School", "42 Barcelona", "cursus", "bootcamp" -> it is school. If it cannot be determined -> personal.
4. Extract ATS-RELEVANT keywords: technologies, tools, hard skills, and soft skills that a recruiter would search for.

KEYWORD RULES:
- INCLUDE: programming languages, frameworks, libraries, databases, cloud platforms, DevOps tools, build systems, testing frameworks, operating systems (if relevant to the project), hardware platforms, protocols, design patterns, methodologies.
- INCLUDE soft skills ONLY if the project clearly demonstrates them (e.g., "teamwork" for group projects, "project management" for coordination).
- EXCLUDE: course names, school identifiers (e.g., "42 school", "cursus", "piscine", "common core", "cadet"), assignment numbers or names (e.g., "exam02", "project42", "ft_printf", "minishell"), degree names, or any keyword that identifies a school context rather than a professional skill.
- EXCLUDE: overly broad or meaningless concepts (e.g., "coding", "programming", "software", "computer", "development", "project", "repository", "open source"). These do not differentiate candidates.
- ASK YOURSELF: "Would a recruiter or ATS search for this specific skill?" If the answer is NO, exclude it. Quality over quantity.

Projects to analyze:
{{input}}',
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
5. COHERENCE: Everything must be consistent. Experiences, projects, and skills should align with each other and with the profile summary.
6. ERROR FIELD: Set "error" to an empty string "" if generation succeeds. Use it only to report actual failures.',
(SELECT id FROM ai_models WHERE name = 'gpt-5.5'))
ON CONFLICT (functionality) DO NOTHING;
