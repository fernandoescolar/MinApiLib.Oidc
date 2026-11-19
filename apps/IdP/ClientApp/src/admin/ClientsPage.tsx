import { useNavigate } from 'react-router';
import { Badge, Button, Card, Code, Field, PageHeader, Pagination, Table } from '../components/ui';
import { query, type Page } from '../api';
import { pageNumber, useAdmin, useDebounced, useLoad, useQueryParams } from './hooks';
import { Dash, Loader } from './shared';
import { grantTypes, type ClientSummary } from './types';

const grantTypeShort: Record<string, string> = Object.fromEntries(grantTypes.map(g => [g.value, g.label]));

export function ClientsPage() {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const [params, setParams] = useQueryParams(['q', 'page'] as const);
  const q = useDebounced(params.q);
  const page = pageNumber(params.page);
  const clients = useLoad(() => api.get<Page<ClientSummary>>(`/api/admin/clients${query({ q, page, pageSize: 25 })}`), [api, q, page]);

  return (
    <>
      <PageHeader title="Clients" description="The applications that sign users in or call APIs through this server." actions={<Button onClick={() => navigate('/clients/new')}>New client</Button>} />
      <Card>
        <Field id="clients-q" label="Search" type="search" placeholder="Client id or name" value={params.q} onChange={e => setParams({ q: e.target.value, page: null })} />
        <Loader state={clients}>
          {result => (
            <>
              <Table<ClientSummary>
                rows={result.items}
                keyOf={c => c.clientId}
                onRowClick={c => navigate(`/clients/${encodeURIComponent(c.clientId)}`)}
                empty={q ? 'No clients match.' : 'No clients yet.'}
                columns={[
                  {
                    header: 'Client',
                    cell: c => (
                      <div className="flex items-center gap-3">
                        {c.logoUri && <img src={c.logoUri} alt="" className="size-8 rounded object-contain" />}
                        <div>
                          <span className="font-medium text-slate-800">{c.clientName ?? c.clientId}</span>
                          <span className="block"><Code>{c.clientId}</Code></span>
                        </div>
                      </div>
                    ),
                  },
                  { header: 'Description', className: 'max-w-xs', cell: c => c.description ? <span className="line-clamp-2">{c.description}</span> : <Dash /> },
                  { header: 'Grant types', cell: c => <span className="flex flex-wrap gap-1">{c.allowedGrantTypes.map(g => <Badge key={g} tone="indigo">{grantTypeShort[g] ?? g}</Badge>)}</span> },
                  {
                    header: 'Flags',
                    cell: c => (
                      <span className="flex flex-wrap gap-1">
                        {!c.enabled && <Badge tone="red">Disabled</Badge>}
                        {c.requireConsent && <Badge tone="sky">Consent</Badge>}
                        {c.requireDPoP && <Badge tone="amber">DPoP</Badge>}
                        {c.secrets === 0 ? <Badge>Public</Badge> : <Badge>{c.secrets} secret{c.secrets === 1 ? '' : 's'}</Badge>}
                      </span>
                    ),
                  },
                ]}
              />
              <Pagination page={result.pageNumber} pageSize={result.pageSize} total={result.total} onChange={p => setParams({ page: String(p) })} />
            </>
          )}
        </Loader>
      </Card>
    </>
  );
}
