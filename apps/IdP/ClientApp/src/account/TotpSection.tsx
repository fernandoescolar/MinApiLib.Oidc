import { useCallback, useEffect, useState, type FormEvent } from 'react';
import QRCode from 'qrcode';
import { Alert, Badge, Button, Card, Code, ConfirmDialog, CopyButton, Field, Modal, Muted, Spinner } from '../components/ui';
import { errorMessage, fieldErrors, useAction, type ProfileModel, type RecoveryCodes, type TotpSetup } from './api';
import { useAccount } from './context';

/** The authenticator app (TOTP): set it up, show the recovery codes, renew them or disable it. */
export function TotpSection({ profile }: { profile: ProfileModel }) {
  const { api, profile: loaded } = useAccount();
  const [setup, setSetup] = useState<TotpSetup | null>(null);
  const [codes, setCodes] = useState<string[] | null>(null);
  const [confirm, setConfirm] = useState<'disable' | 'renew' | null>(null);

  const start = useAction(useCallback(async () => {
    setSetup(await api.post<TotpSetup>('/api/account/totp/setup'));
  }, [api]));

  const disable = useAction(useCallback(async () => {
    await api.delete('/api/account/totp');
    setConfirm(null);
    loaded.reload();
  }, [api, loaded]));

  const renew = useAction(useCallback(async () => {
    const result = await api.post<RecoveryCodes>('/api/account/recovery-codes');
    setConfirm(null);
    setCodes(result.recoveryCodes);
    loaded.reload();
  }, [api, loaded]));

  const error = start.error ?? disable.error ?? renew.error;

  return (
    <Card
      title="Authenticator app"
      description="A second factor: a code from an app such as Google Authenticator or 1Password, asked at sign-in."
      actions={profile.twoFactorEnabled ? <Badge tone="green">Enabled</Badge> : <Badge>Off</Badge>}
    >
      {error && <Alert kind="error" onClose={() => { start.clearError(); disable.clearError(); renew.clearError(); }}>{error}</Alert>}

      {profile.twoFactorEnabled ? (
        <>
          <Muted>
            Recovery codes left: <strong className={profile.recoveryCodesLeft <= 2 ? 'text-amber-700' : 'text-slate-700'}>{profile.recoveryCodesLeft}</strong>.
            {profile.recoveryCodesLeft <= 2 && ' Generate new ones before you run out.'}
          </Muted>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" size="sm" onClick={() => setConfirm('renew')}>New recovery codes</Button>
            <Button variant="danger" size="sm" onClick={() => setConfirm('disable')}>Disable</Button>
          </div>
        </>
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <Muted>Not set up. Anyone with your password can sign in.</Muted>
          <Button onClick={() => void start.run()} loading={start.busy}>Set up</Button>
        </div>
      )}

      {setup && (
        <SetupDialog
          setup={setup}
          onClose={() => setSetup(null)}
          onEnabled={recoveryCodes => { setSetup(null); setCodes(recoveryCodes); loaded.reload(); }}
        />
      )}
      {codes && <RecoveryCodesDialog codes={codes} onClose={() => setCodes(null)} />}
      {confirm === 'disable' && (
        <ConfirmDialog
          title="Disable the authenticator app?"
          message="Sign-in will only ask for your password or passkey. Your recovery codes stop working."
          confirmLabel="Disable"
          busy={disable.busy}
          onCancel={() => setConfirm(null)}
          onConfirm={() => void disable.run()}
        />
      )}
      {confirm === 'renew' && (
        <ConfirmDialog
          title="Generate new recovery codes?"
          message="Your current recovery codes stop working. Save the new ones somewhere safe."
          confirmLabel="Generate"
          danger={false}
          busy={renew.busy}
          onCancel={() => setConfirm(null)}
          onConfirm={() => void renew.run()}
        />
      )}
    </Card>
  );
}

function SetupDialog({ setup, onClose, onEnabled }: { setup: TotpSetup; onClose: () => void; onEnabled: (recoveryCodes: string[]) => void }) {
  const { api } = useAccount();
  const [qr, setQr] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    QRCode.toDataURL(setup.authenticatorUri, { margin: 1, width: 200 }).then(url => { if (active) setQr(url); }, () => { if (active) setQr(''); });
    return () => { active = false; };
  }, [setup.authenticatorUri]);

  async function enable(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const result = await api.post<RecoveryCodes>('/api/account/totp/enable', { code: code.trim() });
      onEnabled(result.recoveryCodes);
    } catch (failure) {
      setError(fieldErrors(failure)?.code ?? errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal title="Set up the authenticator app" onClose={onClose}>
      <form onSubmit={enable} className="space-y-4" noValidate>
        <ol className="list-decimal space-y-3 pl-5 text-sm text-slate-700">
          <li>Install an authenticator app on your phone, if you have none.</li>
          <li>
            Scan this QR code with it, or type the key by hand.
            <div className="mt-3 flex flex-col items-center gap-3 sm:flex-row sm:items-start">
              <div className="grid size-[200px] shrink-0 place-items-center rounded-lg border border-slate-200 bg-white">
                {qr === null && <Spinner />}
                {qr && <img src={qr} alt="QR code of the authenticator key" width={200} height={200} />}
                {qr === '' && <Muted>QR code unavailable</Muted>}
              </div>
              <div className="min-w-0 space-y-2">
                <p className="text-xs text-slate-500">Key</p>
                <Code className="block break-all text-sm">{setup.sharedKey}</Code>
                <CopyButton value={setup.sharedKey.replace(/\s/g, '')} label="Copy key" />
              </div>
            </div>
          </li>
          <li>
            Type the 6-digit code the app shows.
            <div className="mt-2">
              <Field label="Verification code" id="totpCode" value={code} onChange={e => setCode(e.target.value)} inputMode="numeric" autoComplete="one-time-code" placeholder="123 456" error={error ?? undefined} autoFocus />
            </div>
          </li>
        </ol>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={busy} disabled={code.replace(/\s/g, '').length < 6}>Enable</Button>
        </div>
      </form>
    </Modal>
  );
}

function RecoveryCodesDialog({ codes, onClose }: { codes: string[]; onClose: () => void }) {
  return (
    <Modal title="Your recovery codes" onClose={onClose} footer={<><CopyButton value={codes.join('\n')} label="Copy codes" /><Button onClick={onClose}>I saved them</Button></>}>
      <Alert kind="warning">Save these codes somewhere safe. Each one signs you in once if you lose your authenticator, and they are shown only now.</Alert>
      <ul className="grid grid-cols-2 gap-2 font-mono text-sm">
        {codes.map(c => <li key={c} className="rounded border border-slate-200 bg-slate-50 px-3 py-1.5 text-center">{c}</li>)}
      </ul>
    </Modal>
  );
}
