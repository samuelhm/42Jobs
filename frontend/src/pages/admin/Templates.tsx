import { useEffect, useState } from 'react';
import { get, put, del } from '../../utils';

interface Template { id: number; name: string; description: string | null; html_template: string; css: string | null; is_active: boolean; }

function TemplateCard({ t, onToggleActive }: { t: Template; onToggleActive: (id: number, active: boolean) => void }) {
  const [name, setName] = useState(t.name);
  const [html, setHtml] = useState(t.html_template);
  const [css, setCss] = useState(t.css || '');
  const [active, setActive] = useState(t.is_active);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  async function handleSave() {
    setSaving(true);
    setSaved(false);
    await put(`/api/admin/templates/${t.id}`, { name, description: t.description, html_template: html, css: css || null, is_active: active });
    setSaving(false);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  function handleToggle() {
    const next = !active;
    setActive(next);
    onToggleActive(t.id, next);
  }

  return (
    <div className={`service-card ${active ? '' : 'inactive'}`}>
      <div className="service-header">
        <div>
          <input className="input name-input" value={name}
            placeholder="Template name"
            onChange={e => setName(e.target.value)} />
        </div>
        <button className={`toggle-switch ${active ? 'on' : ''}`} onClick={handleToggle}>
          <span className="toggle-knob" />
        </button>
      </div>
      <label className="form-label">HTML Template</label>
      <textarea className="input" rows={10} value={html}
        onChange={e => setHtml(e.target.value)} />
      <label className="form-label mt-2">CSS</label>
      <textarea className="input" rows={4} value={css}
        onChange={e => setCss(e.target.value)} />
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.75rem' }}>
        <button className="admin-btn" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </button>
        {saved && <span className="apikey-saved">Saved</span>}
      </div>
    </div>
  );
}

export default function AdminTemplates() {
  const [templates, setTemplates] = useState<Template[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await get<Template[]>('/api/admin/templates');
    if (res.success) setTemplates(res.data);
    setLoading(false);
  }
  useEffect(() => { load(); }, []);

  async function remove(id: number) {
    if (!confirm('Delete this template?')) return;
    await del(`/api/admin/templates/${id}`);
    load();
  }

  function handleToggleActive(id: number, active: boolean) {
    setTemplates(prev => prev.map(t => {
      if (t.id === id) return { ...t, is_active: active };
      if (active) return { ...t, is_active: false };
      return t;
    }));
  }

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>CV Templates</h2>
      <p className="text-muted">Only one template is active at a time. Toggle to switch. Changes require manual save — click <b>Save</b> to persist.</p>
      <div className="service-grid">
        {templates.map(t => (
          <div key={t.id}>
            <TemplateCard t={t} onToggleActive={handleToggleActive} />
            <button className="btn-delete mt-1" onClick={() => remove(t.id)}>Delete</button>
          </div>
        ))}
      </div>
    </div>
  );
}
