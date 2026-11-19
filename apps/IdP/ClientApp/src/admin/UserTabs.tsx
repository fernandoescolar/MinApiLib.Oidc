import { useState, type ReactNode } from 'react';
import { Badge, Button, Code, ConfirmDialog, DescriptionList, Field, Modal, Table, formatDate } from '../components/ui';
import { useAdmin, useBusy, useLoad, type Loaded } from './hooks';
import { BusyAlerts, CheckboxList, Dash, Errors, EventsTable, Loader } from './shared';
import { GrantsTable } from './SessionsPage';
import type { EventModel, GroupModel, RoleModel, UserAction, UserDetail, UserInput } from './types';

/** PUT the changed fields and keep the detail the server answers with. */
export function useUpdateUser(state: Loaded<UserDetail>) {
  const { api } = useAdmin();
  const save = useBusy();
  const update = (id: string, body: UserInput, message: string) =>
    save.run(() => api.put<UserDetail>(`/api/admin/users/${id}`, body), message).then(result => { if (result) state.setData(result); return result; });
  return { save, update };
}

function Section({ title, actions, children }: { title: string; actions?: ReactNode; children: ReactNode }) {
  return (
    <section className="space-y-3 border-b border-slate-100 pb-6 last:border-0 last:pb-0">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">{title}</h3>
        {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
      </div>
      {children}
    </section>
  );
}

const actionMessages: Record<UserAction, string> = {
  'send-password-reset': 'Password reset e-mail sent.',
  'send-verification': 'Verification e-mail sent.',
  'confirm-email': 'E-mail marked as confirmed.',
  unlock: 'Account unlocked.',
  'reset-authenticator': 'Authenticator reset; two-factor authentication is off.',
  'sign-out': 'Every session and token of the user was ended.',
};

export function CredentialsTab({ user, reload }: { user: UserDetail; reload: () => void }) {
  const { api } = useAdmin();
  const action = useBusy();
  const [settingPassword, setSettingPassword] = useState(false);
  const [confirm, setConfirm] = useState<{ title: string; message: ReactNode; run: () => Promise<unknown> } | null>(null);
  const lockedOut = !!user.lockoutEnd && new Date(user.lockoutEnd) > new Date();

  const act = async (name: UserAction) => {
    const ok = await action.run(() => api.post(`/api/admin/users/${user.id}/actions/${name}`).then(() => true), actionMessages[name]);
    if (ok) reload();
  };
  const runConfirmed = async () => {
    if (!confirm) return;
    await confirm.run();
    setConfirm(null);
  };

  return (
    <div className="space-y-6">
      <BusyAlerts state={action} />
      <Section title="Password" actions={
        <>
          <Button variant="secondary" size="sm" onClick={() => { action.clear(); setSettingPassword(true); }}>Set password</Button>
          <Button variant="secondary" size="sm" onClick={() => act('send-password-reset')} disabled={!user.email || action.busy}>Send reset e-mail</Button>
        </>
      }>
        <p className="text-sm">{user.hasPassword ? <Badge tone="green">Password set</Badge> : <Badge>No password</Badge>}</p>
      </Section>

      <Section title="E-mail" actions={
        <>
          <Button variant="secondary" size="sm" onClick={() => act('send-verification')} disabled={!user.email || user.emailConfirmed || action.busy}>Send verification e-mail</Button>
          <Button variant="secondary" size="sm" onClick={() => act('confirm-email')} disabled={!user.email || user.emailConfirmed || action.busy}>Mark as confirmed</Button>
        </>
      }>
        <p className="flex items-center gap-2 text-sm">{user.email ?? <Dash />}{user.email && (user.emailConfirmed ? <Badge tone="green">Confirmed</Badge> : <Badge tone="amber">Unconfirmed</Badge>)}</p>
      </Section>

      <Section title="Two-factor authentication" actions={
        <Button variant="danger" size="sm" disabled={action.busy} onClick={() => setConfirm({ title: 'Reset authenticator', message: 'Remove the authenticator app and recovery codes? The user signs in with their password alone until they set it up again.', run: () => act('reset-authenticator') })}>Reset authenticator</Button>
      }>
        <DescriptionList items={[
          { label: 'Status', value: user.twoFactorEnabled ? <Badge tone="green">Enabled</Badge> : <Badge>Off</Badge> },
          { label: 'Recovery codes left', value: user.recoveryCodesLeft },
        ]} />
      </Section>

      <Section title="Lockout" actions={<Button variant="secondary" size="sm" onClick={() => act('unlock')} disabled={(!lockedOut && user.accessFailedCount === 0) || action.busy}>Unlock</Button>}>
        <DescriptionList items={[
          { label: 'Status', value: lockedOut ? <Badge tone="amber">Locked out until {formatDate(user.lockoutEnd)}</Badge> : <Badge tone="green">Not locked</Badge> },
          { label: 'Failed attempts', value: user.accessFailedCount },
        ]} />
      </Section>

      <Section title="Passkeys">
        <Table<UserDetail['passkeys'][number]>
          rows={user.passkeys}
          keyOf={p => p.id}
          empty="No passkeys."
          columns={[
            { header: 'Name', cell: p => p.name ?? <span className="text-slate-400">Unnamed</span> },
            { header: 'Created', cell: p => formatDate(p.createdAt) },
            { header: 'Flags', cell: p => <span className="flex gap-1">{p.synced && <Badge tone="sky">Synced</Badge>}{p.userVerified && <Badge tone="green">User verified</Badge>}</span> },
            { header: '', className: 'text-right', cell: p => <Button variant="danger" size="sm" onClick={() => setConfirm({ title: 'Remove passkey', message: <>Remove the passkey <strong>{p.name ?? p.id}</strong>?</>, run: () => action.run(() => api.delete(`/api/admin/users/${user.id}/passkeys/${encodeURIComponent(p.id)}`).then(reload), 'Passkey removed.') })}>Remove</Button> },
          ]}
        />
      </Section>

      <Section title="Linked accounts">
        <Table<UserDetail['logins'][number]>
          rows={user.logins}
          keyOf={l => `${l.provider}:${l.providerKey}`}
          empty="No external accounts linked."
          columns={[
            { header: 'Provider', cell: l => <>{l.displayName ?? l.provider} <Code>{l.provider}</Code></> },
            { header: 'Provider key', cell: l => <Code>{l.providerKey}</Code> },
            { header: 'Linked', cell: l => formatDate(l.linkedAt) },
            { header: '', className: 'text-right', cell: l => <Button variant="danger" size="sm" onClick={() => setConfirm({ title: 'Unlink account', message: <>Unlink the {l.displayName ?? l.provider} account? The user can no longer sign in with it.</>, run: () => action.run(() => api.delete(`/api/admin/users/${user.id}/logins/${encodeURIComponent(l.provider)}`).then(reload), 'Account unlinked.') })}>Unlink</Button> },
          ]}
        />
      </Section>

      {settingPassword && <SetPasswordModal userId={user.id} onClose={() => setSettingPassword(false)} onDone={() => { setSettingPassword(false); reload(); }} />}
      {confirm && <ConfirmDialog title={confirm.title} message={confirm.message} busy={action.busy} onConfirm={runConfirmed} onCancel={() => setConfirm(null)} />}
    </div>
  );
}

function SetPasswordModal({ userId, onClose, onDone }: { userId: string; onClose: () => void; onDone: () => void }) {
  const { api } = useAdmin();
  const save = useBusy();
  const [password, setPassword] = useState('');
  const [repeat, setRepeat] = useState('');
  const mismatch = repeat !== '' && repeat !== password;

  const submit = async () => {
    const ok = await save.run(() => api.put(`/api/admin/users/${userId}/password`, { password }).then(() => true));
    if (ok) onDone();
  };

  return (
    <Modal title="Set password" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={save.busy} disabled={!password || mismatch}>Set password</Button></>}>
      <Errors messages={save.error} />
      <form onSubmit={e => { e.preventDefault(); void submit(); }} className="space-y-4">
        <p className="text-sm text-slate-600">The current password is replaced and the user's sessions end.</p>
        <Field id="sp-password" label="New password" type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete="new-password" autoFocus required />
        <Field id="sp-repeat" label="Repeat" type="password" value={repeat} onChange={e => setRepeat(e.target.value)} autoComplete="new-password" error={mismatch ? 'The passwords differ.' : undefined} />
      </form>
    </Modal>
  );
}

export function RolesGroupsTab({ user, state }: { user: UserDetail; state: Loaded<UserDetail> }) {
  const { api } = useAdmin();
  const { save, update } = useUpdateUser(state);
  const lists = useLoad(async () => {
    const [roles, groups] = await Promise.all([api.get<RoleModel[]>('/api/admin/roles'), api.get<GroupModel[]>('/api/admin/groups')]);
    return { roles, groups };
  }, [api]);
  const [roles, setRoles] = useState(user.roles);
  const [groups, setGroups] = useState(user.groups.map(g => g.id));

  return (
    <Loader state={lists}>
      {({ roles: allRoles, groups: allGroups }) => (
        <form onSubmit={e => { e.preventDefault(); void update(user.id, { roles, groups }, 'Roles and groups saved.'); }} className="space-y-6">
          <BusyAlerts state={save} />
          <Section title="Roles">
            <CheckboxList id="roles" options={allRoles.map(r => ({ value: r.name, label: r.name, hint: r.description ?? undefined }))} selected={roles} onChange={setRoles} empty="No roles defined." />
          </Section>
          <Section title="Groups">
            <CheckboxList id="groups" options={allGroups.map(g => ({ value: g.id, label: g.name, hint: g.roles.length > 0 ? `Grants ${g.roles.join(', ')}` : g.description ?? undefined }))} selected={groups} onChange={setGroups} empty="No groups defined." />
          </Section>
          <Section title="Effective roles">
            <p className="text-sm text-slate-500">Direct roles and the roles of the groups, as last saved.</p>
            <div className="flex flex-wrap gap-1">{user.effectiveRoles.length === 0 ? <Dash /> : user.effectiveRoles.map(r => <Badge key={r} tone="indigo">{r}</Badge>)}</div>
          </Section>
          <div className="flex justify-end"><Button type="submit" loading={save.busy}>Save changes</Button></div>
        </form>
      )}
    </Loader>
  );
}

export function SessionsTab({ user }: { user: UserDetail }) {
  const { api } = useAdmin();
  const action = useBusy();
  const [page, setPage] = useState(1);
  const [confirming, setConfirming] = useState(false);
  const [version, setVersion] = useState(0);

  const signOut = async () => {
    await action.run(() => api.post(`/api/admin/users/${user.id}/actions/sign-out`), actionMessages['sign-out']);
    setConfirming(false);
    setVersion(v => v + 1);
  };

  return (
    <div className="space-y-4">
      <BusyAlerts state={action} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">Tokens and consents granted to applications by this user.</p>
        <Button variant="danger" size="sm" onClick={() => setConfirming(true)}>Sign out everywhere</Button>
      </div>
      <GrantsTable key={version} filters={{ subjectId: user.id }} page={page} onPage={setPage} revokeAll showSubject={false} />
      {confirming && (
        <ConfirmDialog
          title="Sign out everywhere"
          message="End every session of the user and revoke all their tokens? Applications must sign them in again."
          confirmLabel="Sign out"
          busy={action.busy}
          onConfirm={signOut}
          onCancel={() => setConfirming(false)}
        />
      )}
    </div>
  );
}

export function ActivityTab({ user }: { user: UserDetail }) {
  const { api } = useAdmin();
  const events = useLoad(() => api.get<EventModel[]>(`/api/admin/users/${user.id}/events`), [api, user.id]);
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm text-slate-500">The last 100 events of this user. Click a row for the details.</p>
        <Button variant="secondary" size="sm" onClick={events.reload}>Refresh</Button>
      </div>
      <Loader state={events}>{list => <EventsTable events={list} showUser={false} />}</Loader>
    </div>
  );
}
