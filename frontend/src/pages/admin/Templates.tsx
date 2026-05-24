import { useEffect, useState } from 'react';
import { get, put, del } from '../../utils';
import { useDebounce } from '../../hooks';

interface Template { id: number; name: string; description: string | null; html_template: string; css: string | null; is_active: boolean; }

function TemplateCard({ t }: { t: Template }) {
  const [name, setName] = useState(t.name);
  const [html, setHtml] = useState(t.html_template);
  const [css, setCss] = useState(t.css || '');
  const [active, setActive] = useState(t.is_active);
  const [saved, setSaved] = useState(false);
  const debounce = useDebounce(1200);

  function doSave(n: string, h: string, c: string, a: boolean) {
    setSaved(false);
    debounce(async () => {
      await put(`/api/admin/templates/${t.id}`, { name: n, description: t.description, html_template: h, css: c || null, is_active: a });
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    });
  }

  return (
    <div className={`service-card ${active ? '' : 'inactive'}`}>
      <div className="service-header">
        <div>
          <input className="input name-input" value={name}
            placeholder="Template name"
            onChange={e => { setName(e.target.value); doSave(e.target.value, html, css, active); }} />
        </div>
        <button className={`toggle-switch ${active ? 'on' : ''}`}
          onClick={() => { const n = !active; setActive(n); doSave(name, html, css, n); }}>
          <span className="toggle-knob" />
        </button>
      </div>
      <label className="form-label">HTML Template</label>
      <textarea className="input" rows={10} value={html}
        onChange={e => { setHtml(e.target.value); doSave(name, e.target.value, css, active); }} />
      <label className="form-label mt-2">CSS</label>
      <textarea className="input" rows={4} value={css}
        onChange={e => { setCss(e.target.value); doSave(name, html, e.target.value, active); }} />
      {saved && <span className="apikey-saved">Saved</span>}
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

  if (loading) return <div className="p-4 text-muted">Loading...</div>;

  return (
    <div>
      <h2>CV Templates</h2>
      <p className="text-muted">Only one template is active at a time. Toggle to switch between them.</p>
      <div className="service-grid">
        {templates.map(t => (
          <div key={t.id}>
            <TemplateCard t={t} />
            <button className="btn-delete mt-1" onClick={() => remove(t.id)}>Delete</button>
          </div>
        ))}
      </div>
    </div>
  );
}
