import { useEffect, useRef, useState } from 'react';
import { Alert, AntiforgeryField, Button, Card, Code, Divider, Field, FlowLayout, Link, Muted } from '../components/ui';
import type { Antiforgery, Product } from '../spa';
import { conditionalMediationAvailable, describeFailure, getPasskey, passkeysSupported } from '../passkeys';

interface LoginModel {
  returnUrl: string | null;
  clientName: string | null;
  clientUri: string | null;
  logoUri: string | null;
  loginHint: string | null;
  requiresPasskey: boolean;
  localLogin: boolean;
  registrationEnabled: boolean;
  externalProviders: { alias: string; displayName: string; url: string }[];
  error: string | null;
  message: string | null;
  antiforgery: Antiforgery;
  product: Product;
}

/**
 * The authorize endpoint sends the user here with the authorize request in returnUrl. The user signs in with a
 * password, a passkey or an external provider; when the client asks for acr_values=phr, only with a passkey.
 */
export function LoginPage({ model }: { model: LoginModel }) {
  const registerUrl = model.returnUrl ? `/register?returnUrl=${encodeURIComponent(model.returnUrl)}` : '/register';
  return (
    <FlowLayout
      title="Sign in"
      product={model.product}
      footer={model.registrationEnabled && !model.requiresPasskey ? <>No account yet? <Link href={registerUrl}>Create one</Link></> : undefined}
    >
      {model.clientName && (
        <div className="flex items-center justify-center gap-2 text-sm text-slate-600">
          {model.logoUri && <img src={model.logoUri} alt="" className="size-6 rounded" />}
          <span>to continue to <strong className="text-slate-800">{model.clientUri ? <a href={model.clientUri} className="underline">{model.clientName}</a> : model.clientName}</strong></span>
        </div>
      )}
      {model.error && <Alert kind="error">{model.error}</Alert>}
      {model.message && <Alert kind="success">{model.message}</Alert>}

      {model.requiresPasskey ? (
        <Card>
          <p className="text-sm">This application asks for a phishing-resistant sign-in (<Code>acr_values=phr</Code>): use a passkey.</p>
          <PasskeySignIn model={model} conditional={false} />
          <Muted>No passkey yet? Sign in with your password on <Link href="/account/security">your account</Link> and add one.</Muted>
        </Card>
      ) : (
        <Card>
          {model.localLogin && (
            <form method="post" action="/login" className="space-y-4">
              <AntiforgeryField antiforgery={model.antiforgery} />
              <input type="hidden" name="returnUrl" value={model.returnUrl ?? ''} />
              {/* "username webauthn" lets the browser offer the user's passkeys as they type (conditional UI). */}
              <Field id="username" name="username" label="User name or e-mail" autoComplete="username webauthn" autoFocus defaultValue={model.loginHint ?? ''} required />
              <Field id="password" name="password" label="Password" type="password" autoComplete="current-password" required />
              <Button type="submit" className="w-full">Sign in</Button>
              <p className="text-right text-sm"><Link href="/forgot-password">Forgot your password?</Link></p>
            </form>
          )}
          {model.localLogin && <Divider>or</Divider>}
          <PasskeySignIn model={model} conditional={model.localLogin} />
          {model.externalProviders.map(provider => (
            <a key={provider.alias} href={provider.url} className="flex w-full items-center justify-center rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
              Continue with {provider.displayName}
            </a>
          ))}
        </Card>
      )}
    </FlowLayout>
  );
}

/** Signs in with a passkey: the button, and, with conditional set, the user name field's autofill. */
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
    <form ref={form} method="post" action="/login/passkey" className="space-y-2">
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
