import { Link } from 'react-router';

export default function AdminDashboard() {
  return (
    <div>
      <h2>Admin Dashboard</h2>
      <p className="text-muted">Welcome to the 42jobs administration panel.</p>
      <div className="admin-cards">
        <Link to="/admin/ai-services" className="card admin-card">AI Services</Link>
        <Link to="/admin/ai-models" className="card admin-card">AI Models</Link>
        <Link to="/admin/ai-prompts" className="card admin-card">Prompts</Link>
        <Link to="/admin/ai-schemas" className="card admin-card">Schemas</Link>
        <Link to="/admin/templates" className="card admin-card">CV Templates</Link>
        <Link to="/admin/job-providers" className="card admin-card">Job Providers</Link>
        <Link to="/admin/dedup" className="card admin-card">Dedup Keywords</Link>
        <Link to="/admin/logs" className="card admin-card">Logs</Link>
      </div>
    </div>
  );
}
