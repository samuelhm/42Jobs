import { useEffect, useState } from 'react';
import { get } from './api';

interface AiModel { id: number; name: string; ai_service_name: string; is_active: boolean; used_by: string[]; }

export default function AdminAiModels() {
  const [models, setModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    get<AiModel[]>('/api/admin/ai-models').then(res => {
      if (res.success) setModels(res.data);
      setLoading(false);
    });
  }, []);

  const grouped = models.reduce((acc: Record<string, AiModel[]>, m) => {
    (acc[m.ai_service_name] ??= []).push(m);
    return acc;
  }, {});

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Models</h2>
      <p className="text-muted">Available models grouped by service. Assign them to operations in the <b>Prompts</b> section.</p>
      <div className="service-grid">
        {Object.entries(grouped).map(([service, items]) => (
          <div key={service} className="service-card">
            <h3>{service}</h3>
            <div className="model-list">
              {items.map(m => (
                <div key={m.id} className="model-row">
                  <div className="model-info">
                    <span className="model-name">{m.name}</span>
                  </div>
                  <span className="model-usage">
                    {m.used_by.length > 0
                      ? m.used_by.map(f => <code key={f}>{f}</code>)
                      : <span className="text-dim" style={{fontSize:'0.7rem'}}>unused</span>}
                  </span>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
