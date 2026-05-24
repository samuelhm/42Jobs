import { useCallback, useRef, useEffect } from 'react';

export function useDebounce(delay = 800) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, []);

  const debounce = useCallback((fn: () => void) => {
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(fn, delay);
  }, [delay]);

  return debounce;
}

export function usePolling(
  pollFn: () => Promise<boolean>,
  interval = 2000
) {
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startPolling = useCallback(() => {
    stopPolling();
    pollingRef.current = setInterval(async () => {
      const done = await pollFn();
      if (done) stopPolling();
    }, interval);
  }, [pollFn, interval]);

  const stopPolling = useCallback(() => {
    if (pollingRef.current) {
      clearInterval(pollingRef.current);
      pollingRef.current = null;
    }
  }, []);

  useEffect(() => {
    return () => stopPolling();
  }, [stopPolling]);

  return { startPolling, stopPolling };
}
