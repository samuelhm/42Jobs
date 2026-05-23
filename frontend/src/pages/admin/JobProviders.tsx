import { useEffect, useState, useCallback } from 'react';
import { get, put } from './api';

interface Provider { id: number; portal: string; provider_name: string; is_enabled: boolean; is_active: boolean; base_url: string | null; api_key: string | null; config: string | null; }

const FILTERS = {
  datePosted: ['', 'past-24h', 'past-week', 'past-month', 'any'],
  jobType: ['', 'full-time', 'part-time', 'contract', 'temporary', 'internship', 'volunteer', 'other'],
  experienceLevel: ['', 'internship', 'entry', 'associate', 'mid-senior', 'director', 'executive'],
  remote: ['', 'on-site', 'remote', 'hybrid'],
  industry: ['', 'technology', 'software', 'internet', 'finance', 'banking', 'healthcare', 'education', 'retail', 'manufacturing', 'consulting', 'marketing', 'media', 'telecom', 'real-estate', 'hospitality', 'automotive', 'aerospace', 'energy', 'pharma', 'legal', 'hr', 'nonprofit', 'government', 'construction', 'transportation'],
  jobFunction: ['', 'engineering', 'it', 'sales', 'marketing', 'finance', 'hr', 'operations', 'admin', 'legal', 'design', 'product', 'consulting', 'education', 'healthcare', 'research', 'support', 'management', 'business-dev', 'accounting', 'qa'],
  salary: ['', '40000+', '60000+', '80000+', '100000+', '120000+', '140000+', '160000+', '180000+', '200000+'],
  companySize: ['', 'startup', 'small', 'medium', 'large', 'enterprise', 'mega', 'giant', 'massive'],
};

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
  const [saved, setSaved] = useState(false);
  const [config, setConfig] = useState<Record<string, string>>({});

  const debounce = useDebouncedSave();

  useEffect(() => {
    try { setConfig(p.config ? JSON.parse(p.config) : {}); } catch { setConfig({}); }
  }, [p.config]);

  function doSave(h: string, k: string, e: boolean, cfg: Record<string, string>) {
    setSaved(false);
    debounce(async () => {
      const filtered: Record<string, string> = {};
      for (const [key, val] of Object.entries(cfg)) if (val) filtered[key] = val;
      await put(`/api/admin/job-providers/${p.id}`, {
        is_enabled: e, is_active: p.is_active,
        base_url: h || null, api_key: k || null, config: JSON.stringify(filtered),
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

  function setCfg(key: string, val: string) {
    const next = { ...config, [key]: val };
    setConfig(next);
    doSave(host, key, enabled, next);
  }

  return (
    <div className={`service-card ${enabled ? '' : 'inactive'}`}>
      <div className="service-header">
        <div>
          <h3>{p.portal}</h3>
          <span className="service-count">{p.provider_name}</span>
        </div>
        <button className={`toggle-switch ${enabled ? 'on' : ''}`}
          onClick={() => { const n = !enabled; setEnabled(n); doSave(host, key, n, config); }}>
          <span className="toggle-knob" />
        </button>
      </div>
      <div className="apikey-row">
        <input className="input" value={host}
          placeholder="Host (e.g. linkedin-api8.p.rapidapi.com)"
          onChange={e => { setHost(e.target.value); doSave(e.target.value, key, enabled, config); }} />
      </div>
      <div className="apikey-row mt-1">
        <input type="password" className="input apikey-input" value={key}
          placeholder="API key"
          onChange={e => { setKey(e.target.value); doSave(host, e.target.value, enabled, config); }} />
        {saved && <span className="apikey-saved">Saved</span>}
      </div>
      <details className="provider-config">
        <summary>Search defaults</summary>
        <p className="text-muted" style={{fontSize: '0.7rem', marginBottom: '0.5rem'}}>Tip: leave salary, experience level, and industry empty — many job offers don't include this data and filtering by them may hide valid results. Location and date are configured per-user in Profile.</p>
        <div className="config-grid">
          {Object.entries(FILTERS).map(([name, options]) => (
        <div className="config-grid">
          <label className="form-field" style={{marginBottom: '0.4rem'}}>
            <span className="form-label">location</span>
            <input className="input" value={config['location'] || ''}
              placeholder="e.g. Barcelona"
              onChange={e => setCfg('location', e.target.value)} />
          </label>
          {Object.entries(FILTERS).map(([name, options]) => (
            <label key={name} className="form-field" style={{marginBottom: '0.4rem'}}>
              <span className="form-label">{name}</span>
              <select className="input" value={config[name] || ''} onChange={e => setCfg(name, e.target.value)}>
                {options.map(o => <option key={o} value={o}>{o || '—'}</option>)}
              </select>
            </label>
          ))}
        </div>
      </details>
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
      <p className="text-muted">Configure API keys, hosts, and search defaults. Expand "Search defaults" to set preferred filters for job searches.</p>
      <div className="service-grid">
        {providers.map(p => <ProviderCard key={p.id} p={p} />)}
      </div>
    </div>
  );
}
