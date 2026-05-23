import { useEffect, useState, useCallback } from 'react';
import { get, put } from './api';

interface Provider { id: number; portal: string; provider_name: string; is_enabled: boolean; is_active: boolean; base_url: string | null; api_key: string | null; }

function useDebouncedSave(delay = 800) {
  const [timer, setTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  return useCallback((fn: () => void) => {
    if (timer) clearTimeout(timer);
    setTimer(setTimeout(fn, delay));
  }, [delay]);
}

function ProviderCard({ p }: { p: Provider }) {
  const [host, setHost] = useState(p.base_url || '');
  const [key, setKey] = useState(p.api_key || '');
  const [enabled, setEnabled] = useState(p.is_enabled);
  const [active, setActive] = useState(p.is_active);
  const [saved, setSaved] = useState(false);
  const debounce = useDebouncedSave();

  function doSave(h: string, k: string, e: boolean, a: boolean) {
    setSaved(false);
    debounce(async () => {
      await put(`/api/admin/job-providers/${p.id}`, { is_enabled: e, is_active: a, base_url: h || null, api_key: k || null });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

  return (
    <div className={`service-card ${enabled && active ? '' : 'inactive'}`}>
      <div className="service-header">
        <div>
          <h3>{p.portal}</h3>
          <span className="service-count">{p.provider_name}</span>
        </div>
        <div className="toggle-group">
          <button className={`toggle-switch small ${enabled ? 'on' : ''}`}
            onClick={() => { const n = !enabled; setEnabled(n); doSave(host, key, n, active); }}>
            <span className="toggle-knob" />
          </button>
        </div>
      </div>
      <div className="apikey-row">
        <input className="input" value={host}
          placeholder="Host (e.g. linkedin-api8.p.rapidapi.com)"
          onChange={e => { setHost(e.target.value); doSave(e.target.value, key, enabled, active); }} />
      </div>
      <div className="apikey-row mt-1">
        <input type="password" className="input apikey-input" value={key}
          placeholder="API key"
          onChange={e => { setKey(e.target.value); doSave(host, e.target.value, enabled, active); }} />
        {saved && <span className="apikey-saved">Saved</span>}
      </div>
    </div>
  );
}

export default function AdminJobProviders() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    get<Provider[]>('/api/admin/job-providers').then(res => {
      if (res.success) setProviders(res.data);
      setLoading(false);
    });
  }, []);

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>Job Providers</h2>
      <p className="text-muted">Configure API keys and hosts for job search providers. Toggle enabled to activate.</p>
      <div className="service-grid">
        {providers.map(p => <ProviderCard key={p.id} p={p} />)}
      </div>
    </div>
  );
}
