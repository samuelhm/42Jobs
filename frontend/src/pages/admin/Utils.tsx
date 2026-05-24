import { useEffect, useState } from 'react';
import { get, post, del } from '../../utils';

interface CategoryInfo {
  id: number;
  name: string;
  last_fetched_at: string | null;
  job_count: number;
}

export default function AdminUtils() {
  return (
    <div>
      <h2>Utils</h2>
      <p className="text-muted">Dangerous operations — use with caution.</p>
      <DedupSection />
      <CategorySection />
    </div>
  );
}

function DedupSection() {
  const [msg, setMsg] = useState('');
  const [running, setRunning] = useState(false);

  async function runDedup() {
    setRunning(true);
    setMsg('Running dedup...');
    const res = await post<{ message: string; merged: number }>('/api/admin/dedup-keywords', {});
    setMsg(res.success ? res.data.message : 'Error');
    setRunning(false);
  }

  return (
    <div className="service-card" style={{ marginBottom: '1rem' }}>
      <h3>Deduplicate Keywords</h3>
      <p className="text-muted" style={{ marginBottom: '0.75rem' }}>Uses AI to find and merge duplicate/similar keywords across all tables.</p>
      <button className="admin-btn" onClick={runDedup} disabled={running}>
        {running ? 'Running...' : 'Run Dedup'}
      </button>
      {msg && <div style={{ marginTop: '0.75rem', padding: '0.75rem', background: 'var(--bg)', borderRadius: 'var(--radius)', border: '1px solid var(--border)', fontSize: '0.75rem' }}>{msg}</div>}
    </div>
  );
}

function CategorySection() {
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState('');

  async function load() {
    const res = await get<CategoryInfo[]>('/api/categories/available');
    if (res.success) setCategories(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function deleteCategory(cat: CategoryInfo) {
    if (!confirm(`Delete "${cat.name}"? This will remove all its ${cat.job_count} jobs from the system.`)) return;
    setMsg('');
    const res = await del(`/api/admin/categories/${cat.id}`);
    if (res.success) {
      setMsg(`Deleted "${cat.name}" and all its jobs.`);
      setCategories(prev => prev.filter(c => c.id !== cat.id));
    } else {
      setMsg(res.error || 'Deletion failed');
    }
    setTimeout(() => setMsg(''), 6000);
  }

  if (loading) return null;

  return (
    <div className="service-card">
      <h3>Delete Categories</h3>
      <p className="text-muted" style={{ marginBottom: '0.75rem' }}>Permanently removes a category and all its non-tracked jobs. Only jobs with no user interactions are deleted — active jobs survive.</p>
      {msg && (
        <div style={{ marginBottom: '0.75rem', padding: '0.5rem 0.75rem', background: 'rgba(77,184,160,0.08)', borderRadius: 'var(--radius)', border: '1px solid rgba(77,184,160,0.2)', fontSize: '0.72rem', color: 'var(--teal)' }}>
          {msg}
        </div>
      )}
      {categories.length === 0 ? (
        <p className="text-muted" style={{ fontStyle: 'italic' }}>No categories to display.</p>
      ) : (
        <div className="util-list">
          {categories.map(c => (
            <div key={c.id} className="util-row">
              <div>
                <span className="util-name">{c.name}</span>
                <span className="util-meta">{c.job_count} jobs · {c.last_fetched_at ? new Date(c.last_fetched_at).toLocaleDateString() : 'never'}</span>
              </div>
              <button className="btn-delete" onClick={() => deleteCategory(c)}>Delete</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
