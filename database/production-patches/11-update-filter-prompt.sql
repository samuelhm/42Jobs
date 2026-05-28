-- 11-update-filter-prompt.sql
-- Makes AI filter more flexible on relevance and stricter on junior experience.

UPDATE ai_prompts SET
  user_prompt_template = 'Your task is:
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
  updated_at = NOW()
WHERE functionality = 'filter_jobs';
