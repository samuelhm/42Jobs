-- Update dedup_keywords prompt with improved rules
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
{{keywords}}'
WHERE functionality = 'dedup_keywords';

-- Update clean_keywords prompt with CRITICAL RULE and stronger protections
UPDATE ai_prompts SET
  system_prompt = 'You are a keyword quality filter for an ATS (Applicant Tracking System). Your task is to identify keywords that should be REMOVED because they do not represent concrete, recruiter-searchable professional skills.',
  user_prompt_template = 'Review this list of keywords. Return only the ones that should be REMOVED.

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
{{keywords}}'
WHERE functionality = 'clean_keywords';
