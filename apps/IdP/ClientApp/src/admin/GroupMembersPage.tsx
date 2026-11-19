import { useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { Badge, Button, Card, ConfirmDialog, DescriptionList, Field, Muted, PageHeader, Table, formatDate } from '../components/ui';
import { query, type Page } from '../api';
import { useAdmin, useBusy, useDebounced, useLoad } from './hooks';
import { BusyAlerts, Dash, Loader } from './shared';
import { GroupModal } from './GroupsPage';
import type { GroupModel, UserSummary } from './types';

interface GroupDetail {
  group: GroupModel;
  members: UserSummary[];
}

export function GroupMembersPage() {
  const { api } = useAdmin();
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const detail = useLoad(() => api.get<GroupDetail>(`/api/admin/groups/${id}`), [api, id]);
  const action = useBusy();
  const [editing, setEditing] = useState(false);
  const [removing, setRemoving] = useState<UserSummary | null>(null);

  const remove = async () => {
    if (!removing) return;
    const ok = await action.run(() => api.delete(`/api/admin/groups/${id}/members/${removing.id}`).then(() => true), `${removing.userName} removed from the group.`);
    setRemoving(null);
    if (ok) detail.reload();
  };

  return (
    <Loader state={detail}>
      {({ group, members }) => (
        <>
          <PageHeader
            title={group.name}
            description={group.description ?? 'Group'}
            actions={
              <>
                <Button variant="secondary" onClick={() => navigate('/groups')}>Back</Button>
                <Button variant="secondary" onClick={() => setEditing(true)}>Edit group</Button>
              </>
            }
          />
          <div className="space-y-6">
            <Card title="Group">
              <DescriptionList items={[
                { label: 'Roles granted', value: group.roles.length === 0 ? null : <span className="flex flex-wrap gap-1">{group.roles.map(r => <Badge key={r} tone="indigo">{r}</Badge>)}</span> },
                { label: 'Created', value: formatDate(group.createdAt) },
                { label: 'Attributes', value: Object.keys(group.attributes).length === 0 ? null : <span className="flex flex-wrap gap-1">{Object.entries(group.attributes).map(([k, v]) => <Badge key={k}>{k} = {v}</Badge>)}</span> },
                { label: 'Members', value: group.members },
              ]} />
            </Card>

            <Card title="Members" description={members.length >= 500 ? 'Showing the first 500 members.' : undefined}>
              <BusyAlerts state={action} />
              <AddMember groupId={id} members={members} onAdded={detail.reload} />
              <Table<UserSummary>
                rows={members}
                keyOf={u => u.id}
                onRowClick={u => navigate(`/users/${u.id}`)}
                empty="No members yet."
                columns={[
                  { header: 'User', cell: u => <><span className="font-medium text-slate-800">{u.userName}</span>{u.displayName !== u.userName && <span className="block text-xs text-slate-500">{u.displayName}</span>}</> },
                  { header: 'E-mail', cell: u => u.email ?? <Dash /> },
                  { header: 'Status', cell: u => u.enabled ? <Badge tone="green">Enabled</Badge> : <Badge tone="red">Disabled</Badge> },
                  { header: '', className: 'text-right', cell: u => <span onClick={e => e.stopPropagation()}><Button variant="danger" size="sm" onClick={() => setRemoving(u)}>Remove</Button></span> },
                ]}
              />
            </Card>
          </div>

          {editing && <GroupModal group={group} onClose={() => setEditing(false)} onSaved={() => { setEditing(false); detail.reload(); }} />}
          {removing && (
            <ConfirmDialog
              title="Remove member"
              message={<>Remove <strong>{removing.userName}</strong> from {group.name}? They lose the roles the group grants.</>}
              confirmLabel="Remove"
              busy={action.busy}
              onConfirm={remove}
              onCancel={() => setRemoving(null)}
            />
          )}
        </>
      )}
    </Loader>
  );
}

/** Finds users by name or e-mail and adds them to the group. */
function AddMember({ groupId, members, onAdded }: { groupId: string; members: UserSummary[]; onAdded: () => void }) {
  const { api } = useAdmin();
  const [text, setText] = useState('');
  const q = useDebounced(text.trim());
  const results = useLoad(() => (q ? api.get<Page<UserSummary>>(`/api/admin/users${query({ q, pageSize: 10 })}`) : Promise.resolve(null)), [api, q]);
  const action = useBusy();
  const memberIds = new Set(members.map(m => m.id));

  const add = async (user: UserSummary) => {
    const ok = await action.run(() => api.post(`/api/admin/groups/${groupId}/members`, { userId: user.id }).then(() => true), `${user.userName} added.`);
    if (ok) { setText(''); onAdded(); }
  };

  return (
    <div className="space-y-2 rounded-lg border border-slate-200 bg-slate-50 p-4">
      <Field id="add-member" label="Add a member" type="search" placeholder="Search users by name or e-mail" value={text} onChange={e => setText(e.target.value)} autoComplete="off" />
      <BusyAlerts state={action} />
      {results.error && <Muted>Search failed: {results.error.join(' ')}</Muted>}
      {q && results.data && (
        results.data.items.length === 0
          ? <Muted>No users match.</Muted>
          : (
            <ul className="divide-y divide-slate-200 rounded-lg border border-slate-200 bg-white">
              {results.data.items.map(u => (
                <li key={u.id} className="flex items-center justify-between gap-3 px-3 py-2 text-sm">
                  <span><span className="font-medium text-slate-800">{u.userName}</span>{u.email && <span className="ml-2 text-slate-500">{u.email}</span>}</span>
                  {memberIds.has(u.id) ? <Badge tone="green">Member</Badge> : <Button size="sm" variant="secondary" onClick={() => add(u)} disabled={action.busy}>Add</Button>}
                </li>
              ))}
            </ul>
          )
      )}
    </div>
  );
}
