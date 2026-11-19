import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Badge, Button, Card, ConfirmDialog, Field, Modal, PageHeader, Table, TextArea, formatDate } from '../components/ui';
import { useAdmin, useBusy, useLoad } from './hooks';
import { BusyAlerts, CheckboxList, Dash, Errors, KeyValueEditor, Loader } from './shared';
import type { GroupInput, GroupModel, RoleModel } from './types';

export function GroupsPage() {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const groups = useLoad(() => api.get<GroupModel[]>('/api/admin/groups'), [api]);
  const action = useBusy();
  const [editing, setEditing] = useState<GroupModel | 'new' | null>(null);
  const [deleting, setDeleting] = useState<GroupModel | null>(null);

  const remove = async () => {
    if (!deleting) return;
    const ok = await action.run(() => api.delete(`/api/admin/groups/${deleting.id}`).then(() => true), 'Group deleted.');
    setDeleting(null);
    if (ok) groups.reload();
  };

  return (
    <>
      <PageHeader title="Groups" description="Groups gather users and grant them roles together." actions={<Button onClick={() => setEditing('new')}>New group</Button>} />
      <Card>
        <BusyAlerts state={action} />
        <Loader state={groups}>
          {list => (
            <Table<GroupModel>
              rows={list}
              keyOf={g => g.id}
              onRowClick={g => navigate(`/groups/${g.id}`)}
              empty="No groups yet."
              columns={[
                { header: 'Name', cell: g => <span className="font-medium text-slate-800">{g.name}</span> },
                { header: 'Description', className: 'max-w-xs', cell: g => g.description ?? <Dash /> },
                { header: 'Roles', cell: g => g.roles.length === 0 ? <Dash /> : <span className="flex flex-wrap gap-1">{g.roles.map(r => <Badge key={r} tone="indigo">{r}</Badge>)}</span> },
                { header: 'Members', cell: g => g.members },
                { header: 'Created', cell: g => <span className="whitespace-nowrap">{formatDate(g.createdAt)}</span> },
                {
                  header: '',
                  className: 'text-right',
                  cell: g => (
                    <span className="inline-flex gap-2" onClick={e => e.stopPropagation()}>
                      <Button variant="secondary" size="sm" onClick={() => setEditing(g)}>Edit</Button>
                      <Button variant="danger" size="sm" onClick={() => setDeleting(g)}>Delete</Button>
                    </span>
                  ),
                },
              ]}
            />
          )}
        </Loader>
      </Card>
      {editing && <GroupModal group={editing === 'new' ? null : editing} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); groups.reload(); }} />}
      {deleting && (
        <ConfirmDialog
          title="Delete group"
          message={<>Delete the group <strong>{deleting.name}</strong>? Its {deleting.members} member{deleting.members === 1 ? '' : 's'} lose the roles it grants.</>}
          confirmLabel="Delete"
          busy={action.busy}
          onConfirm={remove}
          onCancel={() => setDeleting(null)}
        />
      )}
    </>
  );
}

/** Create or edit a group. */
export function GroupModal({ group, onClose, onSaved }: { group: GroupModel | null; onClose: () => void; onSaved: (group: GroupModel) => void }) {
  const { api } = useAdmin();
  const save = useBusy();
  const roles = useLoad(() => api.get<RoleModel[]>('/api/admin/roles'), [api]);
  const [form, setForm] = useState<GroupInput>({ name: group?.name ?? '', description: group?.description ?? null, roles: group?.roles ?? [], attributes: group?.attributes ?? {} });

  const submit = async () => {
    const body: GroupInput = { ...form, name: form.name.trim(), description: form.description?.trim() || null };
    const saved = await save.run(() => (group ? api.put<GroupModel>(`/api/admin/groups/${group.id}`, body) : api.post<GroupModel>('/api/admin/groups', body)));
    if (saved) onSaved(saved);
  };

  return (
    <Modal title={group ? `Edit ${group.name}` : 'New group'} onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={save.busy} disabled={!form.name.trim()}>{group ? 'Save' : 'Create'}</Button></>}>
      <Errors messages={save.error} />
      <form onSubmit={e => { e.preventDefault(); void submit(); }} className="max-h-[70vh] space-y-4 overflow-y-auto pr-1">
        <Field id="g-name" label="Name" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required autoFocus />
        <TextArea id="g-description" label="Description" value={form.description ?? ''} onChange={e => setForm({ ...form, description: e.target.value })} />
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium text-slate-700">Roles granted to members</legend>
          <Loader state={roles}>
            {list => <CheckboxList id="g-roles" options={list.map(r => ({ value: r.name, label: r.name, hint: r.description ?? undefined }))} selected={form.roles} onChange={selected => setForm({ ...form, roles: selected })} empty="No roles defined." />}
          </Loader>
        </fieldset>
        <KeyValueEditor label="Attributes" value={form.attributes} onChange={attributes => setForm({ ...form, attributes })} />
      </form>
    </Modal>
  );
}
