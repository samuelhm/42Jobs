import { useEffect, useState } from 'react';
import { get, post, del } from '../../utils';
import { AiNotConfiguredModal } from '../../components';

interface CategoryInfo {
  id: number;
  name: string;
  last_fetched_at: string | null;
  job_count: number;
  subscriber_count: number;
}

export default function AdminUtils() {
  return (
    <div>
      <h2>Utils</h2>
      <p className="text-muted">Dangerous operations — use with caution.</p>
      <DedupSection />
      <CleanSection />
      <CategorySection />
    </div>
  );
}

function DedupSection() {
  const [msg, setMsg] = useState('');
  const [running, setRunning] = useState(false);
  const [aiError, setAiError] = useState('');

  async function runDedup() {
    setRunning(true);
    setMsg('Running dedup...');
    const res = await post<{ message: string; merged: number }>('/api/admin/dedup-keywords', {});
    if (res.status === 503) {
      setAiError(res.error || 'AI not configured');
      setRunning(false);
      setMsg('');
      return;
    }
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
      {aiError && <AiNotConfiguredModal message={aiError} onClose={() => setAiError('')} />}
    </div>
  );
}

function CleanSection() {
  const [msg, setMsg] = useState('');
  const [running, setRunning] = useState(false);
  const [aiError, setAiError] = useState('');

  async function runClean() {
    setRunning(true);
    setMsg('Running cleanup...');
    const res = await post<{ message: string; removed: number }>('/api/admin/clean-keywords', {});
    if (res.status === 503) {
      setAiError(res.error || 'AI not configured');
      setRunning(false);
      setMsg('');
      return;
    }
    setMsg(res.success ? res.data.message : 'Error');
    setRunning(false);
  }

  return (
    <div className="service-card" style={{ marginBottom: '1rem' }}>
      <h3>Clean Keywords</h3>
      <p className="text-muted" style={{ marginBottom: '0.75rem' }}>Uses AI to remove low-quality keywords (filler words, overly broad terms, school identifiers, assignment names).</p>
      <button className="admin-btn" onClick={runClean} disabled={running}>
        {running ? 'Running...' : 'Run Clean'}
      </button>
      {msg && <div style={{ marginTop: '0.75rem', padding: '0.75rem', background: 'var(--bg)', borderRadius: 'var(--radius)', border: '1px solid var(--border)', fontSize: '0.75rem' }}>{msg}</div>}
      {aiError && <AiNotConfiguredModal message={aiError} onClose={() => setAiError('')} />}
    </div>
  );
}

function CategorySection() {
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState('');

  async function load() {
    try {
      const res = await get<CategoryInfo[]>('/api/admin/categories');
      if (res.success) {
        const sorted = [...res.data].sort((a, b) => a.name.localeCompare(b.name));
        setCategories(sorted);
      }
    } catch {}
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function deleteCategory(cat: CategoryInfo) {
    if (!confirm(`Delete "${cat.name}"? This will remove all its ${cat.job_count} jobs from the system.`)) return;
    setMsg('');
    try {
      const res = await del<null>(`/api/admin/categories/${cat.id}`);
      if (res.success) {
        setMsg(`Deleted "${cat.name}" and all its jobs.`);
        setCategories(prev => prev.filter(c => c.id !== cat.id));
      } else {
        setMsg(res.error || 'Deletion failed');
      }
    } catch {
      setMsg('Connection error');
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
                <span className="util-meta">{c.job_count} jobs · {c.subscriber_count} users · {c.last_fetched_at ? new Date(c.last_fetched_at).toLocaleDateString() : 'never'}</span>
              </div>
              <button className="btn-delete" onClick={() => deleteCategory(c)}>Delete</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
