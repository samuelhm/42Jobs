import { useState, useEffect, useCallback, useRef } from 'react';
import { useSearchParams } from 'react-router';
import AddCategoryDialog from './AddCategoryDialog';
import { useToast } from '../context/ToastContext';

interface Category {
  id: number;
  name: string;
  job_count: number;
}

export default function CategoriesBar() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [updating, setUpdating] = useState(false);
  const [fetchStatus, setFetchStatus] = useState<{
    message: string;
    type: 'info' | 'success' | 'error';
    processed?: number;
    total?: number;
  } | null>(null);
  const { toast } = useToast();
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const statusTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const categoryId = searchParams.get('category');
  const activeId = categoryId ? Number(categoryId) : null;

  const loadCategories = useCallback(async () => {
    try {
      const res = await fetch('/api/categories');
      const json = await res.json();
      if (json.success) {
        setCategories(json.data);
        const ids = json.data.map((c: Category) => c.id);
        if (activeId === null || !ids.includes(activeId)) {
          if (ids.length > 0) {
            setSearchParams({ category: String(ids[0]) }, { replace: true });
          }
        }
      }
    } finally {
      setLoading(false);
    }
  }, [activeId, setSearchParams]);

  useEffect(() => {
    loadCategories();
  }, [loadCategories]);

  function select(id: number) {
    setSearchParams({ category: String(id) });
  }

  async function triggerFetch(categoryId: number): Promise<boolean> {
    const res = await fetch(`/api/categories/${categoryId}/fetch`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ location: 'Barcelona', limit: 10, datePosted: 'past-week', sortBy: 'recent' }),
    });
    const data = await res.json();

    if (data.status === 'fresh') {
      toast(`fresh-${categoryId}`, 'Already up to date (fetched within 4 hours)', 'success');
      return false;
    }

    const jobId = data.job_id || data.jobId;
    if (!jobId) {
      setFetchStatus({ message: 'Failed to start fetch', type: 'error' });
      clearStatusAfter(4000);
      return false;
    }

    setFetchStatus({ message: 'Searching LinkedIn...', type: 'info' });

    await new Promise<void>((resolve) => {
      pollingRef.current = setInterval(async () => {
        try {
          const r = await fetch(`/api/categories/${categoryId}/fetch/${jobId}`);
          const d = await r.json();
          if (d.status === 'completed' || d.status === 'done') {
            clearInterval(pollingRef.current!);
            pollingRef.current = null;
            setFetchStatus({ message: `${d.inserted} new offers (${d.total} found)`, type: 'success' });
            clearStatusAfter(4000);
            resolve();
          } else if (d.status === 'failed' || d.error) {
            clearInterval(pollingRef.current!);
            pollingRef.current = null;
            setFetchStatus({ message: d.error || 'Fetch failed', type: 'error' });
            clearStatusAfter(4000);
            resolve();
          } else if (d.status === 'running') {
            const total = d.total || 0;
            const processed = d.processed || 0;
            setFetchStatus({
              message: `Processing... ${processed}/${total}`,
              type: 'info',
              processed,
              total,
            });
          }
        } catch {
          clearInterval(pollingRef.current!);
          pollingRef.current = null;
          setFetchStatus({ message: 'Connection lost', type: 'error' });
          clearStatusAfter(4000);
          resolve();
        }
      }, 2000);
    });

    return true;
  }

  function clearStatusAfter(ms: number) {
    if (statusTimerRef.current) clearTimeout(statusTimerRef.current);
    statusTimerRef.current = setTimeout(() => setFetchStatus(null), ms);
  }

  async function handleCreated(id: number) {
    setShowAdd(false);
    setUpdating(true);
    try {
      const ok = await triggerFetch(id);
      if (ok) {
        await loadCategories();
        setSearchParams({ category: String(id) });
      }
    } finally {
      setUpdating(false);
    }
  }

  async function handleUpdate() {
    if (!activeId) return;
    setUpdating(true);
    try {
      const ok = await triggerFetch(activeId);
      if (ok) {
        await loadCategories();
        setSearchParams((prev) => {
          prev.set('_t', String(Date.now()));
          return prev;
        });
      }
    } catch {
      setFetchStatus({ message: 'Connection error', type: 'error' });
      clearStatusAfter(4000);
    } finally {
      setUpdating(false);
    }
  }

  if (loading) return null;

  return (
    <>
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
            </button>
          ))}
          <button className="tab-btn tab-add" onClick={() => setShowAdd(true)}>+</button>
        </div>
        <button className="update-btn" onClick={handleUpdate} disabled={updating || !activeId}>
          {updating ? '...' : 'Update'}
        </button>
        {showAdd && (
          <AddCategoryDialog
            onClose={() => setShowAdd(false)}
            onCreated={handleCreated}
          />
        )}
      </div>
      {fetchStatus && (
        <div className={`fetch-banner fetch-${fetchStatus.type}`}>
          <span>{fetchStatus.message}</span>
          {fetchStatus.processed !== undefined && fetchStatus.total !== undefined && fetchStatus.total > 0 && (
            <div className="fetch-progress">
              <div
                className="fetch-progress-fill"
                style={{ width: `${Math.round((fetchStatus.processed / fetchStatus.total) * 100)}%` }}
              />
            </div>
          )}
        </div>
      )}
    </>
  );
}
