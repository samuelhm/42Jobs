import { useState, useEffect } from 'react';
import { get, post, put, del } from '../../utils';

interface Field {
  key: string;
  label: string;
  type?: 'text' | 'textarea' | 'number' | 'date' | 'select';
  placeholder?: string;
  options?: { value: string; label: string }[];
}

interface Props {
  title: string;
  fields: Field[];
  fetchUrl: string;
  createUrl: string;
  updateUrl: (id: number) => string;
  deleteUrl: (id: number) => string;
  bodyBuilder: (form: Record<string, string>) => object;
  renderItem: (item: Record<string, any>) => React.ReactNode;
}

export default function ProfileList({ title, fields, fetchUrl, createUrl, updateUrl, deleteUrl, bodyBuilder, renderItem }: Props) {
  const [items, setItems] = useState<Record<string, any>[]>([]);
  const [form, setForm] = useState<Record<string, string>>({});
  const [editingId, setEditingId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  function resetForm() {
    const empty: Record<string, string> = {};
    fields.forEach((f) => { empty[f.key] = ''; });
    setForm(empty);
    setEditingId(null);
  }

  async function load() {
    const res = await get<any[]>(fetchUrl);
    if (res.success) setItems(res.data);
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const body = bodyBuilder(form);
    const res = editingId
      ? await put(updateUrl(editingId), body)
      : await post(createUrl, body);
    if (res.success) { resetForm(); load(); }
  }

  function handleEdit(item: Record<string, any>) {
    const vals: Record<string, string> = {};
    fields.forEach((f) => { vals[f.key] = String(item[f.key] ?? ''); });
    setForm(vals);
    setEditingId(item.id);
  }

  async function handleDelete(id: number) {
    if (!confirm('Delete?')) return;
    await del(deleteUrl(id));
    load();
  }

  return (
    <div className="profile-section">
      <h2>{title}</h2>

      {loading ? <div className="loading">Loading...</div> : (
        <div className="pf-list">
          {items.map((item) => (
            <div key={item.id} className="pf-item">
              <div style={{ flex: 1 }}>{renderItem(item)}</div>
              <div>
                <button className="pf-edit-btn" onClick={() => handleEdit(item)}>edit</button>
                <button className="pf-del-btn" onClick={() => handleDelete(item.id)}>x</button>
              </div>
            </div>
          ))}
        </div>
      )}

      <form onSubmit={handleSubmit} className="pf-add-row">
        {fields.map((f) => {
          if (f.type === 'select' && f.options) {
            return (
              <select key={f.key} value={form[f.key] || ''} onChange={(e) => setForm({ ...form, [f.key]: e.target.value })}>
                {f.options.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            );
          }
          if (f.type === 'textarea') {
            return <textarea key={f.key} placeholder={f.label} value={form[f.key] || ''} onChange={(e) => setForm({ ...form, [f.key]: e.target.value })} rows={2} />;
          }
          return <input key={f.key} type={f.type || 'text'} placeholder={f.label} value={form[f.key] || ''} onChange={(e) => setForm({ ...form, [f.key]: e.target.value })} />;
        })}
        <button type="submit" className="btn-confirm">
          {editingId ? '✓' : '+'}
        </button>
        {editingId && <button type="button" className="btn-cancel" onClick={resetForm}>Cancel</button>}
      </form>
    </div>
  );
}
