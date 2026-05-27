interface Props {
  message: string;
  onClose: () => void;
}

export default function AiNotConfiguredModal({ message, onClose }: Props) {
  const isKeyIssue = message.includes('no API key') || message.includes('AI Services');
  const adminUrl = isKeyIssue ? '/admin/ai-services' : '/admin/ai-prompts';
  const adminLabel = isKeyIssue ? 'Go to Admin > AI Services' : 'Go to Admin > AI Prompts';

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog-box" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
        <h3>AI not configured</h3>
        <p style={{ color: 'var(--text-dim)', margin: '1rem 0', fontSize: '0.85rem', lineHeight: 1.5 }}>{message}</p>
        <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
          <button className="btn-cancel" onClick={onClose}>Close</button>
          <a className="btn-confirm" href="/admin/ai-prompts">AI Prompts</a>
          <a className="btn-confirm" href="/admin/ai-services">AI Services</a>
        </div>
      </div>
    </div>
  );
}
