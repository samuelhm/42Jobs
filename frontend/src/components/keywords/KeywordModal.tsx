import { patch } from '../../utils/api';

interface Props {
  keywordName: string;
  keywordId: number;
  currentStatus: string | null;
  onStatusChange: (keywordId: number, newStatus: string) => void;
  onClose: () => void;
}

export default function KeywordModal({ keywordName, keywordId, currentStatus, onStatusChange, onClose }: Props) {
  const options = [
    { value: 'learned_in_school', label: 'Learned at 42 Barcelona', dotClass: 'learned-school' },
    { value: 'learned_personal_project', label: 'Personal project', dotClass: 'learned-project' },
    { value: 'not_learned', label: 'Not learned', dotClass: 'not-learned' },
  ];

  async function handleSelect(status: string) {
    try {
      await patch(`/api/keywords/${keywordId}`, { learning_status: status });
      onStatusChange(keywordId, status);
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
        <div className="dialog-actions">
          <button className="btn-cancel" onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  );
}
