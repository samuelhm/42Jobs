-- Production patch: refined dedup_keywords AI prompt
-- Run this on already-provisioned databases only

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
WHERE functionality = 'dedup_keywords';
