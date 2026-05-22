import { useState, useEffect } from 'react';

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
  const [importing, setImporting] = useState(false);
  const [importMsg, setImportMsg] = useState('');

  async function load() {
    const res = await fetch('/api/projects');
    const data = await res.json();
    if (data.success) setProjects(data.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

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
    if (!username) return;
    setImporting(true);
    setImportMsg('Importing from GitHub...');
    try {
      const res = await fetch('/api/projects/import-github', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username }),
      });
      const data = await res.json();
      if (data.success) {
        setImportMsg(`${data.data.inserted} projects imported`);
        load();
      } else {
        setImportMsg(data.error || 'Import failed');
      }
    } catch {
      setImportMsg('Connection error');
    } finally {
      setImporting(false);
      setTimeout(() => setImportMsg(''), 5000);
    }
  }

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="profile-section">
      <h2>Projects</h2>

      <div className="pf-import-section">
        <h3>Import from GitHub</h3>
        <p className="hint">Enter your GitHub username to import public repositories. Make sure each repo has a detailed README for best results.</p>
        <div className="pf-import-row">
          <input
            type="text"
            placeholder="github username"
            value={ghUsername}
            onChange={(e) => setGhUsername(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') handleImport(); }}
          />
          <button className="btn-confirm" onClick={handleImport} disabled={importing}>
            {importing ? 'Importing...' : 'Import'}
          </button>
        </div>
        {importMsg && <p className="pf-import-msg">{importMsg}</p>}
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
