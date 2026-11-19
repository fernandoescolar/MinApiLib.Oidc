import { useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { Badge, Button, Card, Code, ConfirmDialog, PageHeader, Tabs } from '../components/ui';
import { useAdmin, useBusy, useLoad } from './hooks';
import { BusyAlerts, Loader, SecretRevealModal, SecretsPanel } from './shared';
import { ClaimsTab, FlowsTab, GeneralTab, ScopesTab, TokensTab, UrisTab, grantTypeProblems, type Patch } from './ClientFormTabs';
import { newClient, type ClientDetail, type ClientModel, type CreatedClient, type NewSecretResult, type SecretModel } from './types';

const tabs = [
  { id: 'general', label: 'General' },
  { id: 'flows', label: 'Flows' },
  { id: 'uris', label: 'URIs' },
  { id: 'scopes', label: 'Scopes' },
  { id: 'tokens', label: 'Tokens' },
  { id: 'claims', label: 'Claims & properties' },
  { id: 'secrets', label: 'Secrets' },
];

function split(detail: ClientDetail): { client: ClientModel; secrets: SecretModel[] } {
  const { secrets, ...client } = detail;
  return { client, secrets };
}

/** /clients/new and /clients/:clientId. */
export function ClientEditPage() {
  const { api } = useAdmin();
  const { clientId } = useParams();
  const isNew = clientId === undefined;
  const loaded = useLoad(
    async () => (isNew ? { client: newClient(), secrets: [] } : split(await api.get<ClientDetail>(`/api/admin/clients/${encodeURIComponent(clientId)}`))),
    [api, clientId],
  );

  return (
    <Loader state={loaded}>
      {({ client, secrets }) => <ClientForm key={clientId ?? 'new'} initial={client} secrets={secrets} isNew={isNew} reload={loaded.reload} />}
    </Loader>
  );
}

function ClientForm({ initial, secrets, isNew, reload }: { initial: ClientModel; secrets: SecretModel[]; isNew: boolean; reload: () => void }) {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const [client, setClient] = useState(initial);
  const [tab, setTab] = useState('general');
  const [deleting, setDeleting] = useState(false);
  const [created, setCreated] = useState<CreatedClient | null>(null);
  const save = useBusy();
  const patch: Patch = p => setClient(c => ({ ...c, ...p }));
  const problems = grantTypeProblems(client.allowedGrantTypes);
  const url = `/api/admin/clients/${encodeURIComponent(initial.clientId)}`;

  const submit = async () => {
    if (isNew) {
      const result = await save.run(() => api.post<CreatedClient>('/api/admin/clients', client));
      if (result) setCreated(result);
    } else {
      const result = await save.run(() => api.put<ClientDetail>(url, client), 'Client saved.');
      if (result) setClient(split(result).client);
    }
  };

  const remove = async () => {
    const ok = await save.run(() => api.delete(url).then(() => true));
    setDeleting(false);
    if (ok) navigate('/clients');
  };

  const finishCreate = () => {
    if (created) navigate(`/clients/${encodeURIComponent(created.clientId)}`, { replace: true });
  };

  return (
    <>
      <PageHeader
        title={isNew ? 'New client' : (initial.clientName ?? initial.clientId)}
        description={isNew ? 'Register an application.' : <span className="flex flex-wrap items-center gap-2"><Code>{initial.clientId}</Code>{!client.enabled && <Badge tone="red">Disabled</Badge>}</span>}
        actions={
          <>
            <Button variant="secondary" onClick={() => navigate('/clients')}>Back</Button>
            {!isNew && <Button variant="danger" onClick={() => setDeleting(true)}>Delete</Button>}
          </>
        }
      />
      <form onSubmit={e => { e.preventDefault(); void submit(); }} className="space-y-4">
        <Card>
          <Tabs tabs={isNew ? tabs.filter(t => t.id !== 'secrets') : tabs} active={tab} onChange={setTab} />
          <div className="pt-2">
            {tab === 'general' && <GeneralTab client={client} patch={patch} isNew={isNew} />}
            {tab === 'flows' && <FlowsTab client={client} patch={patch} isNew={isNew} />}
            {tab === 'uris' && <UrisTab client={client} patch={patch} isNew={isNew} />}
            {tab === 'scopes' && <ScopesTab client={client} patch={patch} isNew={isNew} />}
            {tab === 'tokens' && <TokensTab client={client} patch={patch} isNew={isNew} />}
            {tab === 'claims' && <ClaimsTab client={client} patch={patch} isNew={isNew} />}
            {tab === 'secrets' && (
              <SecretsPanel
                secrets={secrets}
                onAdd={body => api.post<NewSecretResult>(`${url}/secrets`, body)}
                onRemove={id => api.delete(`${url}/secrets/${id}`)}
                onChanged={reload}
              />
            )}
          </div>
        </Card>
        {tab !== 'secrets' && (
          <Card>
            <BusyAlerts state={save} />
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-xs text-slate-500">{isNew && client.requireClientSecret && !client.allowedGrantTypes.every(g => g === 'implicit') ? 'A secret is generated and shown once after creation.' : 'The server validates the configuration on save.'}</p>
              <Button type="submit" loading={save.busy} disabled={!client.clientId.trim() || problems.length > 0}>{isNew ? 'Create client' : 'Save changes'}</Button>
            </div>
          </Card>
        )}
      </form>

      {created && (created.clientSecret
        ? (
          <SecretRevealModal title="Client created" value={created.clientSecret} onClose={finishCreate}>
            <p className="text-sm text-slate-600">The client <Code>{created.clientId}</Code> is registered. Its secret:</p>
          </SecretRevealModal>
        )
        : <ConfirmDialog title="Client created" message={<>The public client <Code>{created.clientId}</Code> is registered.</>} confirmLabel="Open" danger={false} onConfirm={finishCreate} onCancel={finishCreate} />)}

      {deleting && (
        <ConfirmDialog
          title="Delete client"
          message={<>Delete <strong>{initial.clientName ?? initial.clientId}</strong>? Its tokens and consents are revoked and the application stops working.</>}
          confirmLabel="Delete"
          busy={save.busy}
          onConfirm={remove}
          onCancel={() => setDeleting(false)}
        />
      )}
    </>
  );
}
