import { useState, useEffect } from 'react';
import { fetchWithAuth } from '../../utils';

interface Props {
  onClose: () => void;
  onCreated: (id: number) => void;
  onSubscribed: (id: number, name: string) => void;
}

interface AvailableCategory {
  id: number;
  name: string;
  last_fetched_at: string | null;
  job_count: number;
}

export default function AddCategoryDialog({ onClose, onCreated, onSubscribed }: Props) {
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [available, setAvailable] = useState<AvailableCategory[]>([]);

  useEffect(() => {
    fetchWithAuth('/api/categories/available')
      .then(r => r.json())
      .then(d => { if (d.success) setAvailable(d.data); })
      .catch(() => {});
  }, []);

  async function handleCreate() {
    const trimmed = name.trim();
    if (!trimmed) return;

    setLoading(true);
    setError('');
    try {
      const res = await fetchWithAuth('/api/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: trimmed }),
      });
      const data = await res.json();
      if (res.ok) {
        onCreated(data.id);
      } else {
        setError(data.error || 'Could not create category');
      }
    } catch {
      setError('Connection error');
    } finally {
      setLoading(false);
    }
  }

  async function handleSubscribe(id: number, categoryName: string) {
    setLoading(true);
    try {
      const res = await fetchWithAuth('/api/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: categoryName }),
      });
      const data = await res.json();
      if (res.ok) {
        onSubscribed(id, categoryName);
      } else {
        setError(data.error || 'Could not subscribe');
      }
    } catch {
      setError('Connection error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog-box" onClick={(e) => e.stopPropagation()}>
        <h3>Add Category</h3>

        {available.length > 0 && (
          <>
            <p className="hint-text">Available categories:</p>
            <div className="available-list">
              {available.map(c => (
                <button key={c.id} className="available-item" onClick={() => handleSubscribe(c.id, c.name)} disabled={loading}>
                  <span className="av-name">{c.name}</span>
                  <span className="av-meta">{c.job_count} jobs · {c.last_fetched_at ? new Date(c.last_fetched_at).toLocaleDateString() : 'never'}</span>
                </button>
              ))}
            </div>
            <p className="hint-text" style={{ marginTop: '1rem' }}>Or create a new one:</p>
          </>
        )}

        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') handleCreate(); if (e.key === 'Escape') onClose(); }}
          placeholder="e.g. Embedded, Backend, Full Stack..."
          autoFocus
        />
        {error && <div id="add-status">{error}</div>}
        <div className="dialog-actions">
          <button className="btn-cancel" onClick={onClose} disabled={loading}>Cancel</button>
          <button className="btn-confirm" onClick={handleCreate} disabled={loading || !name.trim()}>
            {loading ? '...' : 'Create'}
          </button>
        </div>
      </div>
    </div>
  );
}
