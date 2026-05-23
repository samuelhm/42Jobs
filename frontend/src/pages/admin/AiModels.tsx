import { useEffect, useState } from 'react';
import { get, put } from './api';

interface AiModel { id: number; name: string; ai_service_id: number; ai_service_name: string; is_active: boolean; is_default: boolean; }

export default function AdminAiModels() {
  const [models, setModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<AiModel[]>('/api/admin/ai-models');
    if (res.success) setModels(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function setDefault(id: number) {
    await put(`/api/admin/ai-models/${id}`, { ...models.find(m => m.id === id)!, is_default: true });
    load();
  }

  async function toggleActive(m: AiModel) {
    await put(`/api/admin/ai-models/${m.id}`, { ...m, is_active: !m.is_active, is_default: m.is_default });
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
      <p className="text-muted">Click a model to set it as default. The default model is used for all AI operations.</p>
      <div className="service-grid">
        {Object.entries(grouped).map(([service, items]) => (
          <div key={service} className="service-card">
            <h3>{service}</h3>
            <div className="model-list">
              {items.map(m => (
                <div key={m.id} className={`model-row ${m.is_default ? 'default' : ''}`}
                  onClick={() => !m.is_default && setDefault(m.id)}>
                  <div className="model-info">
                    <span className="model-name">{m.name}</span>
                    {m.is_default && <span className="model-default-badge">DEFAULT</span>}
                  </div>
                  <button className={`toggle-switch small ${m.is_active ? 'on' : ''}`}
                    onClick={e => { e.stopPropagation(); toggleActive(m); }}>
                    <span className="toggle-knob" />
                  </button>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
