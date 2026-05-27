import { useState, useEffect, useCallback, useRef } from 'react';
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
  const [processingTitle, setProcessingTitle] = useState('Category created');
  const [processingMsg, setProcessingMsg] = useState('Searching for jobs and processing with AI. New offers will appear automatically here.');
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);

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
    setProcessingTitle('Category created');
    setProcessingMsg('Searching for jobs and processing with AI. New offers will appear automatically here.');
    setShowProcessing(true);

    if (pollingRef.current) clearInterval(pollingRef.current);

    let attempts = 0;
    pollingRef.current = setInterval(async () => {
      attempts++;
      try {
        const res = await fetchWithAuth('/api/categories');
        const json = await res.json();
        if (json.success) {
          const cat = json.data.find((c: Category) => c.id === id);
          if (cat?.job_count && cat.job_count > 0) {
            clearInterval(pollingRef.current!);
            pollingRef.current = null;
            setShowProcessing(false);
            setSearchParams((prev) => {
              prev.set('_r', String(parseInt(prev.get('_r') || '0') + 1));
              return prev;
            }, { replace: true });
          }
        }
      } catch { /* ignore polling errors */ }

      if (attempts >= 24) {
        clearInterval(pollingRef.current!);
        pollingRef.current = null;
      }
    }, 5000);
  }

  async function handleSubscribed(id: number, _name: string, location?: string) {
    setShowAdd(false);
    await loadCategories();
    setSearchParams({ category: String(id) });

    if (location) {
      setProcessingTitle(`Subscribed to ${_name}`);
      setProcessingMsg(`First scan for ${location} in progress. Searching and filtering with AI. New offers will appear here in a few minutes.`);
      setShowProcessing(true);

      if (pollingRef.current) clearInterval(pollingRef.current);

      let attempts = 0;
      pollingRef.current = setInterval(async () => {
        attempts++;
        try {
          const res = await fetchWithAuth('/api/categories');
          const json = await res.json();
          if (json.success) {
            const cat = json.data.find((c: Category) => c.id === id);
            if (cat?.job_count && cat.job_count > 0) {
              clearInterval(pollingRef.current!);
              pollingRef.current = null;
              setShowProcessing(false);
              setSearchParams((prev) => {
                prev.set('_r', String(parseInt(prev.get('_r') || '0') + 1));
                return prev;
              }, { replace: true });
            }
          }
        } catch { /* ignore polling errors */ }

        if (attempts >= 24) {
          clearInterval(pollingRef.current!);
          pollingRef.current = null;
          setShowProcessing(false);
        }
      }, 5000);
    }
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
        <div className="dialog-overlay" onClick={() => { setShowProcessing(false); if (pollingRef.current) { clearInterval(pollingRef.current); pollingRef.current = null; } }}>
          <div className="dialog-box" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 400, textAlign: 'center' }}>
            <h3>{processingTitle}</h3>
            <p style={{ margin: '0.75rem 0', color: 'var(--text-dim)', fontSize: '0.85rem', lineHeight: 1.5 }}>
              {processingMsg}
            </p>
            <button className="btn-confirm" onClick={() => { setShowProcessing(false); if (pollingRef.current) { clearInterval(pollingRef.current); pollingRef.current = null; } }}>Got it</button>
          </div>
        </div>
      )}
    </div>
  );
}
