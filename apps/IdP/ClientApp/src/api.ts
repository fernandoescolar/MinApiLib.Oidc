import type { Antiforgery } from './spa';

/** A failed API call: the HTTP status and the problem details the server answered with (RFC 9457). */
export class ApiError extends Error {
  status: number;
  errors: Record<string, string[]>;

  constructor(status: number, message: string, errors: Record<string, string[]> = {}) {
    super(message);
    this.status = status;
    this.errors = errors;
  }

  /** Every message, for a form that shows them together. */
  get messages(): string[] {
    const all = Object.values(this.errors).flat();
    return all.length > 0 ? all : [this.message];
  }
}

export interface Page<T> {
  items: T[];
  total: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * The JSON client of the consoles. Calls are same-origin, authenticated by the session cookie; the antiforgery token
 * of the page goes in a header on every state-changing call.
 */
export class Api {
  constructor(private readonly antiforgery: Antiforgery) {}

  get<T>(url: string): Promise<T> {
    return this.send<T>('GET', url);
  }

  post<T = void>(url: string, body?: unknown): Promise<T> {
    return this.send<T>('POST', url, body);
  }

  put<T = void>(url: string, body?: unknown): Promise<T> {
    return this.send<T>('PUT', url, body);
  }

  delete<T = void>(url: string): Promise<T> {
    return this.send<T>('DELETE', url);
  }

  private async send<T>(method: string, url: string, body?: unknown): Promise<T> {
    const headers: Record<string, string> = { Accept: 'application/json' };
    if (method !== 'GET') headers[this.antiforgery.headerName] = this.antiforgery.token;
    if (body !== undefined) headers['Content-Type'] = 'application/json';

    const response = await fetch(url, { method, headers, body: body === undefined ? undefined : JSON.stringify(body), credentials: 'same-origin' });
    if (response.status === 401) {
      // The session ended: back to the login page, and here again afterwards.
      window.location.assign(`/login?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`);
      throw new ApiError(401, 'Your session has ended.');
    }

    if (!response.ok) {
      let message = `The server answered ${response.status}.`;
      let errors: Record<string, string[]> = {};
      try {
        const problem = await response.json();
        if (problem.title) message = problem.detail ? `${problem.title} ${problem.detail}` : problem.title;
        if (problem.errors) errors = problem.errors;
      } catch {
        // not a problem details body
      }
      throw new ApiError(response.status, message, errors);
    }

    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
  }
}

/** Builds a query string from the defined values. */
export function query(params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value));
  }
  const s = search.toString();
  return s ? `?${s}` : '';
}
