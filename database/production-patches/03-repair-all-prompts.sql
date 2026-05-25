-- Repair all AI prompts if any are empty
-- Safe to run anytime — only updates prompts with empty content

UPDATE ai_prompts SET
  system_prompt = 'You are a job offer filter specialized in Software Engineering profiles.',
  user_prompt_template = 'Your task is:
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
  updated_at = NOW()
WHERE functionality = 'filter_jobs' AND (system_prompt = '' OR user_prompt_template = '');

UPDATE ai_prompts SET
  system_prompt = 'You are a job offer keyword extractor for an ATS (Applicant Tracking System). The keywords you extract will be used to match candidate profiles against job offers. Every keyword must be useful for candidate matching and filtering.',
  user_prompt_template = 'Extract keywords from this job offer, organized by category:

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
  updated_at = NOW()
WHERE functionality = 'extract_keywords' AND (system_prompt = '' OR user_prompt_template = '');

UPDATE ai_prompts SET
  system_prompt = 'You are a GitHub project analyzer for an ATS (Applicant Tracking System). Your task is to analyze a user''s repositories and extract structured information that will help match their profile to job offers. The keywords you extract will be used by recruiters and ATS systems to find candidates.',
  user_prompt_template = 'Analyze each project. For each one you must:
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
  updated_at = NOW()
WHERE functionality = 'analyze_github' AND (system_prompt = '' OR user_prompt_template = '');

UPDATE ai_prompts SET
  system_prompt = 'You are a technical keyword deduplicator for an ATS. Group keywords that represent IDENTICAL technologies into clusters. The first item in each group will be kept; the rest will be merged into it.',
  user_prompt_template = 'Group the following keywords. Rules:

1. GROUP — versions of the SAME technology:
   .net 6 + .net 8 + .net core → .net
   python3 + python 3.11 → python
   react 18 + react.js + reactjs → react
   node 20 + node.js + nodejs → node.js
   postgresql 16 + postgres → postgresql
   angular 17 + angular 18 → angular

2. GROUP — abbreviations and expanded names:
   aws + amazon web services → aws
   ci/cd + continuous integration → ci/cd
   c# + csharp + .net → c#
   js + javascript → javascript

3. DO NOT GROUP — different technologies that share a parent:
   docker ⊗ docker compose (runtime vs orchestration)
   linux ⊗ ubuntu (kernel vs distro)
   git ⊗ github ⊗ gitlab (different tools)
   spring ⊗ spring boot (framework vs extension)
   mongodb ⊗ mongoose (database vs ODM)
   kubernetes ⊗ k3s ⊗ minikube (different distros, keep separate)
   react ⊗ react native (web vs mobile)

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
  updated_at = NOW()
WHERE functionality = 'dedup_keywords' AND (system_prompt = '' OR user_prompt_template = '');

UPDATE ai_prompts SET
  system_prompt = 'You are a LinkedIn data extractor. You convert work experience text to structured JSON.',
  user_prompt_template = 'Extract work experiences to JSON. The date line ALWAYS has this exact format: "month. year - month. year · X years/months".

Example date line: "sept. 2023 - ene. 2024 · 5 months"
-> start_date: "2023-09-01", end_date: "2024-01-01"

IGNORE the "· X years/months" part. ONLY extract the two dates from that line.
Months: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Fields: company, position, start_date, end_date, description

{{raw_text}}',
  updated_at = NOW()
WHERE functionality = 'parse_experience' AND (system_prompt = '' OR user_prompt_template = '');

UPDATE ai_prompts SET
  system_prompt = 'You are a LinkedIn data extractor. You convert education text to structured JSON.',
  user_prompt_template = 'Extract education to JSON. The date line has format: "month. year – month. year".

Ex: "ene. 2024 – may. 2025" -> start_year:2024, end_year:2025
Ex: "sept. 2009 – jun. 2011" -> start_year:2009, end_year:2011
Only extract the year (4 digits).

Fields: institution, degree, start_year, end_year.
Ignore "Aptitudes:", "Actividades y grupos:".

{{raw_text}}',
  updated_at = NOW()
WHERE functionality = 'parse_education' AND (system_prompt = '' OR user_prompt_template = '');
