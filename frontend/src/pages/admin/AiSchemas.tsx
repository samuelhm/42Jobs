import { useEffect, useState } from 'react';
import { get } from './api';

export default function AdminAiSchemas() {
  const [schemas, setSchemas] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<any[]>('/api/admin/ai-schemas');
    if (res.success) setSchemas(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  if (loading) return <div className="p-4">Loading...</div>;

  return (
    <div>
      <h2>AI Response Schemas</h2>
      <p className="text-muted">Schemas define the JSON structure the AI must return. Edit with caution.</p>

      {schemas.map(s => (
        <details key={s.id} className="card mb-3" style={{ padding: '1rem' }}>
          <summary style={{ cursor: 'pointer', fontWeight: 'bold' }}>{s.name}</summary>
          <p className="text-muted">{s.description}</p>
          <pre style={{ background: '#111', color: '#0f0', padding: '1rem', borderRadius: 8, overflow: 'auto', maxHeight: 400, fontSize: '0.8rem' }}>
            {JSON.stringify(s.json_schema, null, 2)}
          </pre>
        </details>
      ))}
    </div>
  );
}
