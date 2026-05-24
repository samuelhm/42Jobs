import { createContext, useContext, useState, useCallback, useRef } from 'react';
import type { Toast } from '../types';

interface ToastContextValue {
  toast: (key: string, message: string, type?: 'info' | 'success' | 'error') => void;
  toasts: Toast[];
  removeToast: (id: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const timersRef = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
    if (timersRef.current[id]) {
      clearTimeout(timersRef.current[id]);
      delete timersRef.current[id];
    }
  }, []);

  const toast = useCallback((key: string, message: string, type: 'info' | 'success' | 'error' = 'info') => {
    setToasts((prev) => {
      const existing = prev.findIndex((t) => t.id === key);
      const updated: Toast = { id: key, message, type };

      if (existing >= 0) {
        const next = [...prev];
        next[existing] = updated;
        return next;
      }

      return [...prev, updated];
    });

    if (timersRef.current[key]) clearTimeout(timersRef.current[key]);
    timersRef.current[key] = setTimeout(() => removeToast(key), 5000);
  }, [removeToast]);

  return (
    <ToastContext.Provider value={{ toast, toasts, removeToast }}>
      {children}
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}
