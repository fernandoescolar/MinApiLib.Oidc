import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Badge, Button, Card, Checkbox, Field, Modal, PageHeader, Pagination, Select, Table, formatDate } from '../components/ui';
import { query, type Page } from '../api';
import { pageNumber, useAdmin, useBusy, useDebounced, useLoad, useQueryParams } from './hooks';
import { Dash, Errors, Loader } from './shared';
import type { GroupModel, RoleModel, UserDetail, UserInput, UserSummary } from './types';

const filterKeys = ['q', 'role', 'group', 'enabled', 'page'] as const;

export function UsersPage() {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const [filters, setFilters] = useQueryParams(filterKeys);
  const q = useDebounced(filters.q);
  const page = pageNumber(filters.page);
  const users = useLoad(
    () => api.get<Page<UserSummary>>(`/api/admin/users${query({ q, role: filters.role, group: filters.group, enabled: filters.enabled, page, pageSize: 25 })}`),
    [api, q, filters.role, filters.group, filters.enabled, page],
  );
  const roles = useLoad(() => api.get<RoleModel[]>('/api/admin/roles'), [api]);
  const groups = useLoad(() => api.get<GroupModel[]>('/api/admin/groups'), [api]);
  const [creating, setCreating] = useState(false);

  return (
    <>
      <PageHeader title="Users" description="The accounts of this server." actions={<Button onClick={() => setCreating(true)}>New user</Button>} />
      <Card>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Field id="users-q" label="Search" type="search" placeholder="User name, e-mail or name" value={filters.q} onChange={e => setFilters({ q: e.target.value, page: null })} />
          <Select id="users-role" label="Role" value={filters.role} onChange={e => setFilters({ role: e.target.value, page: null })}>
            <option value="">Any</option>
            {roles.data?.map(r => <option key={r.id} value={r.name}>{r.name}</option>)}
          </Select>
          <Select id="users-group" label="Group" value={filters.group} onChange={e => setFilters({ group: e.target.value, page: null })}>
            <option value="">Any</option>
            {groups.data?.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
          </Select>
          <Select id="users-enabled" label="Status" value={filters.enabled} onChange={e => setFilters({ enabled: e.target.value, page: null })}>
            <option value="">Any</option>
            <option value="true">Enabled</option>
            <option value="false">Disabled</option>
          </Select>
        </div>
        <Loader state={users}>
          {result => (
            <>
              <Table<UserSummary>
                rows={result.items}
                keyOf={u => u.id}
                onRowClick={u => navigate(`/users/${u.id}`)}
                empty="No users match."
                columns={[
                  { header: 'User', cell: u => <><span className="font-medium text-slate-800">{u.userName}</span>{u.displayName !== u.userName && <span className="block text-xs text-slate-500">{u.displayName}</span>}</> },
                  { header: 'E-mail', cell: u => u.email ? <span className="flex items-center gap-1.5">{u.email}{!u.emailConfirmed && <Badge tone="amber">Unconfirmed</Badge>}</span> : <Dash /> },
                  { header: 'Roles', cell: u => u.roles.length === 0 ? <Dash /> : <span className="flex flex-wrap gap-1">{u.roles.map(r => <Badge key={r} tone="indigo">{r}</Badge>)}</span> },
                  {
                    header: 'Status',
                    cell: u => (
                      <span className="flex flex-wrap gap-1">
                        {!u.enabled && <Badge tone="red">Disabled</Badge>}
                        {u.lockedOut && <Badge tone="amber">Locked out</Badge>}
                        {u.twoFactorEnabled && <Badge tone="green">2FA</Badge>}
                        {u.enabled && !u.lockedOut && !u.twoFactorEnabled && <Badge tone="green">Active</Badge>}
                      </span>
                    ),
                  },
                  { header: 'Last sign-in', cell: u => <span className="whitespace-nowrap">{formatDate(u.lastSignInAt)}</span> },
                  { header: 'Created', cell: u => <span className="whitespace-nowrap">{formatDate(u.createdAt)}</span> },
                ]}
              />
              <Pagination page={result.pageNumber} pageSize={result.pageSize} total={result.total} onChange={p => setFilters({ page: String(p) })} />
            </>
          )}
        </Loader>
      </Card>
      {creating && <NewUserModal onClose={() => setCreating(false)} onCreated={user => navigate(`/users/${user.id}`)} />}
    </>
  );
}

function NewUserModal({ onClose, onCreated }: { onClose: () => void; onCreated: (user: UserDetail) => void }) {
  const { api } = useAdmin();
  const save = useBusy();
  const [form, setForm] = useState({ userName: '', email: '', firstName: '', lastName: '', enabled: true, emailConfirmed: false, mode: 'password' as 'password' | 'invite' | 'none', password: '' });
  const canInvite = form.email.trim() !== '';

  const submit = async () => {
    const body: UserInput = {
      userName: form.userName.trim(),
      email: form.email.trim() || undefined,
      emailConfirmed: form.emailConfirmed,
      firstName: form.firstName.trim() || undefined,
      lastName: form.lastName.trim() || undefined,
      enabled: form.enabled,
      password: form.mode === 'password' ? form.password : undefined,
      sendInvitation: form.mode === 'invite',
    };
    const user = await save.run(() => api.post<UserDetail>('/api/admin/users', body));
    if (user) onCreated(user);
  };

  const valid = form.userName.trim() !== '' && (form.mode !== 'password' || form.password.length > 0) && (form.mode !== 'invite' || canInvite);

  return (
    <Modal title="New user" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={save.busy} disabled={!valid}>Create</Button></>}>
      <Errors messages={save.error} />
      <form onSubmit={e => { e.preventDefault(); void submit(); }} className="space-y-4">
        <Field id="nu-userName" label="User name" value={form.userName} onChange={e => setForm({ ...form, userName: e.target.value })} required autoFocus autoComplete="off" />
        <Field id="nu-email" label="E-mail" type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} autoComplete="off" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Field id="nu-firstName" label="First name" value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} />
          <Field id="nu-lastName" label="Last name" value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} />
        </div>
        <Checkbox id="nu-enabled" label="Enabled" checked={form.enabled} onChange={e => setForm({ ...form, enabled: e.target.checked })} />
        <Checkbox id="nu-confirmed" label="E-mail already confirmed" checked={form.emailConfirmed} onChange={e => setForm({ ...form, emailConfirmed: e.target.checked })} disabled={!canInvite} />
        <Select id="nu-mode" label="Credentials" value={form.mode} onChange={e => setForm({ ...form, mode: e.target.value as typeof form.mode })}>
          <option value="password">Set a password now</option>
          <option value="invite" disabled={!canInvite}>Send an invitation e-mail</option>
          <option value="none">None yet (passkey or external sign-in)</option>
        </Select>
        {form.mode === 'password' && <Field id="nu-password" label="Password" type="password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} autoComplete="new-password" required />}
        {form.mode === 'invite' && <p className="text-xs text-slate-500">The user receives a link to choose their password.</p>}
      </form>
    </Modal>
  );
}
