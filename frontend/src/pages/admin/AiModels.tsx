import { useEffect, useState } from 'react';
import { get, post, put, del } from './api';

interface AiModel { id: number; name: string; ai_service_name: string; ai_service_id: number; is_active: boolean; used_by: string[]; }
interface AiService { id: number; name: string; }

export default function AdminAiModels() {
  const [models, setModels] = useState<AiModel[]>([]);
  const [services, setServices] = useState<AiService[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<number | null>(null);
  const [editName, setEditName] = useState('');
  const [newName, setNewName] = useState('');
  const [newServiceId, setNewServiceId] = useState<number | null>(null);

  async function load() {
    const [mRes, sRes] = await Promise.all([
      get<AiModel[]>('/api/admin/ai-models'),
      get<AiService[]>('/api/admin/ai-services')
    ]);
    if (mRes.success) setModels(mRes.data);
    if (sRes.success) setServices(sRes.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function addModel() {
    if (!newName.trim() || !newServiceId) return;
    const res = await post('/api/admin/ai-models', { name: newName.trim(), ai_service_id: newServiceId, is_active: true });
    if (res.success) { setNewName(''); load(); }
  }

  async function renameModel(id: number) {
    if (!editName.trim()) return;
    const m = models.find(x => x.id === id);
    if (!m) return;
    await put(`/api/admin/ai-models/${id}`, { name: editName.trim(), ai_service_id: m.ai_service_id, is_active: m.is_active });
    setEditing(null);
    load();
  }

  async function deleteModel(id: number) {
    if (!confirm('Delete this model?')) return;
    await del(`/api/admin/ai-models/${id}`);
    load();
  }

  async function toggleModel(m: AiModel) {
    await put(`/api/admin/ai-models/${m.id}`, { name: m.name, ai_service_id: m.ai_service_id, is_active: !m.is_active });
    load();
  }

  const grouped = models.reduce((acc: Record<string, AiModel[]>, m) => {
    (acc[m.ai_service_name] ??= []).push(m);
    return acc;
  }, {});

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Models</h2>
      <p className="text-muted">Manage available model strings. Add or remove models here, then assign them to operations in <b>Prompts</b>.</p>

      <div className="service-card" style={{ marginBottom: '1rem' }}>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <select className="input" style={{ maxWidth: 200 }}
            value={newServiceId ?? ''}
            onChange={e => setNewServiceId(e.target.value ? +e.target.value : null)}>
            <option value="">— service —</option>
            {services.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <input className="input" style={{ maxWidth: 250 }} placeholder="Model name (e.g. gpt-5.4)"
            value={newName} onChange={e => setNewName(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && addModel()} />
          <button className="admin-btn" onClick={addModel}>Add</button>
        </div>
      </div>

      <div className="service-grid">
        {Object.entries(grouped).map(([service, items]) => (
          <div key={service} className="service-card">
            <h3>{service}</h3>
            <div className="model-list">
              {items.map(m => (
                <div key={m.id} className="model-row">
                  {editing === m.id ? (
                    <div style={{ display: 'flex', gap: '0.5rem', flex: 1 }}>
                      <input className="input" value={editName}
                        onChange={e => setEditName(e.target.value)}
                        onKeyDown={e => { if (e.key === 'Enter') renameModel(m.id); if (e.key === 'Escape') setEditing(null); }} />
                      <button className="admin-btn" onClick={() => renameModel(m.id)}>Save</button>
                      <button className="admin-btn" style={{ borderColor: 'var(--border)', color: 'var(--text-dim)' }}
                        onClick={() => setEditing(null)}>Cancel</button>
                    </div>
                  ) : (
                    <>
                      <div className="model-info">
                        <span className="model-name">{m.name}</span>
                        {!m.is_active && <span style={{ fontSize: '0.6rem', color: 'var(--red)', marginLeft: '0.5rem' }}>inactive</span>}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                        <span className="model-usage">
                          {m.used_by.length > 0
                            ? m.used_by.map(f => <code key={f}>{f}</code>)
                            : <span className="text-dim" style={{ fontSize: '0.7rem' }}>unused</span>}
                        </span>
                        <button className={`toggle-switch small${m.is_active ? ' on' : ''}`} onClick={() => toggleModel(m)}
                          title={m.is_active ? 'Deactivate' : 'Activate'}>
                          <span className="toggle-knob" />
                        </button>
                        <button className="admin-btn" style={{ fontSize: '0.6rem', padding: '0.15rem 0.5rem' }}
                          onClick={() => { setEditing(m.id); setEditName(m.name); }}>Edit</button>
                        <button className="btn-delete" onClick={() => deleteModel(m.id)}>Delete</button>
                      </div>
                    </>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
