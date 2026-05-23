import { useEffect, useState } from 'react';
import { get, put, post, del } from './api';

interface Template {
  id: number; name: string; description: string | null;
  html_template: string; css: string | null; is_active: boolean;
}

export default function AdminTemplates() {
  const [templates, setTemplates] = useState<Template[]>([]);
  const [edit, setEdit] = useState<Template | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<Template[]>('/api/admin/templates');
    if (res.success) setTemplates(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function save() {
    if (!edit) return;
    if (edit.id) {
      await put(`/api/admin/templates/${edit.id}`, edit);
    } else {
      await post('/api/admin/templates', edit);
    }
    setEdit(null);
    load();
  }

  async function remove(id: number) {
    if (!confirm('Delete this template?')) return;
    await del(`/api/admin/templates/${id}`);
    load();
  }

  if (loading) return <div className="p-4">Loading...</div>;

  return (
    <div>
      <h2>CV Templates</h2>

      <button className="btn btn-primary mb-3" onClick={() => setEdit({ id: 0, name: '', description: '', html_template: '', css: '', is_active: false })}>
        New Template
      </button>

      {edit && (
        <div className="card mb-4" style={{ padding: '1rem' }}>
          <h3>{edit.id ? 'Edit' : 'New'} Template</h3>
          <input className="input" placeholder="Name" value={edit.name} onChange={e => setEdit({ ...edit, name: e.target.value })} />
          <textarea className="input mt-2" rows={2} placeholder="Description" value={edit.description || ''}
            onChange={e => setEdit({ ...edit, description: e.target.value })} />
          <label className="form-label mt-2">HTML Template</label>
          <textarea className="input" rows={10} value={edit.html_template}
            onChange={e => setEdit({ ...edit, html_template: e.target.value })} />
          <label className="form-label mt-2">CSS</label>
          <textarea className="input" rows={5} value={edit.css || ''}
            onChange={e => setEdit({ ...edit, css: e.target.value })} />
          <div className="form-row mt-2">
            <label className="checkbox-label">
              <input type="checkbox" checked={edit.is_active} onChange={e => setEdit({ ...edit, is_active: e.target.checked })} /> Active
            </label>
            <button className="btn btn-primary" onClick={save}>Save</button>
            <button className="btn" onClick={() => setEdit(null)}>Cancel</button>
          </div>
        </div>
      )}

      <table className="admin-table">
        <thead>
          <tr><th>Name</th><th>Active</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {templates.map(t => (
            <tr key={t.id}>
              <td>{t.name}</td>
              <td>{t.is_active ? '★ Active' : '—'}</td>
              <td>
                <button className="btn btn-sm" onClick={() => setEdit(t)}>Edit</button>
                <button className="btn btn-sm btn-danger" onClick={() => remove(t.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
