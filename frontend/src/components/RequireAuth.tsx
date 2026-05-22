import { useState, useEffect } from 'react';
import { Navigate, Outlet } from 'react-router';

export default function RequireAuth() {
  const [status, setStatus] = useState<'checking' | 'authenticated' | 'unauthenticated'>('checking');

  useEffect(() => {
    let cancelled = false;

    fetch('/api/profile')
      .then((res) => {
        if (!cancelled) {
          setStatus(res.ok ? 'authenticated' : 'unauthenticated');
        }
      })
      .catch(() => {
        if (!cancelled) setStatus('unauthenticated');
      });

    return () => { cancelled = true; };
  }, []);

  if (status === 'checking') return null;
  if (status === 'unauthenticated') return <Navigate to="/login" replace />;

  return <Outlet />;
}
