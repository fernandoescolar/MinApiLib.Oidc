import { useCallback, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router';
import { Alert, Badge, Button, Card, Code, DescriptionList, Muted, PageHeader, formatDate } from '../components/ui';
import { useAction, type ProfileModel } from './api';
import { useAccount } from './context';
import { Loading } from './common';

export function OverviewPage() {
  const { profile } = useAccount();
  return (
    <>
      <PageHeader title="Overview" description="Your account at a glance." />
      <Loading of={profile}>{data => <Overview profile={data} />}</Loading>
    </>
  );
}

function Overview({ profile }: { profile: ProfileModel }) {
  const { model, api } = useAccount();
  const navigate = useNavigate();
  const [sent, setSent] = useState(false);
  const resend = useAction(useCallback(async () => {
    await api.post('/api/account/email/resend-verification');
    setSent(true);
  }, [api]));

  const name = [profile.firstName, profile.lastName].filter(Boolean).join(' ');
  const checklist: { ok: boolean; label: string; fix: string; path: string }[] = [
    { ok: profile.hasPassword, label: 'Password set', fix: 'Set a password', path: '/security' },
    { ok: profile.twoFactorEnabled, label: 'Authenticator app enabled', fix: 'Set up an authenticator', path: '/security' },
    { ok: profile.passkeys.length > 0, label: profile.passkeys.length === 1 ? '1 passkey' : `${profile.passkeys.length} passkeys`, fix: 'Add a passkey', path: '/security' },
    { ok: profile.emailConfirmed, label: 'E-mail verified', fix: 'Verify your e-mail', path: '/profile' },
  ];

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card title="Who you are" actions={<Button variant="secondary" size="sm" onClick={() => navigate('/profile')}>Edit profile</Button>}>
        <DescriptionList items={[
          { label: 'Name', value: name || null },
          { label: 'User name', value: profile.userName },
          {
            label: 'E-mail',
            value: profile.email ? (
              <span className="flex flex-wrap items-center gap-2">
                <span>{profile.email}</span>
                {profile.emailConfirmed ? <Badge tone="green">Verified</Badge> : <Badge tone="amber">Unverified</Badge>}
              </span>
            ) : null,
          },
          { label: 'Phone', value: profile.phoneNumber },
          { label: 'Member since', value: formatDate(profile.createdAt) },
          { label: 'Last sign-in', value: formatDate(profile.lastSignInAt) },
        ]} />
        {profile.email && !profile.emailConfirmed && (
          sent
            ? <Alert kind="success">Verification e-mail sent. Check your inbox and follow the link.</Alert>
            : (
              <div className="flex flex-wrap items-center gap-3">
                <Button variant="secondary" size="sm" onClick={() => void resend.run()} loading={resend.busy}>Resend verification e-mail</Button>
                {resend.error && <span className="text-sm text-red-600">{resend.error}</span>}
              </div>
            )
        )}
      </Card>

      <Card title="This sign-in" description="How you authenticated for this session.">
        <DescriptionList items={[
          { label: 'Methods', value: model.user.amr.length > 0 ? <Badges items={model.user.amr.map(describeAmr)} /> : null },
          { label: 'Identity provider', value: model.user.idp && model.user.idp !== 'local' ? model.user.idp : 'This server' },
          { label: 'Authentication context', value: model.user.acr ? <Code>{model.user.acr}</Code> : null },
        ]} />
      </Card>

      <Card title="Security checklist" description="The more of these you complete, the safer your account.">
        <ul className="divide-y divide-slate-100">
          {checklist.map(item => (
            <li key={item.label} className="flex items-center justify-between gap-3 py-2.5 text-sm">
              <span className="flex items-center gap-2">
                <span aria-hidden="true" className={`grid size-5 place-items-center rounded-full text-xs ${item.ok ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>{item.ok ? '✓' : '·'}</span>
                <span className={item.ok ? 'text-slate-800' : 'text-slate-500'}>{item.label}</span>
              </span>
              {!item.ok && <Button variant="ghost" size="sm" onClick={() => navigate(item.path)}>{item.fix}</Button>}
            </li>
          ))}
        </ul>
      </Card>

      <Card title="Roles and groups">
        <Section label="Roles"><Badges items={profile.roles} tone="indigo" empty="No roles." /></Section>
        <Section label="Groups"><Badges items={profile.groups} tone="sky" empty="No groups." /></Section>
      </Card>
    </div>
  );
}

function Section({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <p className="mb-1.5 text-xs uppercase tracking-wide text-slate-500">{label}</p>
      {children}
    </div>
  );
}

function Badges({ items, tone, empty }: { items: string[]; tone?: 'slate' | 'indigo' | 'sky'; empty?: string }) {
  if (items.length === 0) return empty ? <Muted>{empty}</Muted> : null;
  return <div className="flex flex-wrap gap-1.5">{items.map(item => <Badge key={item} tone={tone}>{item}</Badge>)}</div>;
}

const amrNames: Record<string, string> = {
  pwd: 'Password',
  otp: 'One-time code',
  mfa: 'Multi-factor',
  external: 'External provider',
  passkey: 'Passkey',
  webauthn: 'Passkey',
  hwk: 'Hardware key',
  swk: 'Software key',
  sms: 'SMS',
  recovery: 'Recovery code',
};

function describeAmr(amr: string): string {
  return amrNames[amr] ?? amr;
}
