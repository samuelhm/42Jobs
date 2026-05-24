import { useEffect, useState } from 'react';
import { get, put } from '../../utils';
import { useDebounce } from '../../hooks';

interface AiService { id: number; name: string; api_key: string | null; is_active: boolean; is_free_tier: boolean; models: { id: number; name: string; is_active: boolean }[]; }

function ApiKeyInput({ service }: { service: AiService }) {
  const [key, setKey] = useState('');
  const [loaded, setLoaded] = useState(false);
  const [saved, setSaved] = useState(false);
  const debounce = useDebounce();

  useEffect(() => {
    get<any[]>('/api/admin/ai-services').then((r: any) => {
      if (r.success) {
        const s = r.data.find((x: any) => x.id === service.id);
        if (s) setKey(s.api_key || '');
      }
      setLoaded(true);
    });
  }, [service.id]);

  function handleChange(val: string) {
    setKey(val);
    setSaved(false);
    debounce(async () => {
      await put(`/api/admin/ai-services/${service.id}`, { name: service.name, base_url: '', api_key: val, is_active: service.is_active, is_free_tier: service.is_free_tier });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

  if (!loaded) return null;

  return (
    <div className="apikey-row">
      <input type="password" className="input apikey-input" value={key}
        placeholder="API key not set — configure here"
        onChange={e => handleChange(e.target.value)} />
      {saved && <span className="apikey-saved">Saved</span>}
    </div>
  );
}

export default function AdminAiServices() {
  const [services, setServices] = useState<AiService[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    get<AiService[]>('/api/admin/ai-services').then((res: any) => {
      if (res.success) setServices(res.data);
      setLoading(false);
    });
  }, []);

  async function toggleFreeTier(s: AiService) {
    await put(`/api/admin/ai-services/${s.id}`, { name: s.name, base_url: '', api_key: s.api_key, is_active: s.is_active, is_free_tier: !s.is_free_tier });
    setServices(prev => prev.map(x => x.id === s.id ? { ...x, is_free_tier: !x.is_free_tier } : x));
  }

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Services</h2>
      <p className="text-muted">Configure API keys. Mark as <b>Free Tier</b> if the key has strict rate limits — the system will add delays between calls to avoid 429 errors.</p>
      <div className="service-grid">
        {services.map(s => (
          <div key={s.id} className="service-card">
            <div className="service-header">
              <div>
                <h3>{s.name}</h3>
                <span className="service-count">{s.models.length} models</span>
              </div>
            </div>
            <ApiKeyInput service={s} />
            {s.name !== 'DeepSeek' && (
            <div className="free-tier-row">
              <label className="checkbox-label" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <span>Free tier</span>
                <button className={`toggle-switch ${s.is_free_tier ? 'on' : ''}`}
                  onClick={() => toggleFreeTier(s)}>
                  <span className="toggle-knob" />
                </button>
              </label>
              <span className="text-dim" style={{ fontSize: '0.7rem' }}>
                {s.is_free_tier ? 'Delays + retries active' : 'No rate limiting'}
              </span>
            </div>
            )}
            <div className="service-models">
              {s.models.map(m => (
                <span key={m.id} className={`model-chip ${!m.is_active ? 'muted' : ''}`}>
                  {m.name}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
