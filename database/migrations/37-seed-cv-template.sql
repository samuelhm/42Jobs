-- 37-seed-cv-template.sql
-- Seed data: ATS-optimized CV template with comma-separated skills and project dates

INSERT INTO cv_templates (name, description, html_template, css, is_active) VALUES (
    'ATS Optimized',
    'ATS-friendly CV template with comma-separated skills, project dates, and clean typography.',
    '<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<style>
  * { margin:0;padding:0;box-sizing:border-box }
  body { font-family:''Helvetica Neue'',Arial,sans-serif;font-size:10pt;line-height:1.55;color:#1a1a1a;max-width:720px;margin:0 auto;padding:1.8rem 2rem }
  header { text-align:center;margin-bottom:1.4rem;padding-bottom:.9rem;border-bottom:2.5px solid #2c3e50 }
  h1 { font-size:1.5rem;font-weight:700;margin-bottom:.1rem;color:#2c3e50 }
  .subtitle { font-size:.9rem;color:#555;margin-bottom:.3rem;font-weight:500 }
  .contact { font-size:.75rem;color:#777;line-height:1.6 }
  .contact a { color:#2980b9;text-decoration:none }
  section { margin-bottom:1.1rem }
  h2 { font-size:.78rem;text-transform:uppercase;letter-spacing:.12em;color:#2c3e50;border-bottom:1.2px solid #bdc3c7;padding-bottom:.18rem;margin-bottom:.45rem;font-weight:700 }
  .profile-text { font-size:.84rem;color:#333;line-height:1.65 }
  .entry { margin-bottom:.55rem }
  .entry-header { font-size:.84rem;font-weight:600;color:#1a1a1a }
  .entry-dates { color:#7f8c8d;font-size:.76rem;margin-bottom:.1rem }
  .entry-desc { font-size:.8rem;color:#555;margin-bottom:.1rem;line-height:1.45 }
  ul { padding-left:1.2rem;margin-top:.1rem }
  li { font-size:.77rem;color:#444;margin-bottom:.06rem;line-height:1.45 }
  .skill-group { margin-bottom:.22rem }
  .skill-group h3 { display:inline;font-weight:600;font-size:.82rem;color:#2c3e50 }
  .skill-group h3::after { content:": " }
  .skill-tags { display:inline }
  .skill-tag { display:inline;background:none;color:#333;padding:0;font-size:.82rem }
  .skill-tag:not(:last-child)::after { content:", " }
  .edu-entry { font-size:.82rem;color:#444;margin-bottom:.1rem }
  .lang-tags { display:flex;gap:.4rem;flex-wrap:wrap;font-size:.8rem;color:#666 }
  @media print { body { padding:0 } }
</style>
</head>
<body>
<header>
  <h1>{{name}}</h1>
  <div class="subtitle">{{job_title}} &mdash; {{company}}</div>
  <div class="contact">{{email}} | {{phone}} | <a href="{{linkedin}}">{{linkedin}}</a> | <a href="{{github}}">{{github}}</a> | {{location}}</div>
</header>
<section><h2>Professional Profile</h2><p class="profile-text">{{profile}}</p></section>
<section><h2>Experience</h2>{{experiences}}</section>
<section><h2>Projects</h2>{{projects}}</section>
<section><h2>Education</h2>{{education}}</section>
<section><h2>Skills</h2>{{skills}}</section>
<section><h2>Languages</h2><div class="lang-tags">{{languages}}</div></section>
</body>
</html>',
    NULL,
    TRUE
) ON CONFLICT DO NOTHING;
