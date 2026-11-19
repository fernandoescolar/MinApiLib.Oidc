import { useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { Badge, Button, Card, Checkbox, Code, ConfirmDialog, Field, PageHeader, Tabs, Toggle } from '../components/ui';
import { useAdmin, useLoad, type Loaded } from './hooks';
import { BusyAlerts, ClaimsEditor, KeyValueEditor, Loader } from './shared';
import { ActivityTab, CredentialsTab, RolesGroupsTab, SessionsTab, useUpdateUser } from './UserTabs';
import type { UserDetail } from './types';

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'credentials', label: 'Credentials' },
  { id: 'roles', label: 'Roles & groups' },
  { id: 'sessions', label: 'Sessions' },
  { id: 'activity', label: 'Activity' },
  { id: 'danger', label: 'Danger zone' },
];

export function UserDetailPage() {
  const { api } = useAdmin();
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const [search, setSearch] = useSearchParams();
  const tab = tabs.some(t => t.id === search.get('tab')) ? search.get('tab')! : 'details';
  const user = useLoad(() => api.get<UserDetail>(`/api/admin/users/${id}`), [api, id]);

  return (
    <Loader state={user}>
      {u => (
        <>
          <PageHeader
            title={u.userName}
            description={
              <span className="flex flex-wrap items-center gap-2">
                {u.email && <span>{u.email}</span>}
                <Code>{u.id}</Code>
                {!u.enabled && <Badge tone="red">Disabled</Badge>}
                {u.lockoutEnd && new Date(u.lockoutEnd) > new Date() && <Badge tone="amber">Locked out</Badge>}
                {u.twoFactorEnabled && <Badge tone="green">2FA</Badge>}
              </span>
            }
            actions={<Button variant="secondary" onClick={() => navigate('/users')}>Back</Button>}
          />
          <Card>
            <Tabs tabs={tabs} active={tab} onChange={t => setSearch({ tab: t }, { replace: true })} />
            <div className="pt-2">
              {tab === 'details' && <DetailsTab user={u} state={user} />}
              {tab === 'credentials' && <CredentialsTab user={u} reload={user.reload} />}
              {tab === 'roles' && <RolesGroupsTab user={u} state={user} />}
              {tab === 'sessions' && <SessionsTab user={u} />}
              {tab === 'activity' && <ActivityTab user={u} />}
              {tab === 'danger' && <DangerTab user={u} state={user} />}
            </div>
          </Card>
        </>
      )}
    </Loader>
  );
}

function DetailsTab({ user, state }: { user: UserDetail; state: Loaded<UserDetail> }) {
  const { save, update } = useUpdateUser(state);
  const [form, setForm] = useState({
    userName: user.userName,
    email: user.email ?? '',
    emailConfirmed: user.emailConfirmed,
    firstName: user.firstName ?? '',
    lastName: user.lastName ?? '',
    phoneNumber: user.phoneNumber ?? '',
    claims: user.claims,
    attributes: user.attributes,
  });

  const submit = () => update(user.id, {
    userName: form.userName.trim(),
    email: form.email.trim(),
    emailConfirmed: form.emailConfirmed,
    firstName: form.firstName,
    lastName: form.lastName,
    phoneNumber: form.phoneNumber,
    claims: form.claims,
    attributes: form.attributes,
  }, 'User saved.');

  return (
    <form onSubmit={e => { e.preventDefault(); void submit(); }} className="space-y-6">
      <BusyAlerts state={save} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="u-userName" label="User name" value={form.userName} onChange={e => setForm({ ...form, userName: e.target.value })} required />
        <Field id="u-email" label="E-mail" type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} hint="Changing it marks the e-mail unconfirmed." />
        <Field id="u-firstName" label="First name" value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} />
        <Field id="u-lastName" label="Last name" value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} />
        <Field id="u-phone" label="Phone number" type="tel" value={form.phoneNumber} onChange={e => setForm({ ...form, phoneNumber: e.target.value })} hint={user.phoneNumberConfirmed ? 'Confirmed.' : undefined} />
      </div>
      <Checkbox id="u-emailConfirmed" label="E-mail confirmed" checked={form.emailConfirmed} onChange={e => setForm({ ...form, emailConfirmed: e.target.checked })} />
      <KeyValueEditor label="Attributes" value={form.attributes} onChange={attributes => setForm({ ...form, attributes })} />
      <ClaimsEditor label="Claims" value={form.claims} onChange={claims => setForm({ ...form, claims })} />
      <div className="flex items-center justify-between gap-3 text-xs text-slate-500">
        <span>Created {new Date(user.createdAt).toLocaleString()} · updated {new Date(user.updatedAt).toLocaleString()}</span>
        <Button type="submit" loading={save.busy} disabled={!form.userName.trim()}>Save changes</Button>
      </div>
    </form>
  );
}

function DangerTab({ user, state }: { user: UserDetail; state: Loaded<UserDetail> }) {
  const { api, model } = useAdmin();
  const navigate = useNavigate();
  const { save, update } = useUpdateUser(state);
  const [deleting, setDeleting] = useState(false);
  const isSelf = user.id === model.user.id;

  const remove = async () => {
    const ok = await save.run(() => api.delete(`/api/admin/users/${user.id}`).then(() => true));
    setDeleting(false);
    if (ok) navigate('/users');
  };

  return (
    <div className="space-y-6">
      <BusyAlerts state={save} />
      <Toggle
        label="Account enabled"
        hint="Disabling ends the user's sessions at once; their tokens stop at the next refresh."
        checked={user.enabled}
        disabled={save.busy || isSelf}
        onChange={enabled => void update(user.id, { enabled }, enabled ? 'Account enabled.' : 'Account disabled.')}
      />
      <div className="rounded-lg border border-red-200 bg-red-50 p-4">
        <p className="text-sm font-medium text-red-800">Delete this account</p>
        <p className="mt-1 text-sm text-red-700">Removes the user, their credentials, passkeys and linked accounts, and revokes every token. This cannot be undone.</p>
        <Button variant="danger" className="mt-3" onClick={() => setDeleting(true)} disabled={isSelf} title={isSelf ? 'You cannot delete your own account from here.' : undefined}>Delete user</Button>
      </div>
      {deleting && (
        <ConfirmDialog
          title="Delete user"
          message={<>Delete <strong>{user.userName}</strong> permanently?</>}
          confirmLabel="Delete"
          busy={save.busy}
          onConfirm={remove}
          onCancel={() => setDeleting(false)}
        />
      )}
    </div>
  );
}
