import { useEffect, useState } from 'react';
import { get, put } from './api';

interface JobProvider {
  id: number; portal: string; provider_name: string;
  is_enabled: boolean; is_active: boolean;
  base_url: string | null; api_key: string | null;
}

export default function AdminJobProviders() {
  const [providers, setProviders] = useState<JobProvider[]>([]);
  const [edit, setEdit] = useState<JobProvider | null>(null);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<JobProvider[]>('/api/admin/job-providers');
    if (res.success) setProviders(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function save() {
    if (!edit) return;
    await put(`/api/admin/job-providers/${edit.id}`, {
      is_enabled: edit.is_enabled,
      is_active: edit.is_active,
      base_url: edit.base_url,
      api_key: edit.api_key,
    });
    setEdit(null);
    load();
  }

  if (loading) return <div className="p-4">Loading...</div>;

  return (
    <div>
      <h2>Job Providers</h2>
      <p className="text-muted">Only one provider per portal can be enabled at a time. API keys set here override env vars.</p>

      {edit && (
        <div className="card mb-4" style={{ padding: '1rem' }}>
          <h3>Edit: {edit.portal} / {edit.provider_name}</h3>
          <div className="form-row">
            <input className="input" placeholder="Host (e.g. linkedin-api8.p.rapidapi.com)"
              value={edit.base_url || ''} onChange={e => setEdit({ ...edit, base_url: e.target.value })} />
            <input className="input" placeholder="API Key" type="password"
              value={edit.api_key || ''} onChange={e => setEdit({ ...edit, api_key: e.target.value })} />
          </div>
          <div className="form-row mt-2">
            <label className="checkbox-label">
              <input type="checkbox" checked={edit.is_enabled} onChange={e => setEdit({ ...edit, is_enabled: e.target.checked })} /> Enabled
            </label>
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
          <tr><th>Portal</th><th>Provider</th><th>Host</th><th>Enabled</th><th>Active</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {providers.map(p => (
            <tr key={p.id}>
              <td>{p.portal}</td>
              <td>{p.provider_name}</td>
              <td style={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>{p.base_url || '— (env)'}</td>
              <td>{p.is_enabled ? '✅' : '❌'}</td>
              <td>{p.is_active ? '✅' : '❌'}</td>
              <td><button className="btn btn-sm" onClick={() => setEdit(p)}>Edit</button></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
