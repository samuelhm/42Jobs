import { useEffect, useState } from 'react';
import { get, put, post, del } from './api';

interface AiModel {
  id: number; name: string; ai_service_id: number; ai_service_name: string;
  is_active: boolean; is_default: boolean;
}

interface AiService { id: number; name: string; }

export default function AdminAiModels() {
  const [models, setModels] = useState<AiModel[]>([]);
  const [services, setServices] = useState<AiService[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState({ name: '', ai_service_id: 0, is_active: true, is_default: false, id: 0 });

  async function load() {
    const [mRes, sRes] = await Promise.all([
      get<AiModel[]>('/api/admin/ai-models'),
      get<AiService[]>('/api/admin/ai-services'),
    ]);
    if (mRes.success) setModels(mRes.data);
    if (sRes.success) setServices(sRes.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function save() {
    if (form.id) {
      await put(`/api/admin/ai-models/${form.id}`, form);
    } else {
      await post('/api/admin/ai-models', form);
    }
    setForm({ name: '', ai_service_id: 0, is_active: true, is_default: false, id: 0 });
    load();
  }

  async function remove(id: number) {
    if (!confirm('Delete this model?')) return;
    await del(`/api/admin/ai-models/${id}`);
    load();
  }

  if (loading) return <div className="p-4">Loading...</div>;

  return (
    <div>
      <h2>AI Models</h2>

      <div className="card mb-4" style={{ padding: '1rem' }}>
        <h3>{form.id ? 'Edit' : 'Add'} Model</h3>
        <div className="form-row">
          <select className="input" value={form.ai_service_id} onChange={e => setForm({ ...form, ai_service_id: +e.target.value })}>
            <option value={0}>Select service...</option>
            {services.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <input className="input" placeholder="Model name" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
          <label className="checkbox-label">
            <input type="checkbox" checked={form.is_active} onChange={e => setForm({ ...form, is_active: e.target.checked })} /> Active
          </label>
          <label className="checkbox-label">
            <input type="checkbox" checked={form.is_default} onChange={e => setForm({ ...form, is_default: e.target.checked })} /> Default
          </label>
          <button className="btn btn-primary" onClick={save}>{form.id ? 'Update' : 'Create'}</button>
        </div>
      </div>

      <table className="admin-table">
        <thead>
          <tr><th>Service</th><th>Model</th><th>Active</th><th>Default</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {models.map(m => (
            <tr key={m.id}>
              <td>{m.ai_service_name}</td>
              <td>{m.name}</td>
              <td>{m.is_active ? '✅' : '❌'}</td>
              <td>{m.is_default ? '★' : '—'}</td>
              <td>
                <button className="btn btn-sm" onClick={() => setForm(m)}>Edit</button>
                <button className="btn btn-sm btn-danger" onClick={() => remove(m.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
