import { useState } from 'react';
import { Badge, Button, Card, Checkbox, Code, ConfirmDialog, Field, ListField, Modal, PageHeader, Table, Tabs, TextArea } from '../components/ui';
import { useAdmin, useBusy, useLoad } from './hooks';
import { BusyAlerts, CheckboxList, Dash, Errors, KeyValueEditor, Loader, SecretsPanel } from './shared';
import type { ApiResourceModel, NewSecretResult, ResourceModel } from './types';

type Kind = 'identity-resources' | 'api-scopes' | 'api-resources';

const kinds: { id: Kind; label: string; title: string; description: string }[] = [
  { id: 'identity-resources', label: 'Identity resources', title: 'Identity resources', description: 'Groups of user claims a client can request as scopes (openid, profile, email...).' },
  { id: 'api-scopes', label: 'API scopes', title: 'API scopes', description: 'What a client may do at an API; they end up in the scope claim of access tokens.' },
  { id: 'api-resources', label: 'API resources', title: 'API resources', description: 'APIs as token audiences, grouping scopes; with secrets for token introspection.' },
];

function emptyResource(): ApiResourceModel {
  return { name: '', displayName: null, description: null, enabled: true, required: false, emphasize: false, showInDiscoveryDocument: true, userClaims: [], properties: {}, scopes: [], allowedAccessTokenSigningAlgorithms: [], requireResourceIndicator: false, secrets: [] };
}

export function ScopesPage() {
  const [kind, setKind] = useState<Kind>('identity-resources');
  const meta = kinds.find(k => k.id === kind)!;
  return (
    <>
      <PageHeader title="Scopes & resources" description="What clients can ask for, and the APIs that accept the tokens." />
      <Card>
        <Tabs tabs={kinds} active={kind} onChange={id => setKind(id as Kind)} />
        <ResourcesTab key={kind} kind={kind} title={meta.title} description={meta.description} />
      </Card>
    </>
  );
}

function ResourcesTab({ kind, title, description }: { kind: Kind; title: string; description: string }) {
  const { api } = useAdmin();
  const isApiResource = kind === 'api-resources';
  const list = useLoad(() => api.get<ApiResourceModel[]>(`/api/admin/${kind}`), [api, kind]);
  const action = useBusy();
  const [editing, setEditing] = useState<{ resource: ApiResourceModel; isNew: boolean } | null>(null);
  const [deleting, setDeleting] = useState<ApiResourceModel | null>(null);
  const [secretsOf, setSecretsOf] = useState<string | null>(null);

  const remove = async () => {
    if (!deleting) return;
    const ok = await action.run(() => api.delete(`/api/admin/${kind}/${encodeURIComponent(deleting.name)}`).then(() => true), `${deleting.name} deleted.`);
    setDeleting(null);
    if (ok) list.reload();
  };

  const secretsResource = secretsOf ? list.data?.find(r => r.name === secretsOf) ?? null : null;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-medium text-slate-900">{title}</h2>
          <p className="text-sm text-slate-500">{description}</p>
        </div>
        <Button onClick={() => { action.clear(); setEditing({ resource: emptyResource(), isNew: true }); }}>New</Button>
      </div>
      <BusyAlerts state={action} />
      <Loader state={list}>
        {rows => (
          <Table<ApiResourceModel>
            rows={rows}
            keyOf={r => r.name}
            empty="Nothing defined yet."
            columns={[
              { header: 'Name', cell: r => <><span className="font-medium text-slate-800">{r.displayName ?? r.name}</span><span className="block"><Code>{r.name}</Code></span></> },
              { header: 'Description', className: 'max-w-xs', cell: r => r.description ?? <Dash /> },
              {
                header: isApiResource ? 'Scopes' : 'User claims',
                cell: r => {
                  const items = isApiResource ? r.scopes : r.userClaims;
                  return items.length === 0 ? <Dash /> : <span className="flex flex-wrap gap-1">{items.map(c => <Badge key={c}>{c}</Badge>)}</span>;
                },
              },
              {
                header: 'Flags',
                cell: r => (
                  <span className="flex flex-wrap gap-1">
                    {!r.enabled && <Badge tone="red">Disabled</Badge>}
                    {r.required && <Badge tone="indigo">Required</Badge>}
                    {r.emphasize && <Badge tone="amber">Emphasized</Badge>}
                    {!r.showInDiscoveryDocument && <Badge>Hidden from discovery</Badge>}
                    {isApiResource && r.requireResourceIndicator && <Badge tone="sky">Resource indicator</Badge>}
                    {isApiResource && <Badge>{r.secrets.length} secret{r.secrets.length === 1 ? '' : 's'}</Badge>}
                  </span>
                ),
              },
              {
                header: '',
                className: 'text-right',
                cell: r => (
                  <span className="inline-flex gap-2">
                    {isApiResource && <Button variant="secondary" size="sm" onClick={() => setSecretsOf(r.name)}>Secrets</Button>}
                    <Button variant="secondary" size="sm" onClick={() => { action.clear(); setEditing({ resource: r, isNew: false }); }}>Edit</Button>
                    <Button variant="danger" size="sm" disabled={kind === 'identity-resources' && r.name === 'openid'} onClick={() => setDeleting(r)}>Delete</Button>
                  </span>
                ),
              },
            ]}
          />
        )}
      </Loader>

      {editing && (
        <ResourceModal
          kind={kind}
          resource={editing.resource}
          isNew={editing.isNew}
          onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); list.reload(); }}
        />
      )}
      {deleting && (
        <ConfirmDialog
          title={`Delete ${deleting.name}`}
          message={<>Delete <Code>{deleting.name}</Code>? Clients that still list it fail to request it.</>}
          confirmLabel="Delete"
          busy={action.busy}
          onConfirm={remove}
          onCancel={() => setDeleting(null)}
        />
      )}
      {secretsResource && (
        <Modal title={`Secrets of ${secretsResource.name}`} onClose={() => setSecretsOf(null)} footer={<Button variant="secondary" onClick={() => setSecretsOf(null)}>Close</Button>}>
          <SecretsPanel
            secrets={secretsResource.secrets}
            onAdd={body => api.post<NewSecretResult>(`/api/admin/api-resources/${encodeURIComponent(secretsResource.name)}/secrets`, body)}
            onRemove={id => api.delete(`/api/admin/api-resources/${encodeURIComponent(secretsResource.name)}/secrets/${id}`)}
            onChanged={list.reload}
          />
        </Modal>
      )}
    </div>
  );
}

function ResourceModal({ kind, resource, isNew, onClose, onSaved }: { kind: Kind; resource: ApiResourceModel; isNew: boolean; onClose: () => void; onSaved: () => void }) {
  const { api } = useAdmin();
  const isApiResource = kind === 'api-resources';
  const [form, setForm] = useState(resource);
  const save = useBusy();
  const apiScopes = useLoad(() => (isApiResource ? api.get<ResourceModel[]>('/api/admin/api-scopes') : Promise.resolve<ResourceModel[]>([])), [api, isApiResource]);
  const patch = (p: Partial<ApiResourceModel>) => setForm(f => ({ ...f, ...p }));
  const text = (value: string) => (value.trim() === '' ? null : value);
  const name = form.name.trim();
  const nameValid = name.length > 0 && name.length <= 200 && !/\s/.test(name);

  const submit = async () => {
    const { secrets: _secrets, scopes, allowedAccessTokenSigningAlgorithms, requireResourceIndicator, ...base } = form;
    const body = isApiResource ? { ...base, scopes, allowedAccessTokenSigningAlgorithms, requireResourceIndicator } : base;
    const ok = await save.run(() => api.put(`/api/admin/${kind}/${encodeURIComponent(name)}`, { ...body, name }).then(() => true));
    if (ok) onSaved();
  };

  return (
    <Modal title={isNew ? 'New' : `Edit ${resource.name}`} onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={save.busy} disabled={!nameValid}>{isNew ? 'Create' : 'Save'}</Button></>}>
      <Errors messages={save.error} />
      <form onSubmit={e => { e.preventDefault(); void submit(); }} className="max-h-[70vh] space-y-4 overflow-y-auto pr-1">
        <Field id="res-name" label="Name" value={form.name} onChange={e => patch({ name: e.target.value })} readOnly={!isNew} required spellCheck={false} hint={isNew ? 'The scope value; no spaces.' : 'The name cannot change.'} autoFocus={isNew} />
        <Field id="res-displayName" label="Display name" value={form.displayName ?? ''} onChange={e => patch({ displayName: text(e.target.value) })} hint="Shown on the consent page." />
        <TextArea id="res-description" label="Description" value={form.description ?? ''} onChange={e => patch({ description: text(e.target.value) })} />
        <div className="grid gap-2 sm:grid-cols-2">
          <Checkbox id="res-enabled" label="Enabled" checked={form.enabled} onChange={e => patch({ enabled: e.target.checked })} />
          <Checkbox id="res-discovery" label="Show in discovery document" checked={form.showInDiscoveryDocument} onChange={e => patch({ showInDiscoveryDocument: e.target.checked })} />
          <Checkbox id="res-required" label="Required" hint="The user cannot uncheck it on the consent page." checked={form.required} onChange={e => patch({ required: e.target.checked })} />
          <Checkbox id="res-emphasize" label="Emphasize" hint="Highlighted on the consent page." checked={form.emphasize} onChange={e => patch({ emphasize: e.target.checked })} />
          {isApiResource && <Checkbox id="res-indicator" label="Require resource indicator" hint="Tokens for it must name it with the resource parameter (RFC 8707)." checked={form.requireResourceIndicator} onChange={e => patch({ requireResourceIndicator: e.target.checked })} />}
        </div>
        {isApiResource && (
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium text-slate-700">Scopes</legend>
            <Loader state={apiScopes}>
              {scopes => <CheckboxList id="res-scopes" options={scopes.map(s => ({ value: s.name, label: s.displayName ?? s.name, hint: s.name }))} selected={form.scopes} onChange={selected => patch({ scopes: selected })} empty="No API scopes defined yet." />}
            </Loader>
          </fieldset>
        )}
        <ListField id="res-claims" label="User claims" value={form.userClaims} onChange={userClaims => patch({ userClaims })} hint="Claim types included in the tokens. One per line." />
        {isApiResource && <ListField id="res-algs" label="Allowed access token signing algorithms" value={form.allowedAccessTokenSigningAlgorithms} onChange={allowedAccessTokenSigningAlgorithms => patch({ allowedAccessTokenSigningAlgorithms })} hint="Empty: the server default. One per line." />}
        <KeyValueEditor label="Properties" value={form.properties} onChange={properties => patch({ properties })} />
      </form>
    </Modal>
  );
}
