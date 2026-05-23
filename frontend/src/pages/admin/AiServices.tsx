import { useEffect, useState, useCallback } from 'react';
import { get, put } from './api';

interface AiService { id: number; name: string; base_url: string; api_key: string | null; is_active: boolean; models: { id: number; name: string; is_default: boolean; is_active: boolean }[]; }

function useDebouncedSave(delay = 800) {
  const [timer, setTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  return useCallback((fn: () => void) => {
    if (timer) clearTimeout(timer);
    setTimer(setTimeout(fn, delay));
  }, [delay]);
}

function ApiKeyInput({ service }: { service: AiService }) {
  const [key, setKey] = useState(service.api_key || '');
  const [saved, setSaved] = useState(false);
  const debounce = useDebouncedSave();

  function handleChange(val: string) {
    setKey(val);
    setSaved(false);
    debounce(async () => {
      await put(`/api/admin/ai-services/${service.id}`, { name: service.name, base_url: service.base_url, api_key: val, is_active: service.is_active });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

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

  async function load() {
    const res = await get<AiService[]>('/api/admin/ai-services');
    if (res.success) setServices(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function toggleActive(s: AiService) {
    await put(`/api/admin/ai-services/${s.id}`, { name: s.name, base_url: s.base_url, api_key: s.api_key, is_active: !s.is_active });
    load();
  }

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>AI Services</h2>
      <p className="text-muted">Configure API keys and enable/disable providers. Models are managed in the <b>AI Models</b> section.</p>
      <div className="service-grid">
        {services.map(s => (
          <div key={s.id} className={`service-card ${s.is_active ? '' : 'inactive'}`}>
            <div className="service-header">
              <div>
                <h3>{s.name}</h3>
                <span className="service-count">{s.models.length} models</span>
              </div>
              <button className={`toggle-switch ${s.is_active ? 'on' : ''}`}
                onClick={() => toggleActive(s)}>
                <span className="toggle-knob" />
              </button>
            </div>
            <ApiKeyInput service={s} />
            <div className="service-models">
              {s.models.map(m => (
                <span key={m.id} className={`model-chip ${m.is_default ? 'default' : ''} ${m.is_active ? '' : 'muted'}`}>
                  {m.name}{m.is_default ? ' ★' : ''}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
