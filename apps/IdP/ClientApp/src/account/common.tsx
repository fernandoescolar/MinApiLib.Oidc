import type { ReactNode } from 'react';
import { Alert, Button, Spinner } from '../components/ui';
import type { Loaded } from './api';

/** Shows a spinner while a value loads, the error when it failed (with a retry), and the children once it is there. */
export function Loading<T>({ of, children }: { of: Loaded<T>; children: (data: T) => ReactNode }) {
  if (of.loading && of.data === null) return <div className="flex justify-center py-10"><Spinner /></div>;
  if (of.error && of.data === null) {
    return (
      <Alert kind="error">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span>{of.error}</span>
          <Button variant="secondary" size="sm" onClick={of.reload}>Retry</Button>
        </div>
      </Alert>
    );
  }
  return of.data === null ? null : <>{children(of.data)}</>;
}
