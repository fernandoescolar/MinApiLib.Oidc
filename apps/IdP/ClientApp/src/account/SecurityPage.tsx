import { useCallback, useState, type FormEvent } from 'react';
import { Alert, Button, Card, ConfirmDialog, Field, Muted, PageHeader } from '../components/ui';
import { errorMessage, fieldErrors, useAction, type ProfileModel } from './api';
import { useAccount } from './context';
import { Loading } from './common';
import { TotpSection } from './TotpSection';
import { PasskeysSection } from './PasskeysSection';

export function SecurityPage() {
  const { profile } = useAccount();
  return (
    <>
      <PageHeader title="Security" description="How you sign in, and where you are signed in." />
      <Loading of={profile}>
        {data => (
          <div className="grid gap-6 lg:grid-cols-2">
            <PasswordSection profile={data} />
            <TotpSection profile={data} />
            <div className="lg:col-span-2"><PasskeysSection profile={data} /></div>
            <div className="lg:col-span-2"><SessionsSection /></div>
          </div>
        )}
      </Loading>
    </>
  );
}

function PasswordSection({ profile }: { profile: ProfileModel }) {
  const { api, profile: loaded } = useAccount();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const mismatch = confirm !== '' && confirm !== newPassword;

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (mismatch) return;
    setSaving(true);
    setError(null);
    setErrors({});
    setDone(false);
    try {
      await api.post('/api/account/password', { currentPassword: profile.hasPassword ? currentPassword : null, newPassword });
      setDone(true);
      setCurrentPassword('');
      setNewPassword('');
      setConfirm('');
      // hasPassword flips when a password was set for the first time.
      if (!profile.hasPassword) loaded.reload();
    } catch (failure) {
      const fields = fieldErrors(failure);
      // Identity errors are keyed by code (PasswordTooShort...), not by field: they go to the general alert.
      if (fields?.currentPassword) setErrors({ currentPassword: fields.currentPassword });
      else setError(errorMessage(failure));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card title="Password" description={profile.hasPassword ? 'Change the password you sign in with.' : 'You have no password: you sign in with a passkey or an external account. You can set one.'}>
      <form onSubmit={submit} className="space-y-4" noValidate>
        {done && <Alert kind="success" onClose={() => setDone(false)}>{profile.hasPassword ? 'Password changed.' : 'Password set. You can now sign in with it.'}</Alert>}
        {error && <Alert kind="error" onClose={() => setError(null)}>{error}</Alert>}
        {profile.hasPassword && (
          <Field label="Current password" id="currentPassword" type="password" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} error={errors.currentPassword} autoComplete="current-password" required />
        )}
        <Field label="New password" id="newPassword" type="password" value={newPassword} onChange={e => setNewPassword(e.target.value)} autoComplete="new-password" required />
        <Field label="Confirm new password" id="confirmPassword" type="password" value={confirm} onChange={e => setConfirm(e.target.value)} error={mismatch ? 'The passwords do not match.' : undefined} autoComplete="new-password" required />
        <div className="flex justify-end">
          <Button type="submit" loading={saving} disabled={!newPassword || mismatch || (profile.hasPassword && !currentPassword)}>{profile.hasPassword ? 'Change password' : 'Set password'}</Button>
        </div>
      </form>
    </Card>
  );
}

function SessionsSection() {
  const { api } = useAccount();
  const [confirming, setConfirming] = useState(false);
  const signOut = useAction(useCallback(async () => {
    const { redirect } = await api.post<{ redirect: string }>('/api/account/sign-out-everywhere');
    window.location.assign(redirect);
  }, [api]));

  return (
    <Card title="Sessions" description="Every browser and device where this account is signed in.">
      {signOut.error && <Alert kind="error" onClose={signOut.clearError}>{signOut.error}</Alert>}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Muted>Signing out everywhere ends every session, this one included, and revokes the tokens applications hold for you. You will sign in again.</Muted>
        <Button variant="danger" onClick={() => setConfirming(true)}>Sign out everywhere</Button>
      </div>
      {confirming && (
        <ConfirmDialog
          title="Sign out everywhere?"
          message="Every session on every device ends, and applications lose the access tokens you granted them. You will be taken to the sign-in page."
          confirmLabel="Sign out everywhere"
          busy={signOut.busy}
          onCancel={() => setConfirming(false)}
          onConfirm={() => void signOut.run()}
        />
      )}
    </Card>
  );
}
