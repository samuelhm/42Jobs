import { Link } from 'react-router';

const dash = [
  { to: '/admin/ai-services', icon: '🔌', label: 'AI Services', desc: 'API keys & providers' },
  { to: '/admin/ai-models', icon: '🧠', label: 'AI Models', desc: 'Default model & active' },
  { to: '/admin/ai-prompts', icon: '💬', label: 'Prompts', desc: 'System & user templates' },
  { to: '/admin/templates', icon: '📄', label: 'CV Templates', desc: 'HTML & CSS layouts' },
  { to: '/admin/job-providers', icon: '🌐', label: 'Job Providers', desc: 'API sources & keys' },
  { to: '/admin/dedup', icon: '🔄', label: 'Dedup Keywords', desc: 'Merge duplicates' },
  { to: '/admin/logs', icon: '📋', label: 'Logs', desc: 'Coming soon' },
];

export default function AdminDashboard() {
  return (
    <div>
      <h2>Admin Dashboard</h2>
      <p className="text-muted">Configure all aspects of 42jobs from here.</p>
      <div className="admin-dash-grid">
        {dash.map(d => (
          <Link key={d.to} to={d.to} className="admin-dash-card">
            <span className="icon">{d.icon}</span>
            <span>{d.label}</span>
            <span className="label">{d.desc}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
