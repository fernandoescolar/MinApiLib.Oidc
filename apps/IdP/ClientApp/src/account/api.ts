import { useCallback, useEffect, useMemo, useState } from 'react';
import { Api, ApiError } from '../api';
import type { ConsoleModel } from '../spa';

// The account API (/api/account): its models and the hooks that load them.

export interface PasskeyModel {
  id: string;
  name: string | null;
  createdAt: string;
  synced: boolean;
  userVerified: boolean;
}

export interface LinkedAccountModel {
  provider: string;
  displayName: string | null;
  providerKey: string;
  linkedAt: string;
}

export interface ProfileModel {
  id: string;
  userName: string;
  email: string | null;
  emailConfirmed: boolean;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  roles: string[];
  groups: string[];
  hasPassword: boolean;
  twoFactorEnabled: boolean;
  recoveryCodesLeft: number;
  passkeys: PasskeyModel[];
  logins: LinkedAccountModel[];
  createdAt: string;
  lastSignInAt: string | null;
  attributes: Record<string, string>;
}

export interface ProfileUpdate {
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  email: string | null;
  attributes: Record<string, string>;
}

export interface ProfileUpdateResult {
  profile: ProfileModel;
  emailChangePending: boolean;
  pendingEmail: string | null;
}

export interface TotpSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface RecoveryCodes {
  recoveryCodes: string[];
}

export interface ApplicationModel {
  clientId: string;
  clientName: string;
  clientUri: string | null;
  logoUri: string | null;
  scopes: string[];
  creationTime: string;
  expiration: string | null;
  description: string | null;
}

export interface ExternalProviderModel {
  alias: string;
  displayName: string;
  linked: boolean;
  providerKey: string | null;
  linkedAt: string | null;
  linkUrl: string;
}

export interface EventModel {
  id: string;
  category: string;
  name: string;
  eventType: string;
  message: string | null;
  timeStamp: string;
  remoteIpAddress: string | null;
  clientId: string | null;
}

export function useApi(model: ConsoleModel): Api {
  return useMemo(() => new Api(model.antiforgery), [model.antiforgery]);
}

export interface Loaded<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  reload: () => void;
  /** Replaces the loaded value, after a call that returned the fresh one. */
  setData: (value: T) => void;
}

/** Loads a value once (and again on `reload`), tracking the loading state and the error. */
export function useLoad<T>(load: () => Promise<T>): Loaded<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    load().then(
      value => { if (active) { setData(value); setLoading(false); } },
      failure => { if (active) { setError(errorMessage(failure)); setLoading(false); } },
    );
    return () => { active = false; };
  }, [load, attempt]);

  const reload = useCallback(() => setAttempt(n => n + 1), []);
  return { data, loading, error, reload, setData };
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.messages.join(' ');
  return error instanceof Error ? error.message : String(error);
}

/** The field errors of a failed call, keyed by camelCase field, or null when it was not a validation problem. */
export function fieldErrors(error: unknown): Record<string, string> | null {
  if (!(error instanceof ApiError) || Object.keys(error.errors).length === 0) return null;
  return Object.fromEntries(Object.entries(error.errors).map(([key, messages]) => [key.charAt(0).toLowerCase() + key.slice(1), messages.join(' ')]));
}

/** Runs an action, tracking whether it is in flight and what went wrong. */
export function useAction<Args extends unknown[]>(action: (...args: Args) => Promise<void>): { run: (...args: Args) => Promise<boolean>; busy: boolean; error: string | null; clearError: () => void } {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const run = useCallback(async (...args: Args) => {
    setBusy(true);
    setError(null);
    try {
      await action(...args);
      return true;
    } catch (failure) {
      setError(errorMessage(failure));
      return false;
    } finally {
      setBusy(false);
    }
  }, [action]);
  return { run, busy, error, clearError: () => setError(null) };
}
