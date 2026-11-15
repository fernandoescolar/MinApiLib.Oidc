import { useEffect, useRef, useState } from 'react';
import { Alert, AntiforgeryField, Button, Card, Code, Field, Layout, Link, Muted, type Antiforgery } from '../components/ui';
import { conditionalMediationAvailable, describeFailure, passkeysSupported, getPasskey } from '../passkeys';

interface LoginModel {
  returnUrl: string | null;
  clientName: string | null;
  requiresPasskey: boolean;
  error: string | null;
  antiforgery: Antiforgery;
}

/**
 * The authorize endpoint sends the user here with the authorize request in returnUrl. The user signs in with a
 * password or a passkey; when the client asks for acr_values=phr, only with a passkey.
 */
export function LoginPage({ model }: { model: LoginModel }) {
  return (
    <Layout title="Sign in">
      {model.clientName && <Muted>Signing in to <strong className="text-slate-700">{model.clientName}</strong>.</Muted>}
      {model.error && <Alert kind="error">{model.error}</Alert>}

      {model.requiresPasskey ? (
        <Card>
          <p>This application asks for a phishing-resistant sign-in (<Code>acr_values=phr</Code>): use a passkey.</p>
          <PasskeySignIn model={model} conditional={false} />
          <Muted>No passkey yet? Sign in with a password on <Link href="/account/passkeys">your passkeys page</Link> and add one.</Muted>
        </Card>
      ) : (
        <>
          <Card>
            <form method="post" action="/account/login" className="space-y-4">
              <AntiforgeryField antiforgery={model.antiforgery} />
              <input type="hidden" name="returnUrl" value={model.returnUrl ?? ''} />
              {/* "username webauthn" lets the browser offer the user's passkeys as they type (conditional UI). */}
              <Field id="username" name="username" label="User name" autoComplete="username webauthn" autoFocus />
              <Field id="password" name="password" label="Password" type="password" autoComplete="current-password" />
              <Button type="submit" className="w-full">Sign in</Button>
            </form>
          </Card>
          <Card>
            <PasskeySignIn model={model} conditional />
            <Muted>
              Users: <Code>alice</Code> / <Code>alice</Code> and <Code>bob</Code> / <Code>bob</Code>. Add a passkey on{' '}
              <Link href="/account/passkeys">your passkeys page</Link>.
            </Muted>
          </Card>
        </>
      )}
    </Layout>
  );
}

/**
 * Signs in with a passkey: the button, and, with conditional set, the user name field's autofill when the browser
 * supports conditional UI. The browser's answer is posted in the form.
 */
function PasskeySignIn({ model, conditional }: { model: LoginModel; conditional: boolean }) {
  const form = useRef<HTMLFormElement>(null);
  const credential = useRef<HTMLInputElement>(null);
  const pending = useRef<AbortController | null>(null);
  const [status, setStatus] = useState<string | null>(passkeysSupported() ? null : 'This browser does not support passkeys.');

  async function signIn(mediation: CredentialMediationRequirement) {
    pending.current?.abort();
    const controller = new AbortController();
    pending.current = controller;
    try {
      credential.current!.value = await getPasskey(form.current!, mediation, controller.signal);
      form.current!.submit();
    } catch (error) {
      const message = describeFailure(error, 'The passkey sign-in was cancelled.');
      if (message) setStatus(message);
    }
  }

  useEffect(() => {
    if (!conditional) return;
    let active = true;
    conditionalMediationAvailable().then(available => {
      if (active && available) void signIn('conditional');
    });
    return () => {
      active = false;
      pending.current?.abort();
    };
    // Runs once: the conditional request waits for the user to pick a passkey in the user name field.
  }, [conditional]);

  return (
    <form ref={form} method="post" action="/account/passkey/login" className="space-y-3">
      <AntiforgeryField antiforgery={model.antiforgery} />
      <input type="hidden" name="returnUrl" value={model.returnUrl ?? ''} />
      <input ref={credential} type="hidden" name="credential" />
      <Button type="button" variant="secondary" className="w-full" disabled={!passkeysSupported()} onClick={() => void signIn('optional')}>
        Sign in with a passkey
      </Button>
      {status && <Muted>{status}</Muted>}
    </form>
  );
}
