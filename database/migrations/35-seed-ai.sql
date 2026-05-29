-- 35-seed-ai.sql
-- Seed data: AI services, models and prompts.
-- Schemas are file-based: backend/src/Services/Ai/Schemas/{functionality}.{provider}.json

-- ═══════════════════════════════════════════════════════════
-- AI Services
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_services (name, is_free_tier) VALUES
    ('Google', FALSE),
    ('OpenAI', FALSE),
    ('DeepSeek', FALSE)
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
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-4o-mini'),
    -- DeepSeek
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-v4-flash'),
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-v4-pro')
ON CONFLICT (ai_service_id, name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Prompts
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_prompts (functionality, name, description, system_prompt, user_prompt_template, default_model_id) VALUES

('filter_jobs', 'Filter job relevance', 'Determines if a job offer is relevant and junior-friendly',
'You are a job offer filter specialized in Software Engineering profiles.',
'Your task is:
1. Determine if a job offer is RELEVANT for a professional specialized in "{{keyword}}".
2. Determine if the offer is SUITABLE FOR A JUNIOR PROFILE.

RELEVANCE CRITERIA:
- A position is relevant if its responsibilities and required skills relate to "{{keyword}}". Do NOT require the words "Engineer" or "Developer" in the title — evaluate the actual work described.
- Adjacent or related roles are relevant. Use common sense: if the keyword is "Game Dev", roles like Game Designer, Game Tester, or Game Producer are adjacent and relevant.
- Completely unrelated positions such as "Sales Manager", "Recruiter", "Administrative Assistant", etc. are NOT relevant.
- If unsure after careful consideration, respond "unknown".

JUNIOR PROFILE CRITERIA (junior_friendly):
- Respond "no" if the offer requires 4 or more years of experience in any form (e.g., "4+ years", "minimum 4 years", "4-6 years", "at least 4 years").
- Respond "no" if the offer explicitly requires: "Senior", "Lead", "Principal", "Staff Engineer", "Tech Lead", "Engineering Manager", "Head of", "Director".
- Respond "yes" if the offer mentions "Junior", "Internship", "Intern", "Graduate", "Entry Level", "0-2 years", "1-3 years", or does not specify seniority level.
- If the offer asks for "3 years" or less, respond "yes" (acceptable for junior).
- If nothing is mentioned about seniority or years of experience, respond "yes".

Offer: "{{title}}"
Description: "{{description}}"',
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-pro')),

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
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-flash')),

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
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-pro')),

('dedup_keywords', 'Deduplicate keywords', 'Groups equivalent/similar keywords into clusters',
'You are a technical keyword deduplicator for an ATS. Group keywords that represent IDENTICAL technologies into clusters. The first item in each group will be kept; the rest will be merged into it.',
'Group the following keywords. Rules:

1. GROUP — versions of the SAME technology:
   .net 6 + .net 8 + .net core → .net
   python3 + python 3.11 → python
   react 18 + react.js + reactjs → react
   node 20 + node.js + nodejs → node.js
   postgresql 16 + postgres → postgresql
   angular 17 + angular 18 → angular

2. GROUP — abbreviations, expanded names and phrasal variations of the SAME concept:
   aws + amazon web services → aws
   ci/cd + continuous integration → ci/cd
   c# + csharp → c#
   js + javascript → javascript
   ml + machine learning → machine learning
   disaster recovery + disaster recovery plan + disaster recovery planning → disaster recovery
   data analysis + data analytics + data analyzing → data analysis
   project management + managing projects + project managing → project management
   NOTE: when a keyword is a more specific phrasing of another keyword representing the exact same professional skill, group them. Keep the shortest canonical form.

3. DO NOT GROUP — different technologies that share a parent or ecosystem:
   docker ⊗ docker compose (runtime vs orchestration)
   linux ⊗ ubuntu (kernel vs distro)
   git ⊗ github ⊗ gitlab (different tools)
   spring ⊗ spring boot (framework vs extension)
   mongodb ⊗ mongoose (database vs ODM)
   kubernetes ⊗ k3s ⊗ minikube (different distros, keep separate)
   react ⊗ react native (web vs mobile)
   c ⊗ c++ ⊗ c# (different programming languages)
   .net ⊗ c# ⊗ f# (framework vs languages)
   python ⊗ python3 ⊗ python 3.11 (same language, different versions → GROUP)
   xgboost ⊗ lightgbm ⊗ catboost (different ML libraries)

4. DO NOT GROUP — different frameworks/languages/clouds:
   react ⊗ vue ⊗ angular
   postgresql ⊗ mysql ⊗ mongodb
   aws ⊗ azure ⊗ gcp
   express ⊗ fastify ⊗ nestjs

5. CANONICAL FORM: put the most widely recognized form FIRST:
   "react" not "react.js" | "c#" not "csharp"
   "postgresql" not "postgres" | "node.js" not "nodejs"
   "typescript" not "ts"

6. LOWERCASE all output. No duplicates within groups.
7. If no equivalents exist, place the keyword alone in its own group.

Keywords to analyze:
{{keywords}}',
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-flash')),

('clean_keywords', 'Clean low-quality keywords', 'Identifies keywords that should be permanently removed from the system',
'You are a keyword quality filter for an ATS (Applicant Tracking System). Your task is to identify keywords that should be REMOVED because they do not represent concrete, recruiter-searchable professional skills.',
'Review this list of keywords. Return only the ones that should be REMOVED.

VALID keywords (KEEP — do NOT flag these):
- Specific technologies: programming languages (including single-letter ones like C, R, Go, D), frameworks, libraries (including ML/AI: xgboost, scikit-learn, pytorch, tensorflow, keras, lightgbm), databases, cloud services, DevOps tools, build systems, testing frameworks, operating systems, hardware platforms, protocols
- Concrete hard skills: "api design", "database optimization", "unit testing", "system architecture", "rest api", "microservices", "ci/cd", "authentication"
- Recruiter-relevant soft skills only if explicitly legitimate: "communication", "teamwork", "problem solving", "leadership", "project management"
- Proper technical names with correct casing: "c", "c#", "c++", ".net", "node.js", "react", "postgresql", "typescript", "docker", "f#"

CRITICAL RULE — DO NOT REMOVE:
- Single-character keywords that are real programming languages: "c", "r", "d", "j"
- Keywords with special characters that are real technologies: "c++", "c#", "f#"
- ML and data science libraries: "xgboost", "scikit-learn", "pytorch", "tensorflow", "keras", "lightgbm", "catboost", "pandas", "numpy", "scipy", "nltk", "spacy", "opencv"
- Cloud and infrastructure: "aws", "gcp", "azure", "terraform", "ansible", "pulumi"

COMPOUND KEYWORDS RULE (CRITICAL):
A compound keyword (2+ words) containing a generic root word is DIFFERENT from the root word alone.
Examples of VALID compounds that must be KEPT:
- "code review" / "code quality" / "code optimization" / "code generation" / "code analysis" (specific skills) ≠ "code" (generic)
- "deployment pipelines" / "deployment architecture" / "deployment workflows" / "deployment tooling" (DevOps skills) ≠ "deployment" (generic)
- "design systems" / "design tools" / "design review" / "design documentation" / "design specifications" / "design thinking" (specific skills) ≠ "design" (generic)
- "developer experience" / "developer tooling" / "developer enablement" / "developer workflows" (platform engineering) ≠ "development" (generic)
- "digital transformation" / "digital product design" / "digital asset management" / "digital marketing" / "digital forensics" (specific fields) ≠ "digital" (generic)
- "devops engineering" / "devops security" / "devtools" (specific practices) ≠ "development" (generic)
- "data analysis" / "data engineering" / "data modeling" / "data visualization" (specific skills) ≠ "data" (generic)

The question to ask: "Can a recruiter specifically search for this exact phrase?" If the compound is a recognized professional skill, KEEP IT. Only remove compound keywords when EVERY word in them is filler (e.g., "ongoing development process", "strong technical skills").

INVALID keywords (REMOVE — flag these):
- Filler/generic words: "experience", "knowledge", "ability", "skill", "proficient", "understanding", "expertise", "capability", "competence"
- Overly broad meaningless terms: "coding", "programming", "software", "computer", "development", "project", "repository", "open source", "technology", "application", "system", "engineering", "tool", "platform", "solution", "service", "implementation"
- School identifiers: "42 school", "cursus", "piscine", "common core", "cadet", "student", "bootcamp", "academic"
- Assignment/exercise names: "exam02", "project42", "ft_printf", "minishell", "libft", "get_next_line", "push_swap", "pipex", "minitalk", "philosophers"
- Job titles or company names: anything that sounds like a position or employer, not a skill
- Synonyms that are not the canonical form: prefer "react" over "reactjs", "postgresql" over "postgres"

DECISION RULE: "Would a recruiter or ATS search for this exact term when looking for a candidate?" If the answer is clearly NO, flag it for removal. ONLY flag keywords you are absolutely confident are invalid. WHEN IN DOUBT, KEEP THE KEYWORD. It is much better to keep a borderline keyword than to accidentally delete a real technology.

Keywords to analyze:
{{keywords}}',
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-flash')),

('parse_experience', 'Parse LinkedIn experience', 'Extracts structured work experience from LinkedIn raw text',
'You are a LinkedIn data extractor. You convert work experience text to structured JSON.',
'Extract work experiences to JSON. The date line ALWAYS has this exact format: "month. year - month. year · X years/months".

Example date line: "sept. 2023 - ene. 2024 · 5 months"
-> start_date: "2023-09-01", end_date: "2024-01-01"

IGNORE the "· X years/months" part. ONLY extract the two dates from that line.
Months: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Fields: company, position, start_date, end_date, description

{{raw_text}}',
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-flash')),

('parse_education', 'Parse LinkedIn education', 'Extracts structured education from LinkedIn raw text',
'You are a LinkedIn data extractor. You convert education text to structured JSON.',
'Extract education to JSON. The date line has format: "month. year – month. year".

Ex: "ene. 2024 – may. 2025" -> start_year:2024, end_year:2025
Ex: "sept. 2009 – jun. 2011" -> start_year:2009, end_year:2011
Only extract the year (4 digits).

Fields: institution, degree, start_year, end_year.
Ignore "Aptitudes:", "Actividades y grupos:".

{{raw_text}}',
(SELECT id FROM ai_models WHERE name = 'deepseek-v4-flash')),

('cv_generation', 'Generate CV', 'Generates structured CV content tailored to a job offer',
'You are an expert CV/resume writer specialized in ATS optimization and recruiter-friendly positioning.

Your task is to generate truthful, structured, ATS-compatible CV content tailored to a specific job offer.

Core principles:
1. Optimize for both modern ATS systems and human recruiters.
2. Use the same language as the job offer.
3. Prioritize relevance, clarity, keyword alignment and factual consistency.
4. Use exact keywords from the job offer naturally when they are supported by the candidate''s background.
5. Do not fabricate skills, technologies, responsibilities, achievements, certifications, degrees, employers, dates, metrics, team sizes or results.
6. You may rephrase, reorder and emphasize existing experience to better match the offer, but never create false information.
7. If a job requirement is not supported by the candidate background, do not claim it. You may emphasize adjacent or transferable experience if truthful.
8. Write in a clear, professional, achievement-oriented style.
9. Ensure the output is compatible with ATS-friendly CV templates: standard section names, concise bullet points, no icons, no decorative wording, no keyword stuffing.
10. Avoid exaggerated claims, motivational language and AI-generated writing patterns. Prefer direct and technically grounded wording.
11. Return only a valid JSON object following the requested schema. Do not include explanations, markdown or comments outside the JSON.',
'Generate tailored CV content for the following job offer and candidate background.

The CV content must be written in the same language as the job offer.

Return ONLY a valid JSON object following the schema.

JOB OFFER:
Title: {{job_title}}
Company: {{company}}
Description:
{{job_description}}

Offer keywords:
{{job_keywords}}

CANDIDATE BACKGROUND:
Professional summary / presentation:
{{user_presentation}}

Languages:
{{user_languages}}

Experience:
{{user_experiences}}

Education:
{{user_education}}

Projects:
{{user_projects}}

Candidate skills / keywords:
{{user_keywords}}

GENERATION RULES:

1. GENERAL OBJECTIVE
Create a targeted CV version for this specific job offer.
The content must maximize truthful alignment with the job requirements while remaining natural, readable and credible.

2. LANGUAGE
Use the same language as the job offer.
If the offer contains multiple languages, use the dominant language.

3. PROFILE
Write a concise professional profile of 3 to 4 lines.
Highlight the most relevant experience, domain knowledge, technologies and strengths for the target role.
Use important job-offer keywords naturally, only when supported by the candidate background.

4. EXPERIENCE
Select the most relevant experiences for the target role.
Prefer recent and directly relevant experience.
Include 1 to 4 positions depending on relevance.
Do not invent employers, job titles, dates, responsibilities or technologies.
For each selected experience:
- Keep the original role and company unless a translated version is needed.
- Write a short description aligned with the target role.
- Write 3 to 5 bullet highlights.
- Make bullets achievement-oriented and specific.
- Use strong action verbs.
- Include metrics only if they are present in the candidate background.
- Naturally include relevant keywords from the offer when truthful.
- Avoid generic filler.

5. PROJECTS
Select 1 to 3 projects most relevant to the offer.
For each project:
- Explain the purpose and relevance of the project.
- Include 2 to 4 bullet highlights.
- Mention technologies, methodologies and responsibilities only if supported by the provided background.
- Prioritize projects that demonstrate skills required in the job offer.

6. EDUCATION
Include relevant education from the candidate background.
Do not create degrees, institutions, certifications or dates.
Translate degree names only if appropriate and safe.

7. SKILLS
Create 3 to 5 skill categories relevant to the job.
Each category should contain 5 to 10 skills.
Use only skills explicitly provided or strongly evidenced by the background.
Prioritize exact job-offer terms when truthful.
Preserve correct capitalization for technologies and tools.
Do not add unrelated skills just to fill categories.
If the offer emphasizes soft skills, include a Soft Skills category.

8. ATS OPTIMIZATION
Use standard terminology likely to be recognized by ATS.
Include both acronyms and expanded forms when useful and natural.
Avoid keyword stuffing. The CV should read naturally to a human recruiter.

9. CONSISTENCY
Ensure the profile, experience, projects and skills are coherent with each other.
Do not mention a technology in skills if it is contradicted by the background.
Do not claim seniority, leadership, management or specialization unless supported by the candidate information.

10. ERROR FIELD
If generation succeeds, set error to an empty string.
If required input is missing or impossible to process, set error with a concise explanation and leave unsupported fields empty.',
(SELECT id FROM ai_models WHERE name = 'gpt-5.5'))
ON CONFLICT (functionality) DO NOTHING;
