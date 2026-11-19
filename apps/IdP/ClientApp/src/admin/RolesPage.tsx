import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Badge, Button, Card, ConfirmDialog, Field, Modal, PageHeader, Table, TextArea } from '../components/ui';
import { useAdmin, useBusy, useLoad } from './hooks';
import { BusyAlerts, Dash, Errors, Loader } from './shared';
import type { RoleModel } from './types';

type Editing = { role: RoleModel | null; name: string; description: string };

export function RolesPage() {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const roles = useLoad(() => api.get<RoleModel[]>('/api/admin/roles'), [api]);
  const action = useBusy();
  const [editing, setEditing] = useState<Editing | null>(null);
  const [deleting, setDeleting] = useState<RoleModel | null>(null);

  const save = async () => {
    if (!editing) return;
    const body = { name: editing.name.trim(), description: editing.description.trim() || null };
    const ok = await action.run(
      () => (editing.role ? api.put(`/api/admin/roles/${editing.role.id}`, body) : api.post('/api/admin/roles', body)).then(() => true),
      editing.role ? 'Role saved.' : 'Role created.',
    );
    if (ok) { setEditing(null); roles.reload(); }
  };

  const remove = async () => {
    if (!deleting) return;
    const ok = await action.run(() => api.delete(`/api/admin/roles/${deleting.id}`).then(() => true), 'Role deleted.');
    if (ok) { setDeleting(null); roles.reload(); }
  };

  return (
    <>
      <PageHeader title="Roles" description="Roles are granted to users directly or through their groups." actions={<Button onClick={() => { action.clear(); setEditing({ role: null, name: '', description: '' }); }}>New role</Button>} />
      <Card>
        <BusyAlerts state={action} />
        <Loader state={roles}>
          {list => (
            <Table<RoleModel>
              rows={list}
              keyOf={r => r.id}
              empty="No roles yet."
              columns={[
                { header: 'Name', cell: r => <span className="flex items-center gap-2 font-medium text-slate-800">{r.name}{r.builtIn && <Badge tone="indigo">Built-in</Badge>}</span> },
                { header: 'Description', cell: r => r.description ?? <Dash /> },
                { header: 'Users', cell: r => <button type="button" className="font-medium text-indigo-600 hover:text-indigo-500" onClick={() => navigate(`/users?role=${encodeURIComponent(r.name)}`)}>{r.users}</button> },
                {
                  header: '',
                  className: 'text-right',
                  cell: r => (
                    <span className="inline-flex gap-2">
                      <Button variant="secondary" size="sm" onClick={() => { action.clear(); setEditing({ role: r, name: r.name, description: r.description ?? '' }); }}>Edit</Button>
                      <Button variant="danger" size="sm" disabled={r.builtIn} title={r.builtIn ? 'Built-in roles cannot be deleted.' : undefined} onClick={() => setDeleting(r)}>Delete</Button>
                    </span>
                  ),
                },
              ]}
            />
          )}
        </Loader>
      </Card>

      {editing && (
        <Modal title={editing.role ? `Edit ${editing.role.name}` : 'New role'} onClose={() => setEditing(null)} footer={<><Button variant="secondary" onClick={() => setEditing(null)}>Cancel</Button><Button onClick={save} loading={action.busy} disabled={!editing.name.trim()}>{editing.role ? 'Save' : 'Create'}</Button></>}>
          <Errors messages={action.error} />
          <form onSubmit={e => { e.preventDefault(); void save(); }} className="space-y-4">
            <Field id="role-name" label="Name" value={editing.name} onChange={e => setEditing({ ...editing, name: e.target.value })} required maxLength={100} disabled={editing.role?.builtIn} hint={editing.role?.builtIn ? 'A built-in role cannot be renamed.' : undefined} autoFocus />
            <TextArea id="role-description" label="Description" value={editing.description} onChange={e => setEditing({ ...editing, description: e.target.value })} />
          </form>
        </Modal>
      )}
      {deleting && (
        <ConfirmDialog
          title="Delete role"
          message={<>Delete the role <strong>{deleting.name}</strong>? It is removed from {deleting.users} user{deleting.users === 1 ? '' : 's'} and from every group.</>}
          confirmLabel="Delete"
          busy={action.busy}
          onConfirm={remove}
          onCancel={() => setDeleting(null)}
        />
      )}
    </>
  );
}
