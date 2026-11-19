import { useCallback, useState, type FormEvent } from 'react';
import { Alert, Badge, Button, Card, ConfirmDialog, Field, Modal, Muted, Table, formatDate } from '../components/ui';
import { createPasskey, describeFailure, passkeysSupported } from '../passkeys';
import { errorMessage, useAction, type PasskeyModel, type ProfileModel } from './api';
import { useAccount } from './context';

/** The user's passkeys: add one (a WebAuthn ceremony), rename or remove. */
export function PasskeysSection({ profile }: { profile: ProfileModel }) {
  const { api, profile: loaded } = useAccount();
  const supported = passkeysSupported();
  const [adding, setAdding] = useState(false);
  const [renaming, setRenaming] = useState<PasskeyModel | null>(null);
  const [removing, setRemoving] = useState<PasskeyModel | null>(null);

  const remove = useAction(useCallback(async (passkey: PasskeyModel) => {
    await api.delete(`/api/account/passkeys/${encodeURIComponent(passkey.id)}`);
    setRemoving(null);
    loaded.reload();
  }, [api, loaded]));

  return (
    <Card
      title="Passkeys"
      description="Sign in without a password, with your device's fingerprint, face or PIN."
      actions={<Button size="sm" onClick={() => setAdding(true)} disabled={!supported}>Add passkey</Button>}
    >
      {!supported && <Alert kind="info">This browser does not support passkeys. Use a recent browser to add or use one.</Alert>}
      {remove.error && <Alert kind="error" onClose={remove.clearError}>{remove.error}</Alert>}
      <Table<PasskeyModel>
        rows={profile.passkeys}
        keyOf={p => p.id}
        empty={<span>No passkeys yet.{supported && ' Add one to sign in without a password.'}</span>}
        columns={[
          { header: 'Name', cell: p => <span className="font-medium text-slate-800">{p.name ?? 'Unnamed passkey'}</span> },
          { header: 'Created', cell: p => formatDate(p.createdAt), className: 'whitespace-nowrap' },
          {
            header: 'Properties',
            cell: p => (
              <span className="flex flex-wrap gap-1">
                {p.synced ? <Badge tone="sky">Synced</Badge> : <Badge>Device-bound</Badge>}
                {p.userVerified && <Badge tone="green">User verified</Badge>}
              </span>
            ),
          },
          {
            header: <span className="sr-only">Actions</span>,
            className: 'text-right',
            cell: p => (
              <span className="flex justify-end gap-1">
                <Button variant="ghost" size="sm" onClick={() => setRenaming(p)}>Rename</Button>
                <Button variant="danger" size="sm" onClick={() => setRemoving(p)}>Remove</Button>
              </span>
            ),
          },
        ]}
      />
      {adding && <AddPasskeyDialog onClose={() => setAdding(false)} onAdded={() => { setAdding(false); loaded.reload(); }} />}
      {renaming && <RenameDialog passkey={renaming} onClose={() => setRenaming(null)} onRenamed={() => { setRenaming(null); loaded.reload(); }} />}
      {removing && (
        <ConfirmDialog
          title="Remove this passkey?"
          message={<>You will no longer be able to sign in with <strong>{removing.name ?? 'this passkey'}</strong>. Remove it from your device or password manager too.</>}
          confirmLabel="Remove"
          busy={remove.busy}
          onCancel={() => setRemoving(null)}
          onConfirm={() => void remove.run(removing)}
        />
      )}
    </Card>
  );
}

function AddPasskeyDialog({ onClose, onAdded }: { onClose: () => void; onAdded: () => void }) {
  const { api } = useAccount();
  const [name, setName] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function add(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const options = await api.post<unknown>('/api/account/passkeys/options');
      let credential: string;
      try {
        credential = await createPasskey(options);
      } catch (failure) {
        const message = describeFailure(failure, 'The passkey creation was cancelled.');
        if (message) setError(message);
        return;
      }
      await api.post<PasskeyModel>('/api/account/passkeys', { credential, name: name.trim() || null });
      onAdded();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal title="Add a passkey" onClose={onClose}>
      <form onSubmit={add} className="space-y-4" noValidate>
        <Muted>Give it a name to tell it apart later, for example the device or password manager that stores it. Your browser then asks you to create the passkey.</Muted>
        {error && <Alert kind="error" onClose={() => setError(null)}>{error}</Alert>}
        <Field label="Name" id="passkeyName" value={name} onChange={e => setName(e.target.value)} placeholder="My laptop" hint="Optional." autoFocus />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={busy}>Create passkey</Button>
        </div>
      </form>
    </Modal>
  );
}

function RenameDialog({ passkey, onClose, onRenamed }: { passkey: PasskeyModel; onClose: () => void; onRenamed: () => void }) {
  const { api } = useAccount();
  const [name, setName] = useState(passkey.name ?? '');
  const rename = useAction(useCallback(async () => {
    await api.put<PasskeyModel>(`/api/account/passkeys/${encodeURIComponent(passkey.id)}`, { name: name.trim() });
    onRenamed();
  }, [api, passkey.id, name, onRenamed]));

  return (
    <Modal title="Rename passkey" onClose={onClose}>
      <form onSubmit={e => { e.preventDefault(); void rename.run(); }} className="space-y-4" noValidate>
        {rename.error && <Alert kind="error" onClose={rename.clearError}>{rename.error}</Alert>}
        <Field label="Name" id="renamePasskey" value={name} onChange={e => setName(e.target.value)} autoFocus required />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={rename.busy} disabled={!name.trim()}>Save</Button>
        </div>
      </form>
    </Modal>
  );
}
