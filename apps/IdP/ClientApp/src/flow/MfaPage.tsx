import { useState } from 'react';
import { Alert, AntiforgeryField, Button, Card, Field, FlowLayout, Muted } from '../components/ui';
import type { Antiforgery, Product } from '../spa';

interface MfaModel {
  userName: string;
  error: string | null;
  antiforgery: Antiforgery;
  product: Product;
}

/** The second step of the sign-in: a code from the authenticator app, or a recovery code. */
export function MfaPage({ model }: { model: MfaModel }) {
  const [recovery, setRecovery] = useState(false);
  return (
    <FlowLayout title="Second factor" product={model.product} footer={<a href="/login" className="text-indigo-600">Back to sign in</a>}>
      {model.error && <Alert kind="error">{model.error}</Alert>}
      <Card>
        <Muted>Signing in as <strong className="text-slate-700">{model.userName}</strong>.</Muted>
        <form method="post" action="/login/mfa" className="space-y-4">
          <AntiforgeryField antiforgery={model.antiforgery} />
          <input type="hidden" name="recovery" value={recovery ? 'true' : 'false'} />
          {recovery ? (
            <Field id="code" name="code" label="Recovery code" autoComplete="off" autoFocus required hint="One of the codes you saved when you set up the authenticator. Each works once." />
          ) : (
            <Field id="code" name="code" label="Authenticator code" inputMode="numeric" pattern="[0-9 ]*" autoComplete="one-time-code" autoFocus required hint="The six digits your authenticator app shows." />
          )}
          <Button type="submit" className="w-full">Continue</Button>
        </form>
        <button type="button" className="text-sm text-indigo-600 hover:text-indigo-500" onClick={() => setRecovery(!recovery)}>
          {recovery ? 'Use the authenticator app instead' : 'Use a recovery code instead'}
        </button>
      </Card>
    </FlowLayout>
  );
}
