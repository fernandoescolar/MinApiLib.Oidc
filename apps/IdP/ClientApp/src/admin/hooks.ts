import { createContext, useCallback, useContext, useEffect, useMemo, useState, type DependencyList } from 'react';
import { useSearchParams } from 'react-router';
import { Api, ApiError } from '../api';
import type { ConsoleModel } from '../spa';

export function useApi(model: ConsoleModel): Api {
  return useMemo(() => new Api(model.antiforgery), [model.antiforgery]);
}

export const AdminContext = createContext<{ api: Api; model: ConsoleModel } | null>(null);

export function useAdmin() {
  const context = useContext(AdminContext);
  if (!context) throw new Error('useAdmin needs an AdminContext.');
  return context;
}

/** The messages to show for a failed call. */
export function errorMessages(error: unknown): string[] {
  if (error instanceof ApiError) return error.messages;
  if (error instanceof Error) return [error.message];
  return ['Something went wrong.'];
}

export interface Loaded<T> {
  data: T | null;
  error: string[] | null;
  loading: boolean;
  reload: () => void;
  setData: (data: T) => void;
}

/** Loads something when the dependencies change; `reload` fetches again, `setData` patches the copy here. */
export function useLoad<T>(load: () => Promise<T>, deps: DependencyList): Loaded<T> {
  const [state, setState] = useState<{ data: T | null; error: string[] | null; loading: boolean }>({ data: null, error: null, loading: true });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setState(s => ({ ...s, loading: true, error: null }));
    load().then(
      data => { if (!cancelled) setState({ data, error: null, loading: false }); },
      (error: unknown) => { if (!cancelled) setState(s => ({ ...s, error: errorMessages(error), loading: false })); },
    );
    return () => { cancelled = true; };
  }, [...deps, version]);

  const reload = useCallback(() => setVersion(v => v + 1), []);
  const setData = useCallback((data: T) => setState(s => ({ ...s, data })), []);
  return { ...state, reload, setData };
}

export interface Busy {
  busy: boolean;
  error: string[] | null;
  success: string | null;
  /** Runs a call; resolves to its result, or to undefined when it failed (the error is kept here). */
  run: <R>(fn: () => Promise<R>, successMessage?: string) => Promise<R | undefined>;
  clear: () => void;
}

/** The state of a save or an action: busy flag, error messages and a success message. */
export function useBusy(): Busy {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string[] | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const run = useCallback(async <R,>(fn: () => Promise<R>, successMessage?: string): Promise<R | undefined> => {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await fn();
      if (successMessage) setSuccess(successMessage);
      return result;
    } catch (e) {
      setError(errorMessages(e));
      return undefined;
    } finally {
      setBusy(false);
    }
  }, []);

  const clear = useCallback(() => { setError(null); setSuccess(null); }, []);
  return { busy, error, success, run, clear };
}

export function useDebounced<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const handle = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(handle);
  }, [value, delay]);
  return debounced;
}

/** Filters and paging of a list, kept in the URL so they survive navigation and can be linked to. */
export function useQueryParams<K extends string>(keys: readonly K[]): [Record<K, string>, (patch: Partial<Record<K, string | null | undefined>>) => void] {
  const [searchParams, setSearchParams] = useSearchParams();
  const values = useMemo(() => {
    const result = {} as Record<K, string>;
    for (const key of keys) result[key] = searchParams.get(key) ?? '';
    return result;
  }, [searchParams, ...keys]);

  const set = useCallback((patch: Partial<Record<K, string | null | undefined>>) => {
    setSearchParams(previous => {
      const next = new URLSearchParams(previous);
      for (const [key, value] of Object.entries(patch) as [string, string | null | undefined][]) {
        if (value === null || value === undefined || value === '') next.delete(key);
        else next.set(key, value);
      }
      return next;
    }, { replace: true });
  }, [setSearchParams]);

  return [values, set];
}

export function pageNumber(value: string): number {
  const n = Number.parseInt(value, 10);
  return Number.isFinite(n) && n > 0 ? n : 1;
}
