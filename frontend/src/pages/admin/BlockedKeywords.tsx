import { useEffect, useState } from 'react';
import { get, put, del } from '../../utils/api';

interface BlockedKeyword {
  id: number;
  name: string;
  redirect_to: number | null;
  redirect_name: string | null;
  created_at: string;
}

export default function BlockedKeywords() {
  const [items, setItems] = useState<BlockedKeyword[]>([]);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState('');
  const [newName, setNewName] = useState('');
  const [newRedirect, setNewRedirect] = useState('');

  async function load() {
    try {
      const res = await get<BlockedKeyword[]>('/api/admin/blocked-keywords');
      if (res.success) setItems(res.data);
    } catch {
      setMsg('Failed to load');
    }
    setLoading(false);
  }

  useEffect(() => { load(); }, []);

  async function updateRedirect(id: number, redirectToName: string) {
    try {
      await put(`/api/admin/blocked-keywords/${id}`, { redirect_to_name: redirectToName || null });
      setItems(prev => prev.map(i => i.id === id ? { ...i, redirect_name: redirectToName || null } : i));
      setMsg('Updated');
      setTimeout(() => setMsg(''), 3000);
    } catch {
      setMsg('Failed to update');
    }
  }

  async function remove(id: number) {
    if (!confirm('Remove this blocked keyword?')) return;
    try {
      await del(`/api/admin/blocked-keywords/${id}`);
      setItems(prev => prev.filter(i => i.id !== id));
      setMsg('Deleted');
      setTimeout(() => setMsg(''), 3000);
    } catch {
      setMsg('Failed to delete');
    }
  }

  if (loading) return null;

  return (
    <div>
      <h2>Blocked Keywords</h2>
      <p className="text-muted">Keywords that should never be inserted or should redirect to a canonical keyword automatically.</p>

      {msg && (
        <div style={{ marginBottom: '1rem', padding: '0.5rem 0.75rem', background: 'rgba(77,184,160,0.08)', borderRadius: 'var(--radius)', border: '1px solid rgba(77,184,160,0.2)', fontSize: '0.72rem', color: 'var(--teal)' }}>
          {msg}
        </div>
      )}

      <div className="service-card" style={{ marginBottom: '1rem' }}>
        <h3>Add entry</h3>
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem', flexWrap: 'wrap' }}>
          <input
            type="text"
            placeholder="Keyword name"
            value={newName}
            onChange={e => setNewName(e.target.value)}
            style={inputStyle}
          />
          <input
            type="text"
            placeholder="Redirect to (optional)"
            value={newRedirect}
            onChange={e => setNewRedirect(e.target.value)}
            style={inputStyle}
          />
          <button
            className="admin-btn"
            disabled
          >
            Add
          </button>
        </div>
        <p className="text-muted" style={{ marginTop: '0.5rem', fontSize: '0.7rem' }}>
          To block a keyword, go to the <strong>Keywords</strong> page and click any keyword tag. The modal now has a Block button for admins.
        </p>
      </div>

      {items.length === 0 ? (
        <p className="text-muted" style={{ fontStyle: 'italic' }}>No blocked keywords yet. Use the Keywords page to block keywords.</p>
      ) : (
        <div className="table-wrapper" style={{ overflowX: 'auto' }}>
          <table className="logs-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Redirects to</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td className="mono">{item.name}</td>
                  <td>
                    {item.redirect_to ? (
                      <span style={{ color: 'var(--amber)' }}>{item.redirect_name}</span>
                    ) : (
                      <span className="text-muted">blocked (no redirect)</span>
                    )}
                  </td>
                  <td className="text-muted">{new Date(item.created_at).toLocaleDateString()}</td>
                  <td>
                    <div style={{ display: 'flex', gap: '0.5rem' }}>
                      <select
                        value={item.redirect_name || ''}
                        onChange={(e) => updateRedirect(item.id, e.target.value)}
                        style={{ ...inputStyle, width: '160px' }}
                      >
                        <option value="">No redirect</option>
                        {item.redirect_name && <option value={item.redirect_name}>{item.redirect_name}</option>}
                      </select>
                      <input
                        type="text"
                        placeholder="Type keyword name"
                        style={{ ...inputStyle, width: '140px' }}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') {
                            updateRedirect(item.id, (e.target as HTMLInputElement).value);
                            (e.target as HTMLInputElement).value = '';
                          }
                        }}
                      />
                      <button className="btn-delete" onClick={() => remove(item.id)}>Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  background: 'var(--bg)',
  border: '1px solid var(--border)',
  borderRadius: 'var(--radius)',
  color: 'var(--text)',
  padding: '0.35rem 0.5rem',
  fontSize: '0.75rem',
  fontFamily: 'inherit',
};
