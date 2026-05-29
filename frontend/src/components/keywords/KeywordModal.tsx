import { useEffect, useRef, useState } from 'react';
import { get, patch, post } from '../../utils/api';

interface Props {
  keywordName: string;
  keywordId: number;
  currentStatus: string | null;
  isAdmin: boolean;
  onStatusChange: (keywordId: number, newStatus: string) => void;
  onDelete: (keywordId: number) => void;
  onClose: () => void;
}

let cachedKeywordNames: string[] | null = null;

export default function KeywordModal({ keywordName, keywordId, currentStatus, isAdmin, onStatusChange, onDelete, onClose }: Props) {
  const options = [
    { value: 'learned_in_school', label: 'Learned in my studies', dotClass: 'learned-studies' },
    { value: 'learned_personal_project', label: 'Personal project', dotClass: 'learned-project' },
    { value: 'not_learned', label: 'Not learned', dotClass: 'not-learned' },
  ];

  const [redirectName, setRedirectName] = useState('');
  const [blocking, setBlocking] = useState(false);
  const [allNames, setAllNames] = useState<string[]>(cachedKeywordNames || []);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (allNames.length > 0) return;
    get<{ name: string }[]>('/api/admin/keywords-names').then(res => {
      if (res.success) {
        const names = res.data.map(k => k.name);
        cachedKeywordNames = names;
        setAllNames(names);
      }
    }).catch(() => {});
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (suggestionsRef.current && !suggestionsRef.current.contains(e.target as Node) &&
          inputRef.current && !inputRef.current.contains(e.target as Node)) {
        setShowSuggestions(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const filtered = redirectName.trim()
    ? allNames.filter(n => n.toLowerCase().includes(redirectName.trim().toLowerCase()) && n !== keywordName).slice(0, 6)
    : [];

  const exactMatch = allNames.find(n => n.toLowerCase() === redirectName.trim().toLowerCase());

  async function handleSelect(status: string) {
    try {
      await patch(`/api/keywords/${keywordId}`, { learning_status: status });
      onStatusChange(keywordId, status);
    } catch { }
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
    } catch { }
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
            <div style={{ position: 'relative' }}>
              <input
                ref={inputRef}
                type="text"
                className="kw-block-input"
                style={{ width: '100%', boxSizing: 'border-box' }}
                placeholder="Redirect to (optional)"
                value={redirectName}
                onChange={(e) => { setRedirectName(e.target.value); setShowSuggestions(true); }}
                onFocus={() => setShowSuggestions(true)}
              />
              {showSuggestions && filtered.length > 0 && !exactMatch && (
                <div ref={suggestionsRef} className="kw-suggestions">
                  {filtered.map(name => (
                    <button
                      key={name}
                      className="kw-suggestion-item"
                      onMouseDown={() => { setRedirectName(name); setShowSuggestions(false); }}
                    >
                      {name}
                    </button>
                  ))}
                </div>
              )}
            </div>
            <button
              className={exactMatch ? 'kw-redirect-btn' : 'kw-block-btn'}
              onClick={handleBlock}
              disabled={blocking}
            >
              {blocking ? '...' : exactMatch ? `Redirect to ${exactMatch}` : 'Block'}
            </button>
            {exactMatch && (
              <p className="kw-block-hint" style={{ fontSize: '0.62rem', color: 'var(--amber-dim)', margin: 0 }}>
                All associations (jobs, projects, users) will be migrated to "{exactMatch}", then this keyword will be deleted and blocked forever.
              </p>
            )}
          </div>
        )}

        <div className="dialog-actions">
          <button className="btn-cancel" onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  );
}
