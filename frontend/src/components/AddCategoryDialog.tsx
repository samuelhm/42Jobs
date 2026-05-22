import { useState } from 'react';

interface Props {
  onClose: () => void;
  onCreated: (id: number) => void;
}

export default function AddCategoryDialog({ onClose, onCreated }: Props) {
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function handleCreate() {
    const trimmed = name.trim();
    if (!trimmed) return;

    setLoading(true);
    setError('');
    try {
      const res = await fetch('/api/categories', {
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

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog-box" onClick={(e) => e.stopPropagation()}>
        <h3>New category</h3>
        <p>Enter a keyword to search for jobs on LinkedIn:</p>
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
          <button className="btn-confirm" onClick={handleCreate} disabled={loading}>
            {loading ? 'Searching...' : 'Search'}
          </button>
        </div>
      </div>
    </div>
  );
}
