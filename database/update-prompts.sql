-- Update extract_keywords prompt
UPDATE ai_prompts SET
  description = $p$Extracts technologies, tools, hard skills and soft skills from a job description for ATS matching$p$,
  system_prompt = $p$You are a job offer keyword extractor for an ATS (Applicant Tracking System). The keywords you extract will be used to match candidate profiles against job offers. Every keyword must be useful for candidate matching and filtering.$p$,
  user_prompt_template = $p$Extract keywords from this job offer, organized by category:

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

Offer: "{{text}}"$p$
WHERE functionality = 'extract_keywords';

-- Update analyze_github prompt
UPDATE ai_prompts SET
  description = $p$Extracts structured project information from GitHub repos for ATS profile matching$p$,
  system_prompt = $p$You are a GitHub project analyzer for an ATS (Applicant Tracking System). Your task is to analyze a user's repositories and extract structured information that will help match their profile to job offers. The keywords you extract will be used by recruiters and ATS systems to find candidates.$p$,
  user_prompt_template = $p$Analyze each project. For each one you must:
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
{{input}}$p$
WHERE functionality = 'analyze_github';
