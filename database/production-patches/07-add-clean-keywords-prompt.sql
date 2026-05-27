-- Add clean_keywords prompt to existing production database
-- Safe to run anytime — won't overwrite existing content

UPDATE ai_prompts SET
  system_prompt = 'You are a keyword quality filter for an ATS (Applicant Tracking System). Your task is to identify keywords that should be REMOVED because they do not represent concrete, recruiter-searchable professional skills.',
  user_prompt_template = 'Review this list of keywords. Return only the ones that should be REMOVED.

VALID keywords (KEEP — do NOT flag these):
- Specific technologies: programming languages, frameworks, libraries, databases, cloud services, DevOps tools, build systems, testing frameworks, operating systems, hardware platforms, protocols
- Concrete hard skills: "api design", "database optimization", "unit testing", "system architecture", "rest api", "microservices", "ci/cd", "authentication"
- Recruiter-relevant soft skills only if explicitly legitimate: "communication", "teamwork", "problem solving", "leadership", "project management"
- Proper technical names with correct casing: "c#", "c++", ".net", "node.js", "react", "postgresql", "typescript", "docker"

INVALID keywords (REMOVE — flag these):
- Filler/generic words: "experience", "knowledge", "ability", "skill", "proficient", "understanding", "expertise", "capability", "competence"
- Overly broad meaningless terms: "coding", "programming", "software", "computer", "development", "project", "repository", "open source", "technology", "application", "system", "engineering", "tool", "platform", "solution", "service", "implementation"
- School identifiers: "42 school", "cursus", "piscine", "common core", "cadet", "student", "bootcamp", "academic"
- Assignment/exercise names: "exam02", "project42", "ft_printf", "minishell", "libft", "get_next_line", "push_swap", "pipex", "minitalk", "philosophers"
- Job titles or company names: anything that sounds like a position or employer, not a skill
- Synonyms that are not the canonical form: prefer "react" over "reactjs", "postgresql" over "postgres"

DECISION RULE: "Would a recruiter or ATS search for this exact term when looking for a candidate?" If the answer is clearly NO, flag it for removal. Only flag keywords you are confident are invalid. When in doubt, KEEP.

Keywords to analyze:
{{keywords}}',
  updated_at = NOW()
WHERE functionality = 'clean_keywords' AND (system_prompt = '' OR user_prompt_template = '');

INSERT INTO ai_prompts (functionality, name, description, system_prompt, user_prompt_template, default_model_id) VALUES (
  'clean_keywords',
  'Clean low-quality keywords',
  'Identifies keywords that should be permanently removed from the system',
  'You are a keyword quality filter for an ATS (Applicant Tracking System). Your task is to identify keywords that should be REMOVED because they do not represent concrete, recruiter-searchable professional skills.',
  'Review this list of keywords. Return only the ones that should be REMOVED.

VALID keywords (KEEP — do NOT flag these):
- Specific technologies: programming languages, frameworks, libraries, databases, cloud services, DevOps tools, build systems, testing frameworks, operating systems, hardware platforms, protocols
- Concrete hard skills: "api design", "database optimization", "unit testing", "system architecture", "rest api", "microservices", "ci/cd", "authentication"
- Recruiter-relevant soft skills only if explicitly legitimate: "communication", "teamwork", "problem solving", "leadership", "project management"
- Proper technical names with correct casing: "c#", "c++", ".net", "node.js", "react", "postgresql", "typescript", "docker"

INVALID keywords (REMOVE — flag these):
- Filler/generic words: "experience", "knowledge", "ability", "skill", "proficient", "understanding", "expertise", "capability", "competence"
- Overly broad meaningless terms: "coding", "programming", "software", "computer", "development", "project", "repository", "open source", "technology", "application", "system", "engineering", "tool", "platform", "solution", "service", "implementation"
- School identifiers: "42 school", "cursus", "piscine", "common core", "cadet", "student", "bootcamp", "academic"
- Assignment/exercise names: "exam02", "project42", "ft_printf", "minishell", "libft", "get_next_line", "push_swap", "pipex", "minitalk", "philosophers"
- Job titles or company names: anything that sounds like a position or employer, not a skill
- Synonyms that are not the canonical form: prefer "react" over "reactjs", "postgresql" over "postgres"

DECISION RULE: "Would a recruiter or ATS search for this exact term when looking for a candidate?" If the answer is clearly NO, flag it for removal. Only flag keywords you are confident are invalid. When in doubt, KEEP.

Keywords to analyze:
{{keywords}}',
  (SELECT id FROM ai_models WHERE name = 'deepseek-v4-flash')
) ON CONFLICT (functionality) DO NOTHING;
