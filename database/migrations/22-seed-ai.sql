-- 22-seed-ai.sql
-- Seed data: AI services, models, schemas and prompts

-- ═══════════════════════════════════════════════════════════
-- AI Services
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_services (name, base_url) VALUES
    ('Google', 'https://generativelanguage.googleapis.com/'),
    ('OpenAI', 'https://api.openai.com/')
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
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-pro'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.5'),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.5-pro')
ON CONFLICT (ai_service_id, name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Schemas
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_schemas (name, description, json_schema) VALUES

('job_filter', 'Filters job relevance and junior suitability', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if the model cannot determine the result."
    },
    "relevant": {
      "type": "STRING",
      "description": "\"yes\" if the offer is clearly relevant for the profile, \"no\" if it clearly is not, \"unknown\" if uncertain."
    },
    "junior_friendly": {
      "type": "STRING",
      "description": "\"no\" if the offer explicitly requires a senior profile or more than 4 years of experience. \"yes\" otherwise."
    }
  },
  "required": ["relevant", "junior_friendly"]
}'),

('keyword_extraction', 'Extracts technologies and company type from job offers', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if extraction fails."
    },
    "skills": {
      "type": "ARRAY",
      "items": { "type": "STRING" },
      "description": "Exhaustive list of ALL technologies, languages, frameworks, tools, and soft skills mentioned in the offer."
    },
    "company_type": {
      "type": "STRING",
      "description": "Company type: Multinational, Startup, SME, Consultancy, or \"Not identified\"."
    }
  },
  "required": ["skills", "company_type"]
}'),

('github_projects', 'Extracts project info from GitHub repositories', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if analysis fails."
    },
    "projects": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "name": { "type": "STRING" },
          "description": { "type": "STRING" },
          "type": { "type": "STRING", "enum": ["personal", "school"] },
          "keywords": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["name", "description", "type", "keywords"]
      }
    }
  },
  "required": ["projects"]
}'),

('keyword_dedup', 'Groups duplicate/similar keywords', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if dedup fails."
    },
    "groups": {
      "type": "ARRAY",
      "items": {
        "type": "ARRAY",
        "items": { "type": "STRING" }
      }
    }
  },
  "required": ["groups"]
}'),

('experience_parse', 'Parses LinkedIn experience text into structured data', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if parsing fails."
    },
    "experiences": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "company": { "type": "STRING" },
          "position": { "type": "STRING" },
          "start_date": { "type": "STRING" },
          "end_date": { "type": "STRING" },
          "description": { "type": "STRING" }
        },
        "required": ["company"]
      }
    }
  },
  "required": ["experiences"]
}'),

('education_parse', 'Parses LinkedIn education text into structured data', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if parsing fails."
    },
    "education": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "institution": { "type": "STRING" },
          "degree": { "type": "STRING" },
          "start_year": { "type": "NUMBER" },
          "end_year": { "type": "NUMBER" }
        },
        "required": ["degree"]
      }
    }
  },
  "required": ["education"]
}'),

('cv_generation', 'Generates structured CV data from user profile and job offer', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if generation fails."
    },
    "contact": {
      "type": "OBJECT",
      "properties": {
        "name": { "type": "STRING" },
        "email": { "type": "STRING" },
        "phone": { "type": "STRING" },
        "linkedin": { "type": "STRING" },
        "github": { "type": "STRING" },
        "location": { "type": "STRING" }
      },
      "required": ["name", "email"]
    },
    "summary": { "type": "STRING" },
    "experiences": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "company": { "type": "STRING" },
          "position": { "type": "STRING" },
          "start_date": { "type": "STRING" },
          "end_date": { "type": "STRING" },
          "highlights": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["company", "position", "highlights"]
      }
    },
    "projects": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "name": { "type": "STRING" },
          "description": { "type": "STRING" },
          "highlights": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["name", "highlights"]
      }
    },
    "education": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "degree": { "type": "STRING" },
          "institution": { "type": "STRING" },
          "year": { "type": "STRING" }
        },
        "required": ["degree", "institution"]
      }
    },
    "skills": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "category": { "type": "STRING" },
          "items": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["category", "items"]
      }
    },
    "languages": {
      "type": "ARRAY",
      "items": { "type": "STRING" }
    },
    "html": {
      "type": "STRING",
      "description": "The complete rendered CV as HTML with inline CSS. No markdown."
    }
  },
  "required": ["contact", "summary", "skills", "html"]
}')
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
(SELECT id FROM ai_models WHERE name = 'gpt-5.4-nano')),

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
(SELECT id FROM ai_models WHERE name = 'gpt-5.4-nano')),

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
(SELECT id FROM ai_models WHERE name = 'gpt-5.4-nano')),

('cv_generation', 'Generate CV', 'Generates a structured CV from user profile and job offer',
'You are a professional CV generator optimized for ATS (Applicant Tracking Systems). Generate structured CV data AND a fully rendered HTML version.',
'Generate a CV in the same language as the job offer. If the offer is in Spanish, CV in Spanish. If in English, CV in English.
OUTPUT FORMAT: Return a JSON object with BOTH structured data fields AND an "html" field containing the complete rendered CV as HTML with inline CSS.

JOB OFFER:
Title: {{job_title}}
Company: {{company}}
Description: {{job_description}}
Offer keywords: {{job_keywords}}

USER PROFILE:
Name: {{user_name}}
Email: {{user_email}}
Phone: {{user_phone}}
Location: {{user_location}}
LinkedIn: {{user_linkedin}}
GitHub: {{user_github}}
Summary: {{user_presentation}}
Languages: {{user_languages}}

EXPERIENCE:
{{user_experiences}}

EDUCATION:
{{user_education}}

PROJECTS:
{{user_projects}}

USER KEYWORDS (learned): {{user_keywords}}

INSTRUCTIONS:
1. Contact: full name, email, phone, linkedin, github, location.
2. Summary: 3-4 lines in the offer''s language highlighting the most relevant experience.
3. Experiences: MIN 1, MAX 3. Most relevant first, with highlights using offer keywords.
4. Projects: MIN 1, MAX 3. Most relevant first.
5. Education: max 3, most recent first.
6. Skills: Grouped by category (Backend, Frontend, Databases, DevOps, AI, Tools, Soft Skills...). MIN 8 skills per category. If the offer mentions soft skills, include a Soft Skills category with at least 8 relevant soft skills.
7. Languages: names only (no level).

HTML REQUIREMENTS:
- A4-friendly layout, Arial/Helvetica font
- Section titles (h2) visibly larger than content
- Sections separated by subtle <hr>
- Contact info on one line separated by |
- If CV in Spanish: profile photo <img> on the left using /resources/YoFinal.webp as a round image. If English: NO photo.
- NO external fonts, NO emojis, NO markdown
- Minimal CSS, no flashy colors

FINAL REVIEW: Check the CV against the offer. Add missing keywords. Improve any descriptions for ATS compatibility.',
(SELECT id FROM ai_schemas WHERE name = 'cv_generation'),
(SELECT id FROM ai_models WHERE name = 'gemini-3.5-flash'))
ON CONFLICT (functionality) DO NOTHING;
