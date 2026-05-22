import { useState } from 'react';
import KeywordModal from './KeywordModal';

interface Props {
  name: string;
  id: number;
  status: string;
  onStatusChange: (keywordId: number, newStatus: string) => void;
}

export default function KeywordTag({ name, id, status, onStatusChange }: Props) {
  const [showModal, setShowModal] = useState(false);

  return (
    <>
      <span
        className={`kw-tag ${status}`}
        onClick={(e) => { e.stopPropagation(); setShowModal(true); }}
      >
        {name}
      </span>
      {showModal && (
        <KeywordModal
          keywordName={name}
          keywordId={id}
          currentStatus={status}
          onStatusChange={(kwId, newStatus) => {
            onStatusChange(kwId, newStatus);
            setShowModal(false);
          }}
          onClose={() => setShowModal(false)}
        />
      )}
    </>
  );
}
