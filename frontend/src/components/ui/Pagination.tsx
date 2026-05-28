interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null;

  const pages: (number | '...')[] = [];

  if (totalPages <= 7) {
    for (let i = 1; i <= totalPages; i++) pages.push(i);
  } else {
    pages.push(1);
    if (page > 3) pages.push('...');

    const start = Math.max(2, page - 1);
    const end = Math.min(totalPages - 1, page + 1);
    for (let i = start; i <= end; i++) pages.push(i);

    if (page < totalPages - 2) pages.push('...');
    pages.push(totalPages);
  }

  return (
    <div style={{ display: 'flex', justifyContent: 'center', gap: '0.3rem', marginTop: '1rem', flexWrap: 'wrap' }}>
      <button
        className="admin-btn"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
        style={{ minWidth: '2rem', padding: '0.25rem 0.5rem' }}
      >
        ←
      </button>
      {pages.map((p, i) =>
        p === '...' ? (
          <span key={`dots-${i}`} style={{ padding: '0.25rem 0.3rem', color: 'var(--text-dim)', fontSize: '0.75rem' }}>…</span>
        ) : (
          <button
            key={p}
            className="admin-btn"
            onClick={() => onPageChange(p)}
            style={{
              minWidth: '2rem',
              padding: '0.25rem 0.5rem',
              background: p === page ? 'var(--amber)' : undefined,
              borderColor: p === page ? 'var(--amber)' : undefined,
              color: p === page ? '#1a1a1a' : undefined,
              fontWeight: p === page ? 700 : undefined,
            }}
          >
            {p}
          </button>
        )
      )}
      <button
        className="admin-btn"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
        style={{ minWidth: '2rem', padding: '0.25rem 0.5rem' }}
      >
        →
      </button>
    </div>
  );
}
