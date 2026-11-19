import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { Badge, Button, Card, Code, ConfirmDialog, Field, PageHeader, Pagination, Select, Table, formatDate } from '../components/ui';
import { query, type Page } from '../api';
import { pageNumber, useAdmin, useBusy, useDebounced, useLoad, useQueryParams } from './hooks';
import { BusyAlerts, Dash, Loader } from './shared';
import { grantTypeLabels, type GrantModel } from './types';

const filterKeys = ['subjectId', 'clientId', 'type', 'sessionId', 'page'] as const;

export function SessionsPage() {
  const [filters, setFilters] = useQueryParams(filterKeys);
  const hasFilter = !!(filters.subjectId || filters.clientId || filters.sessionId);

  return (
    <>
      <PageHeader title="Sessions & tokens" description="The persisted grants: refresh and reference tokens, authorization codes, consents and device codes." />
      <Card>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Field id="f-subject" label="Subject id" value={filters.subjectId} onChange={e => setFilters({ subjectId: e.target.value, page: null })} placeholder="User id" />
          <Field id="f-client" label="Client id" value={filters.clientId} onChange={e => setFilters({ clientId: e.target.value, page: null })} />
          <Select id="f-type" label="Type" value={filters.type} onChange={e => setFilters({ type: e.target.value, page: null })}>
            <option value="">Any</option>
            {Object.entries(grantTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </Select>
          <Field id="f-session" label="Session id" value={filters.sessionId} onChange={e => setFilters({ sessionId: e.target.value, page: null })} />
        </div>
        <GrantsTable filters={{ subjectId: filters.subjectId, clientId: filters.clientId, type: filters.type, sessionId: filters.sessionId }} page={pageNumber(filters.page)} onPage={p => setFilters({ page: String(p) })} revokeAll={hasFilter} />
      </Card>
    </>
  );
}

export interface GrantFilters {
  subjectId?: string;
  clientId?: string;
  type?: string;
  sessionId?: string;
}

/** The grants matching the filters, with revocation of one or of all of them. Also used in the user's Sessions tab. */
export function GrantsTable({ filters, page, onPage, revokeAll, showSubject = true }: { filters: GrantFilters; page: number; onPage: (page: number) => void; revokeAll: boolean; showSubject?: boolean }) {
  const { api } = useAdmin();
  const navigate = useNavigate();
  // Debounced on the serialized filters: the object itself is new on every render.
  const debouncedKey = useDebounced(JSON.stringify(filters));
  const debounced = useMemo(() => JSON.parse(debouncedKey) as GrantFilters, [debouncedKey]);
  const pageSize = 25;
  const state = useLoad(() => api.get<Page<GrantModel>>(`/api/admin/grants${query({ ...debounced, page, pageSize })}`), [api, debounced, page]);
  const action = useBusy();
  const [revoking, setRevoking] = useState<GrantModel | 'all' | null>(null);

  const confirm = async () => {
    if (!revoking) return;
    const url = revoking === 'all'
      ? `/api/admin/grants${query({ subjectId: debounced.subjectId, clientId: debounced.clientId, sessionId: debounced.sessionId })}`
      : `/api/admin/grants/${encodeURIComponent(revoking.key)}`;
    const ok = await action.run(() => api.delete(url).then(() => true), revoking === 'all' ? 'All matching grants were revoked.' : 'The grant was revoked.');
    if (ok) { setRevoking(null); state.reload(); }
  };

  return (
    <div className="space-y-4">
      <BusyAlerts state={action} />
      {revokeAll && (
        <div className="flex justify-end">
          <Button variant="danger" size="sm" onClick={() => setRevoking('all')} disabled={!state.data || state.data.total === 0}>Revoke all matching</Button>
        </div>
      )}
      <Loader state={state}>
        {result => (
          <>
            <Table<GrantModel>
              rows={result.items}
              keyOf={g => g.key}
              empty="No grants match."
              columns={[
                { header: 'Type', cell: g => <Badge tone={g.type === 'refresh_token' ? 'indigo' : g.type === 'user_consent' ? 'sky' : 'slate'}>{grantTypeLabels[g.type] ?? g.type}</Badge> },
                ...(showSubject ? [{
                  header: 'User',
                  cell: (g: GrantModel) => g.subjectId
                    ? <button type="button" className="font-medium text-indigo-600 hover:text-indigo-500" onClick={() => navigate(`/users/${g.subjectId}`)}>{g.subjectName ?? g.subjectId}</button>
                    : <Dash />,
                }] : []),
                { header: 'Client', cell: g => <button type="button" className="font-medium text-indigo-600 hover:text-indigo-500" onClick={() => navigate(`/clients/${encodeURIComponent(g.clientId)}`)}>{g.clientName ?? g.clientId}</button> },
                { header: 'Session', cell: g => g.sessionId ? <Code>{g.sessionId}</Code> : <Dash /> },
                { header: 'Created', cell: g => <span className="whitespace-nowrap">{formatDate(g.creationTime)}</span> },
                { header: 'Expires', cell: g => <span className="whitespace-nowrap">{g.expiration ? formatDate(g.expiration) : 'Never'}</span> },
                { header: 'Consumed', cell: g => g.consumedTime ? <span className="whitespace-nowrap">{formatDate(g.consumedTime)}</span> : <Dash /> },
                { header: '', className: 'text-right', cell: g => <Button variant="danger" size="sm" onClick={() => setRevoking(g)}>Revoke</Button> },
              ]}
            />
            <Pagination page={result.pageNumber} pageSize={result.pageSize} total={result.total} onChange={onPage} />
          </>
        )}
      </Loader>
      {revoking && (
        <ConfirmDialog
          title={revoking === 'all' ? 'Revoke all matching grants' : 'Revoke grant'}
          message={revoking === 'all'
            ? <>Revoke every grant matching the current user, client and session filters ({state.data?.total ?? 0} in total)? The applications lose their tokens and consents.</>
            : <>Revoke this {(grantTypeLabels[revoking.type] ?? revoking.type).toLowerCase()} for {revoking.clientName ?? revoking.clientId}?</>}
          confirmLabel="Revoke"
          busy={action.busy}
          onConfirm={confirm}
          onCancel={() => setRevoking(null)}
        />
      )}
    </div>
  );
}
