import { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router';
import AddCategoryDialog from './AddCategoryDialog';
import { fetchWithAuth } from '../../utils';
import type { Category } from '../../types';

export default function CategoriesBar({ availableOnly }: { availableOnly?: boolean }) {
  const [searchParams, setSearchParams] = useSearchParams();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [showProcessing, setShowProcessing] = useState(false);

  const categoryId = searchParams.get('category');
  const activeId = categoryId ? Number(categoryId) : null;

  const loadCategories = useCallback(async () => {
    try {
      const url = availableOnly ? '/api/categories?available=true' : '/api/categories';
      const res = await fetchWithAuth(url);
      const json = await res.json();
      if (json.success) {
        setCategories(json.data);
        const ids = json.data.map((c: Category) => c.id);
        if (activeId === null || !ids.includes(activeId)) {
          if (ids.length > 0) {
            setSearchParams({ category: String(ids[0]) }, { replace: true });
          } else {
            setSearchParams({}, { replace: true });
          }
        }
      }
    } finally {
      setLoading(false);
    }
  }, [activeId, setSearchParams, availableOnly]);

  useEffect(() => {
    loadCategories();
  }, [loadCategories]);

  function select(id: number) {
    setSearchParams({ category: String(id) });
  }

  async function handleCreated(id: number) {
    setShowAdd(false);
    await loadCategories();
    setSearchParams({ category: String(id) });
    setShowProcessing(true);
  }

  async function handleSubscribed(id: number, _name: string) {
    setShowAdd(false);
    await loadCategories();
    setSearchParams({ category: String(id) });
  }

  async function handleUnfollow(id: number, e: React.MouseEvent) {
    e.stopPropagation();
    await fetchWithAuth(`/api/categories/${id}/follow`, { method: 'DELETE' });
    await loadCategories();
  }

  if (loading) return null;

  return (
    <div className="categories-bar">
      <div className="tabs-scroll">
        {categories.map((c) => (
          <button
            key={c.id}
            className={`tab-btn${c.id === activeId ? ' active' : ''}`}
            onClick={() => select(c.id)}
          >
            {c.name}
            <span className="tab-count">{c.job_count}</span>
            <span className="tab-close" onClick={(e) => handleUnfollow(c.id, e)} title="Unfollow">x</span>
          </button>
        ))}
        <button className="tab-btn tab-add" onClick={() => setShowAdd(true)}>+</button>
      </div>
      {showAdd && (
        <AddCategoryDialog
          onClose={() => setShowAdd(false)}
          onCreated={handleCreated}
          onSubscribed={handleSubscribed}
        />
      )}
      {showProcessing && (
        <div className="dialog-overlay" onClick={() => setShowProcessing(false)}>
          <div className="dialog-box" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 400, textAlign: 'center' }}>
            <h3>Category created</h3>
            <p style={{ margin: '0.75rem 0', color: 'var(--text-dim)', fontSize: '0.85rem', lineHeight: 1.5 }}>
              Jobs are being processed with AI. They will appear here in a few minutes.
            </p>
            <button className="btn-confirm" onClick={() => setShowProcessing(false)}>Got it</button>
          </div>
        </div>
      )}
    </div>
  );
}
