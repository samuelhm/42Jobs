import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { fetchWithAuth } from '../utils/fetchWithAuth';
import type { User } from '../types';

interface AuthState {
  user: User | null;
  loading: boolean;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ user: null, loading: true });

  useEffect(() => {
    fetchWithAuth('/api/users/me')
      .then(res => res.ok ? res.json() : null)
      .then(data => {
        if (data?.success) {
          setState({ user: data.data, loading: false });
        } else {
          setState({ user: null, loading: false });
        }
      })
      .catch(() => setState({ user: null, loading: false }));
  }, []);

  return <AuthContext.Provider value={state}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
