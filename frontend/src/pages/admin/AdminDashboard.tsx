import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { get } from '../../utils/api';

interface QueueStats {
  queued: number;
  running: number;
  completed: number;
  failed: number;
  fetch_all_running: boolean;
}

const dash = [
  { to: '/admin/ai-services', icon: '🔌', label: 'AI Services', desc: 'API keys & providers' },
  { to: '/admin/ai-models', icon: '🧠', label: 'AI Models', desc: 'Manage & assign models' },
  { to: '/admin/ai-prompts', icon: '💬', label: 'Prompts', desc: 'System & user templates' },
  { to: '/admin/templates', icon: '📄', label: 'CV Templates', desc: 'HTML & CSS layouts' },
  { to: '/admin/job-providers', icon: '🌐', label: 'Job Providers', desc: 'API sources & keys' },
  { to: '/admin/utils', icon: '🛠️', label: 'Utils', desc: 'Dedup & cleanup' },
  { to: '/admin/logs', icon: '📋', label: 'Logs', desc: 'Request & response logs' },
];

export default function AdminDashboard() {
  const [stats, setStats] = useState<QueueStats | null>(null);

  useEffect(() => {
    let cancelled = false;
    const fetchStats = async () => {
      try {
        const res = await get<QueueStats>('/api/admin/queue-stats');
        if (!cancelled && res.success) setStats(res.data);
      } catch { /* polling silently fails */ }
    };
    fetchStats();
    const id = setInterval(fetchStats, 5000);
    return () => { cancelled = true; clearInterval(id); };
  }, []);

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

      {stats && (
        <div className="queue-stats">
          <h3>Job Queue</h3>
          <div className="queue-stats-grid">
            <div className="queue-stat">
              <span className="queue-stat-value">{stats.queued}</span>
              <span className="queue-stat-label">Queued</span>
            </div>
            <div className="queue-stat running">
              <span className="queue-stat-value">{stats.running}</span>
              <span className="queue-stat-label">Running</span>
            </div>
            <div className="queue-stat ok">
              <span className="queue-stat-value">{stats.completed}</span>
              <span className="queue-stat-label">Completed</span>
            </div>
            <div className="queue-stat error">
              <span className="queue-stat-value">{stats.failed}</span>
              <span className="queue-stat-label">Failed</span>
            </div>
            <div className={`queue-stat ${stats.fetch_all_running ? 'running' : 'ok'}`}>
              <span className="queue-stat-value">{stats.fetch_all_running ? 'Yes' : 'No'}</span>
              <span className="queue-stat-label">Fetch All</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
