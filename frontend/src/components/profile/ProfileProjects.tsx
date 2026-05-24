import { useState, useEffect, useRef } from 'react';

interface Project {
  id: number;
  name: string;
  description: string | null;
  type: string;
  keywords: string[];
}

export default function ProfileProjects() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState({ name: '', description: '', type: 'personal' });
  const [editingId, setEditingId] = useState<number | null>(null);
  const [ghUsername, setGhUsername] = useState('');
  const [ghToken, setGhToken] = useState('');
  const [importing, setImporting] = useState(false);
  const [importStatus, setImportStatus] = useState<{ message: string; type: 'info' | 'success' | 'error'; processed?: number; total?: number } | null>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const activeJobRef = useRef<string | null>(null);

  async function load() {
    const res = await fetch('/api/projects');
    const data = await res.json();
    if (data.success) setProjects(data.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  useEffect(() => {
    return () => {
      if (pollRef.current) clearInterval(pollRef.current);
    };
  }, []);

  useEffect(() => {
    if (activeJobRef.current) {
      setImporting(true);
      pollImport(activeJobRef.current);
    }
  }, []);

  function resetForm() {
    setForm({ name: '', description: '', type: 'personal' });
    setEditingId(null);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const body = {
      name: form.name,
      description: form.description || null,
      type: form.type,
    };
    const url = editingId ? `/api/projects/${editingId}` : '/api/projects';
    const method = editingId ? 'PUT' : 'POST';
    const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    const data = await res.json();
    if (data.success) { resetForm(); load(); }
  }

  function handleEdit(p: Project) {
    setForm({ name: p.name, description: p.description || '', type: p.type });
    setEditingId(p.id);
  }

  async function handleDelete(id: number) {
    if (!confirm('Delete?')) return;
    await fetch(`/api/projects/${id}`, { method: 'DELETE' });
    load();
  }

  async function handleImport() {
    const username = ghUsername.trim();
    if (!username || !ghToken.trim()) return;
    setImporting(true);
    setImportStatus({ message: 'Starting import — this may take several minutes while AI analyzes your repositories.', type: 'info' });

    try {
      const res = await fetch('/api/projects/import-github', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, token: ghToken || null }),
      });
      const data = await res.json();

      const jobId = data.job_id;
      if (!jobId) {
        setImportStatus({ message: 'Failed to start import', type: 'error' });
        setTimeout(() => setImportStatus(null), 4000);
        setImporting(false);
        return;
      }

      activeJobRef.current = jobId;

      await new Promise<void>((resolve) => { pollImportInternal(jobId, resolve); });
    } catch {
      setImportStatus({ message: 'Connection error', type: 'error' });
      setTimeout(() => setImportStatus(null), 4000);
    } finally {
      setImporting(false);
    }
  }

  function pollImportInternal(jobId: string, resolve: () => void) {
    pollRef.current = setInterval(async () => {
      try {
        const r = await fetch(`/api/projects/import-github/${jobId}`);
        const d = await r.json();
        if (d.status === 'completed') {
          clearInterval(pollRef.current!);
          activeJobRef.current = null;
          setImportStatus({ message: `${d.inserted} projects imported`, type: 'success' });
          setTimeout(() => setImportStatus(null), 12000);
          load();
          resolve();
        } else if (d.status === 'failed') {
          clearInterval(pollRef.current!);
          activeJobRef.current = null;
          setImportStatus({ message: d.error || 'Import failed', type: 'error' });
          setTimeout(() => setImportStatus(null), 5000);
          resolve();
        } else if (d.status === 'running') {
          const total = d.total || 0;
          const processed = d.processed || 0;
          const isAnalyzing = d.message && d.message.includes('Analyzing');
          setImportStatus({
            message: d.message || `Processing... ${processed}/${total}`,
            type: 'info',
            processed: isAnalyzing ? undefined : (total > 0 ? processed : undefined),
            total: isAnalyzing ? undefined : (total > 0 ? total : undefined),
          });
        }
      } catch {
        clearInterval(pollRef.current!);
        resolve();
      }
    }, 2000);
  }

  function pollImport(jobId: string) {
    pollImportInternal(jobId, () => {});
  }

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="profile-section">
      <h2>Projects</h2>

      <div className="pf-import-section">
        <h3>Import from GitHub</h3>
        <p className="hint">
          Enter your GitHub username and a{' '}
          <a href="https://github.com/settings/tokens" target="_blank" rel="noopener noreferrer">personal access token</a>
          {' '}(classic). Only <code>repo</code> scope is needed to read both public and private repositories.
        </p>
        <div className="pf-import-row">
          <input
            type="text"
            placeholder="github username"
            value={ghUsername}
            onChange={(e) => setGhUsername(e.target.value)}
          />
          <input
            type="password"
            placeholder="token (required)"
            value={ghToken}
            onChange={(e) => setGhToken(e.target.value)}
          />
          <button className="btn-confirm" onClick={handleImport} disabled={importing}>
            {importing ? 'Importing...' : 'Import'}
          </button>
        </div>
        {importStatus && (
          <div className={`fetch-banner fetch-${importStatus.type}`}>
            <span>{importStatus.message}</span>
            {importStatus.processed !== undefined && importStatus.total !== undefined && importStatus.total > 0 && (
              <div className="fetch-progress">
                <div className="fetch-progress-fill" style={{ width: `${Math.round((importStatus.processed / importStatus.total) * 100)}%` }} />
              </div>
            )}
            {importStatus.processed === undefined && importStatus.total === undefined && importStatus.type === 'info' && (
              <div className="fetch-progress">
                <div className="fetch-progress-fill indeterminate" />
              </div>
            )}
          </div>
        )}
      </div>

      <div className="pf-list">
        {projects.map((p) => (
          <div key={p.id} className="pf-item pf-item-block">
            <div>
              <strong>{p.name}</strong>
              <span className="pf-type"> ({p.type})</span>
              {p.description && <p className="pf-desc">{p.description}</p>}
              {p.keywords && p.keywords.length > 0 && (
                <div className="pf-kw-list">
                  {p.keywords.map((k) => <span key={k} className="pf-kw">{k}</span>)}
                </div>
              )}
            </div>
            <div>
              <button className="pf-edit-btn" onClick={() => handleEdit(p)}>edit</button>
              <button className="pf-del-btn" onClick={() => handleDelete(p.id)}>x</button>
            </div>
          </div>
        ))}
      </div>

      <form onSubmit={handleSubmit} className="pf-add-block">
        <div className="form-field">
          <label>Name</label>
          <input
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            placeholder="Project name"
          />
        </div>
        <div className="form-field full">
          <label>Description</label>
          <textarea
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            placeholder="Project description"
            rows={4}
          />
        </div>
        <div className="form-field">
          <label>Type</label>
          <select
            value={form.type}
            onChange={(e) => setForm({ ...form, type: e.target.value })}
          >
            <option value="personal">Personal</option>
            <option value="school">School</option>
          </select>
        </div>
        <div className="pf-form-actions">
          <button type="submit" className="btn-confirm">
            {editingId ? 'Update' : 'Add Project'}
          </button>
          {editingId && <button type="button" className="btn-cancel" onClick={resetForm}>Cancel</button>}
        </div>
      </form>
    </div>
  );
}
