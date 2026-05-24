import { useState } from 'react';
import { useLoaderData } from 'react-router';
import { KeywordTag } from '../../components';
import type { KeywordsPageData } from './keywordsPage.loader';

export default function KeywordsPage() {
  const { keywords: initialKeywords } = useLoaderData() as KeywordsPageData;
  const [keywords, setKeywords] = useState(initialKeywords);

  function handleStatusChange(keywordId: number, newStatus: string) {
    setKeywords((prev) =>
      prev.map((k) => (k.id === keywordId ? { ...k, learning_status: newStatus } : k))
    );
  }

  const unset = keywords.filter((k) => k.learning_status === null);
  const notLearned = keywords.filter((k) => k.learning_status === 'not_learned');
  const school = keywords.filter((k) => k.learning_status === 'learned_in_school');
  const personal = keywords.filter((k) => k.learning_status === 'learned_personal_project');

  return (
    <div className="keywords-page">
      <h2>Keywords ({keywords.length})</h2>
      <KeywordSection title="Not specified" className="unset" items={unset} emptyMsg="All keywords assigned" onStatusChange={handleStatusChange} />
      <KeywordSection title="Not learned" className="not-learned" items={notLearned} emptyMsg="None" onStatusChange={handleStatusChange} displayStatus="not_learned" />
      <KeywordSection title="Learned at 42 Barcelona" className="learned" items={school} emptyMsg="None yet" onStatusChange={handleStatusChange} />
      <KeywordSection title="Personal projects" className="project" items={personal} emptyMsg="None yet" onStatusChange={handleStatusChange} />
    </div>
  );
}

function KeywordSection({ title, className, items, emptyMsg, onStatusChange, displayStatus }: {
  title: string;
  className: string;
  items: Array<{ id: number; name: string; learning_status: string | null }>;
  emptyMsg: string;
  onStatusChange: (id: number, status: string) => void;
  displayStatus?: string;
}) {
  return (
    <section>
      <h3 className={`kw-section-title ${className}`}>{title} ({items.length})</h3>
      <div className="kw-section-list">
        {items.length === 0 && <p className="text-dim">{emptyMsg}</p>}
        {items.map((k) => (
          <KeywordTag
            key={k.id}
            name={k.name}
            id={k.id}
            status={displayStatus ?? k.learning_status}
            onStatusChange={onStatusChange}
          />
        ))}
      </div>
    </section>
  );
}
