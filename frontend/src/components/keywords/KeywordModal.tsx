import { useState } from 'react';
import { patch, post } from '../../utils/api';

interface Props {
  keywordName: string;
  keywordId: number;
  currentStatus: string | null;
  isAdmin: boolean;
  onStatusChange: (keywordId: number, newStatus: string) => void;
  onDelete: (keywordId: number) => void;
  onClose: () => void;
}

export default function KeywordModal({ keywordName, keywordId, currentStatus, isAdmin, onStatusChange, onDelete, onClose }: Props) {
  const options = [
    { value: 'learned_in_school', label: 'Learned in my studies', dotClass: 'learned-studies' },
    { value: 'learned_personal_project', label: 'Personal project', dotClass: 'learned-project' },
    { value: 'not_learned', label: 'Not learned', dotClass: 'not-learned' },
  ];

  const [redirectName, setRedirectName] = useState('');
  const [blocking, setBlocking] = useState(false);

  async function handleSelect(status: string) {
    try {
      await patch(`/api/keywords/${keywordId}`, { learning_status: status });
      onStatusChange(keywordId, status);
    } catch {
      // silently fail
    }
    onClose();
  }

  async function handleBlock() {
    if (blocking) return;
    setBlocking(true);
    try {
      const body: { keywordId: number; redirectToName?: string } = { keywordId };
      if (redirectName.trim()) body.redirectToName = redirectName.trim();
      await post('/api/admin/block-keyword', body);
      onDelete(keywordId);
    } catch {
      // silently fail
    }
    onClose();
  }

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog-box kw-modal-box" onClick={(e) => e.stopPropagation()}>
        <h3>{keywordName}</h3>
        <p>Select learning status:</p>
        <div className="kw-status-options">
          {options.map((opt) => (
            <button
              key={opt.value}
              className={`kw-status-btn${currentStatus === opt.value ? ' selected' : ''}`}
              onClick={() => handleSelect(opt.value)}
            >
              <span className={`kw-dot ${opt.dotClass}`} />
              {opt.label}
            </button>
          ))}
        </div>

        {isAdmin && (
          <div className="kw-block-section">
            <p className="kw-block-label">Admin: Block this keyword</p>
            <input
              type="text"
              className="kw-block-input"
              placeholder="Redirect to (optional)"
              value={redirectName}
              onChange={(e) => setRedirectName(e.target.value)}
            />
            <button
              className="kw-block-btn"
              onClick={handleBlock}
              disabled={blocking}
            >
              {blocking ? '...' : 'Block'}
            </button>
          </div>
        )}

        <div className="dialog-actions">
          <button className="btn-cancel" onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  );
}
