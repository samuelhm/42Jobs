import { useEffect, useState } from 'react';
import { get, put, post, del } from './api';

interface AiService {
  id: number; name: string; base_url: string; api_key: string | null;
  is_active: boolean; models: { id: number; name: string; is_default: boolean }[];
}

export default function AdminAiServices() {
  const [services, setServices] = useState<AiService[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<AiService[]>('/api/admin/ai-services');
    if (res.success) setServices(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function save(service: Partial<AiService> & { name: string; base_url: string }) {
    if ('id' in service && service.id! > 0) {
      await put(`/api/admin/ai-services/${service.id}`, service);
    } else {
      await post('/api/admin/ai-services', service);
    }
    load();
  }

  async function remove(id: number) {
    if (!confirm('Delete this service and all its models?')) return;
    await del(`/api/admin/ai-services/${id}`);
    load();
  }

  const [form, setForm] = useState<Partial<AiService>>({ name: '', base_url: '', api_key: '', is_active: true });

  if (loading) return <div className="p-4">Loading...</div>;

  return (
    <div>
      <h2>AI Services</h2>

      <div className="card mb-4" style={{ padding: '1rem' }}>
        <h3>{form.id ? 'Edit' : 'Add'} Service</h3>
        <div className="form-row">
          <input className="input" placeholder="Name" value={form.name || ''} onChange={e => setForm({ ...form, name: e.target.value })} />
          <input className="input" placeholder="Base URL" value={form.base_url || ''} onChange={e => setForm({ ...form, base_url: e.target.value })} />
          <input className="input" placeholder="API Key (optional)" value={form.api_key || ''} onChange={e => setForm({ ...form, api_key: e.target.value })} />
          <label className="checkbox-label">
            <input type="checkbox" checked={form.is_active ?? true} onChange={e => setForm({ ...form, is_active: e.target.checked })} /> Active
          </label>
          <button className="btn btn-primary" onClick={() => { save(form as any); setForm({ name: '', base_url: '', api_key: '', is_active: true }); }}>
            {form.id ? 'Update' : 'Create'}
          </button>
        </div>
      </div>

      <table className="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Base URL</th>
            <th>Models</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {services.map(s => (
            <tr key={s.id}>
              <td>{s.name}</td>
              <td style={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>{s.base_url}</td>
              <td>{s.models?.map(m => m.name + (m.is_default ? ' ★' : '')).join(', ') || '—'}</td>
              <td>{s.is_active ? '✅' : '❌'}</td>
              <td>
                <button className="btn btn-sm" onClick={() => setForm(s)}>Edit</button>
                <button className="btn btn-sm btn-danger" onClick={() => remove(s.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
