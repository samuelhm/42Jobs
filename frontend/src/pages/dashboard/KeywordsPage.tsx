import { useState } from 'react';
import { useLoaderData } from 'react-router';
import { KeywordTag } from '../../components';
import { useAuth } from '../../context';
import type { KeywordsPageData } from './keywordsPage.loader';

export default function KeywordsPage() {
  const { keywords: initialKeywords } = useLoaderData() as KeywordsPageData;
  const { user } = useAuth();
  const [keywords, setKeywords] = useState(initialKeywords);
  const isAdmin = user?.role === 'Admin';

  function handleStatusChange(keywordId: number, newStatus: string) {
    setKeywords((prev) =>
      prev.map((k) => (k.id === keywordId ? { ...k, learning_status: newStatus } : k))
    );
  }

  function handleDelete(keywordId: number) {
    setKeywords((prev) => prev.filter((k) => k.id !== keywordId));
  }

  const unset = keywords.filter((k) => k.learning_status === null);
  const notLearned = keywords.filter((k) => k.learning_status === 'not_learned');
  const studies = keywords.filter((k) => k.learning_status === 'learned_in_school');
  const personal = keywords.filter((k) => k.learning_status === 'learned_personal_project');

  return (
    <div className="keywords-page">
      <h2>Keywords ({keywords.length})</h2>
      <KeywordSection title="Not specified" className="unset" items={unset} emptyMsg="All keywords assigned" isAdmin={isAdmin} onStatusChange={handleStatusChange} onDelete={handleDelete} />
      <KeywordSection title="Not learned" className="not-learned" items={notLearned} emptyMsg="None" isAdmin={isAdmin} onStatusChange={handleStatusChange} onDelete={handleDelete} displayStatus="not_learned" />
      <KeywordSection title="Learned in my studies" className="learned" items={studies} emptyMsg="None yet" isAdmin={isAdmin} onStatusChange={handleStatusChange} onDelete={handleDelete} />
      <KeywordSection title="Personal projects" className="project" items={personal} emptyMsg="None yet" isAdmin={isAdmin} onStatusChange={handleStatusChange} onDelete={handleDelete} />
    </div>
  );
}

function KeywordSection({ title, className, items, emptyMsg, isAdmin, onStatusChange, onDelete, displayStatus }: {
  title: string;
  className: string;
  items: Array<{ id: number; name: string; learning_status: string | null }>;
  emptyMsg: string;
  isAdmin: boolean;
  onStatusChange: (id: number, status: string) => void;
  onDelete: (id: number) => void;
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
            isAdmin={isAdmin}
            onStatusChange={onStatusChange}
            onDelete={onDelete}
          />
        ))}
      </div>
    </section>
  );
}
