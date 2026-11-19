import { Alert, AntiforgeryField, Button, Card, Code, Field, FlowLayout, Link, Muted } from '../components/ui';
import type { Antiforgery, Product } from '../spa';

interface LogoutModel {
  prompt: boolean;
  logoutId: string | null;
  postLogoutRedirectUri: string | null;
  clientName: string | null;
  signOutIFrameUrl: string | null;
  antiforgery: Antiforgery | null;
  product: Product;
}

/** Asks to confirm the sign-out, or shows that it happened and runs the front-channel logout iframe. */
export function LogoutPage({ model }: { model: LogoutModel }) {
  if (model.prompt) {
    return (
      <FlowLayout title="Sign out" product={model.product}>
        <Card>
          <p className="text-sm">Do you want to sign out of {model.product.name}?</p>
          <form method="post" action="/logout" className="flex gap-3">
            {model.antiforgery && <AntiforgeryField antiforgery={model.antiforgery} />}
            <input type="hidden" name="logoutId" value={model.logoutId ?? ''} />
            <Button type="submit" className="flex-1">Yes, sign out</Button>
            <a href="/account" className="flex flex-1 items-center justify-center rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">No</a>
          </form>
        </Card>
      </FlowLayout>
    );
  }

  return (
    <FlowLayout title="Signed out" product={model.product}>
      <Card>
        <p className="text-sm">You are signed out.</p>
        {model.postLogoutRedirectUri
          ? <p className="text-sm"><Link href={model.postLogoutRedirectUri}>Return to {model.clientName ?? 'the application'}</Link></p>
          : <p className="text-sm"><Link href="/login">Sign in again</Link></p>}
      </Card>
      {model.signOutIFrameUrl && <iframe src={model.signOutIFrameUrl} title="Front-channel logout" className="hidden" />}
    </FlowLayout>
  );
}

interface ErrorModel {
  error: string | null;
  errorDescription: string | null;
  requestId: string | null;
  product: Product;
}

export function ErrorPage({ model }: { model: ErrorModel }) {
  return (
    <FlowLayout title="Something went wrong" product={model.product}>
      <Card>
        <Alert kind="error">{model.error ?? 'The request could not be completed.'}</Alert>
        {model.errorDescription && <p className="text-sm">{model.errorDescription}</p>}
        {model.requestId && <Muted>Request id: <Code>{model.requestId}</Code></Muted>}
        <p className="text-sm"><Link href="/account">Go to my account</Link></p>
      </Card>
    </FlowLayout>
  );
}

interface MessageModel {
  title: string;
  text: string;
  linkHref: string | null;
  linkText: string | null;
  error: boolean;
  product: Product;
}

/** A page with a title, a text and a link: "check your e-mail", "password changed", "device connected"... */
export function MessagePage({ model }: { model: MessageModel }) {
  return (
    <FlowLayout title={model.title} product={model.product}>
      <Card>
        {model.error ? <Alert kind="error">{model.text}</Alert> : <p className="text-sm">{model.text}</p>}
        {model.linkHref && <p className="text-sm"><Link href={model.linkHref}>{model.linkText ?? 'Continue'}</Link></p>}
      </Card>
    </FlowLayout>
  );
}

interface RegisterModel {
  returnUrl: string | null;
  error: string | null;
  values: { username: string | null; email: string | null; firstName: string | null; lastName: string | null } | null;
  requireEmailVerification: boolean;
  passwordLength: number;
  antiforgery: Antiforgery;
  product: Product;
}

export function RegisterPage({ model }: { model: RegisterModel }) {
  const loginUrl = model.returnUrl ? `/login?returnUrl=${encodeURIComponent(model.returnUrl)}` : '/login';
  return (
    <FlowLayout title="Create an account" product={model.product} footer={<>Already have one? <Link href={loginUrl}>Sign in</Link></>}>
      {model.error && <Alert kind="error">{model.error}</Alert>}
      <Card>
        <form method="post" action="/register" className="space-y-4">
          <AntiforgeryField antiforgery={model.antiforgery} />
          <input type="hidden" name="returnUrl" value={model.returnUrl ?? ''} />
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="firstName" name="firstName" label="First name" autoComplete="given-name" defaultValue={model.values?.firstName ?? ''} />
            <Field id="lastName" name="lastName" label="Last name" autoComplete="family-name" defaultValue={model.values?.lastName ?? ''} />
          </div>
          <Field id="username" name="username" label="User name" autoComplete="username" required defaultValue={model.values?.username ?? ''} />
          <Field id="email" name="email" label="E-mail" type="email" autoComplete="email" required defaultValue={model.values?.email ?? ''}
            hint={model.requireEmailVerification ? 'You will have to verify it before signing in.' : undefined} />
          <Field id="password" name="password" label="Password" type="password" autoComplete="new-password" required minLength={model.passwordLength} hint={`At least ${model.passwordLength} characters.`} />
          <Button type="submit" className="w-full">Create my account</Button>
        </form>
      </Card>
    </FlowLayout>
  );
}

interface ForgotPasswordModel {
  antiforgery: Antiforgery;
  product: Product;
}

export function ForgotPasswordPage({ model }: { model: ForgotPasswordModel }) {
  return (
    <FlowLayout title="Reset your password" product={model.product} footer={<Link href="/login">Back to sign in</Link>}>
      <Card>
        <p className="text-sm">Type the e-mail of your account and we will send you a link to choose a new password.</p>
        <form method="post" action="/forgot-password" className="space-y-4">
          <AntiforgeryField antiforgery={model.antiforgery} />
          <Field id="email" name="email" label="E-mail or user name" autoComplete="username" autoFocus required />
          <Button type="submit" className="w-full">Send the link</Button>
        </form>
      </Card>
    </FlowLayout>
  );
}

interface ResetPasswordModel {
  userId: string;
  token: string;
  error: string | null;
  passwordLength: number;
  antiforgery: Antiforgery;
  product: Product;
}

export function ResetPasswordPage({ model }: { model: ResetPasswordModel }) {
  return (
    <FlowLayout title="Choose a new password" product={model.product}>
      {model.error && <Alert kind="error">{model.error}</Alert>}
      <Card>
        <form method="post" action="/reset-password" className="space-y-4">
          <AntiforgeryField antiforgery={model.antiforgery} />
          <input type="hidden" name="userId" value={model.userId} />
          <input type="hidden" name="token" value={model.token} />
          <Field id="password" name="password" label="New password" type="password" autoComplete="new-password" autoFocus required minLength={model.passwordLength} hint={`At least ${model.passwordLength} characters.`} />
          <Button type="submit" className="w-full">Change my password</Button>
        </form>
      </Card>
    </FlowLayout>
  );
}

interface DeviceModel {
  error: string | null;
  antiforgery: Antiforgery;
  product: Product;
}

/** The device flow: the user types the code the device shows. */
export function DevicePage({ model }: { model: DeviceModel }) {
  return (
    <FlowLayout title="Connect a device" product={model.product}>
      {model.error && <Alert kind="error">{model.error}</Alert>}
      <Card>
        <p className="text-sm">Type the code shown on the device you want to connect.</p>
        <form method="post" action="/device" className="space-y-4">
          <AntiforgeryField antiforgery={model.antiforgery} />
          <Field id="userCode" name="userCode" label="Code" autoComplete="off" autoFocus required className="font-mono" />
          <Button type="submit" className="w-full">Continue</Button>
        </form>
      </Card>
    </FlowLayout>
  );
}
