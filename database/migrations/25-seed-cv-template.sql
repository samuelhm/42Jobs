-- 25-seed-cv-template.sql
-- Seed data: default CV template

INSERT INTO cv_templates (name, description, html_template, css, is_active) VALUES (
    'Default',
    'Clean ATS-friendly CV template with profile, experience, projects, skills, and education sections.',
    '<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<style>
  * { margin:0;padding:0;box-sizing:border-box }
  body { font-family:Helvetica Neue,Arial,sans-serif;font-size:10pt;line-height:1.5;color:#222;max-width:700px;margin:0 auto;padding:1.5rem 2rem }
  header { text-align:center;margin-bottom:1.2rem;padding-bottom:.8rem;border-bottom:2px solid #333 }
  h1 { font-size:1.4rem;font-weight:700;margin-bottom:.15rem }
  .subtitle { font-size:.9rem;color:#555;margin-bottom:.35rem }
  .contact { font-size:.75rem;color:#666 }
  .contact a { color:#2563eb;text-decoration:none }
  section { margin-bottom:1rem }
  h2 { font-size:.8rem;text-transform:uppercase;letter-spacing:.1em;color:#333;border-bottom:1px solid #ccc;padding-bottom:.15rem;margin-bottom:.4rem;font-weight:700 }
  .profile-text { font-size:.85rem;color:#444;line-height:1.6 }
  .entry { margin-bottom:.5rem }
  .entry-header { font-size:.85rem;font-weight:600 }
  .entry-dates { color:#888;font-size:.75rem }
  .entry-desc { font-size:.8rem;color:#555;margin-bottom:.15rem }
  ul { padding-left:1.2rem;margin-top:.1rem }
  li { font-size:.78rem;color:#444;margin-bottom:.08rem }
  .skills-grid { display:flex;flex-wrap:wrap;gap:.8rem 1.5rem }
  .skill-group { flex:1;min-width:140px }
  .skill-group h3 { font-size:.75rem;font-weight:600;color:#555;margin-bottom:.15rem }
  .skill-tags { display:flex;flex-wrap:wrap;gap:.2rem }
  .skill-tag { background:#f0f4ff;color:#2563eb;font-size:.7rem;padding:.1rem .4rem;border-radius:3px;white-space:nowrap }
  .edu-entry { font-size:.82rem;color:#444 }
  .lang-tags { display:flex;gap:.5rem;flex-wrap:wrap;font-size:.8rem;color:#555 }
  @media print { body { padding:0 } }
</style>
</head>
<body>
<header>
  <h1>{{name}}</h1>
  <div class="subtitle">{{job_title}} &mdash; {{company}}</div>
  <div class="contact">{{email}} | {{phone}} | <a href="{{linkedin}}">{{linkedin}}</a> | <a href="{{github}}">{{github}}</a> | {{location}}</div>
</header>
<section><h2>Profile</h2><p class="profile-text">{{profile}}</p></section>
<section><h2>Experience</h2>{{experiences}}</section>
<section><h2>Projects</h2>{{projects}}</section>
<section><h2>Education</h2>{{education}}</section>
<section><h2>Skills</h2><div class="skills-grid">{{skills}}</div></section>
<section><h2>Languages</h2><div class="lang-tags">{{languages}}</div></section>
</body>
</html>',
    NULL,
    TRUE
) ON CONFLICT DO NOTHING;
