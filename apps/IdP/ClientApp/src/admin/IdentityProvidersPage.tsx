import { useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { Badge, Button, Card, Code, ConfirmDialog, CopyButton, Field, ListField, Muted, PageHeader, Table, Toggle, formatDate } from '../components/ui';
import { useAdmin, useBusy, useLoad } from './hooks';
import { BusyAlerts, EnabledBadge, Loader } from './shared';
import type { IdentityProviderInput, IdentityProviderModel } from './types';

export function IdentityProvidersPage() {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const providers = useLoad(() => api.get<IdentityProviderModel[]>('/api/admin/identity-providers'), [api]);

  return (
    <>
      <PageHeader title="Identity providers" description="External OpenID Connect providers users can sign in with." actions={<Button onClick={() => navigate('/identity-providers/new')}>New provider</Button>} />
      <Card>
        <Loader state={providers}>
          {list => (
            <Table<IdentityProviderModel>
              rows={list}
              keyOf={p => p.alias}
              onRowClick={p => navigate(`/identity-providers/${encodeURIComponent(p.alias)}`)}
              empty="No external providers configured."
              columns={[
                { header: 'Provider', cell: p => <><span className="font-medium text-slate-800">{p.displayName}</span><span className="block"><Code>{p.alias}</Code></span></> },
                { header: 'Authority', className: 'max-w-xs', cell: p => <span className="break-all">{p.authority}</span> },
                {
                  header: 'Status',
                  cell: p => (
                    <span className="flex flex-wrap gap-1">
                      <EnabledBadge enabled={p.enabled} />
                      {!p.showOnLoginPage && <Badge>Hidden on login page</Badge>}
                      {p.autoProvision && <Badge tone="sky">Auto-provision</Badge>}
                      {!p.hasClientSecret && <Badge tone="amber">No secret</Badge>}
                    </span>
                  ),
                },
                { header: 'Linked users', cell: p => p.linkedUsers },
                { header: 'Order', cell: p => p.order },
              ]}
            />
          )}
        </Loader>
      </Card>
    </>
  );
}

function toInput(p: IdentityProviderModel | null): IdentityProviderInput {
  return {
    alias: p?.alias ?? '',
    displayName: p?.displayName ?? '',
    enabled: p?.enabled ?? true,
    authority: p?.authority ?? '',
    clientId: p?.clientId ?? '',
    clientSecret: '',
    scopes: p?.scopes ?? ['openid', 'profile', 'email'],
    getClaimsFromUserInfoEndpoint: p?.getClaimsFromUserInfoEndpoint ?? true,
    usePkce: p?.usePkce ?? true,
    autoProvision: p?.autoProvision ?? true,
    trustEmail: p?.trustEmail ?? false,
    showOnLoginPage: p?.showOnLoginPage ?? true,
    order: p?.order ?? 0,
  };
}

/** /identity-providers/new and /identity-providers/:alias. */
export function IdentityProviderEditPage() {
  const { api } = useAdmin();
  const { alias } = useParams();
  const isNew = alias === undefined;
  const loaded = useLoad(() => (isNew ? Promise.resolve(null) : api.get<IdentityProviderModel>(`/api/admin/identity-providers/${encodeURIComponent(alias)}`)), [api, alias]);

  if (!isNew) {
    return <Loader state={loaded}>{provider => <ProviderForm key={alias} provider={provider} onSaved={loaded.setData} />}</Loader>;
  }
  return <ProviderForm key="new" provider={null} onSaved={() => undefined} />;
}

function ProviderForm({ provider, onSaved }: { provider: IdentityProviderModel | null; onSaved: (provider: IdentityProviderModel) => void }) {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const isNew = provider === null;
  const [form, setForm] = useState(() => toInput(provider));
  const [deleting, setDeleting] = useState(false);
  const save = useBusy();
  const patch = (p: Partial<IdentityProviderInput>) => setForm(f => ({ ...f, ...p }));
  const aliasValid = /^[A-Za-z0-9_-]+$/.test(form.alias);
  const valid = aliasValid && form.displayName.trim() !== '' && form.authority.trim() !== '' && form.clientId.trim() !== '';
  const redirectUri = provider ? `${window.location.origin}${provider.callbackPath}` : null;

  const submit = async () => {
    if (isNew) {
      const created = await save.run(() => api.post<IdentityProviderModel>('/api/admin/identity-providers', form));
      if (created) navigate(`/identity-providers/${encodeURIComponent(created.alias)}`, { replace: true });
    } else {
      const saved = await save.run(() => api.put<IdentityProviderModel>(`/api/admin/identity-providers/${encodeURIComponent(provider.alias)}`, form), 'Provider saved.');
      if (saved) { onSaved(saved); patch({ clientSecret: '' }); }
    }
  };

  const remove = async () => {
    if (!provider) return;
    const ok = await save.run(() => api.delete(`/api/admin/identity-providers/${encodeURIComponent(provider.alias)}`).then(() => true));
    setDeleting(false);
    if (ok) navigate('/identity-providers');
  };

  return (
    <>
      <PageHeader
        title={isNew ? 'New identity provider' : provider.displayName}
        description={isNew ? 'Connect an external OpenID Connect provider.' : <span className="flex flex-wrap items-center gap-2"><Code>{provider.alias}</Code><EnabledBadge enabled={provider.enabled} /><span>{provider.linkedUsers} linked user{provider.linkedUsers === 1 ? '' : 's'}</span></span>}
        actions={
          <>
            <Button variant="secondary" onClick={() => navigate('/identity-providers')}>Back</Button>
            {!isNew && <Button variant="danger" onClick={() => setDeleting(true)}>Delete</Button>}
          </>
        }
      />
      <form onSubmit={e => { e.preventDefault(); void submit(); }} className="space-y-6">
        <Card title="Provider">
          <BusyAlerts state={save} />
          <Toggle label="Enabled" checked={form.enabled} onChange={enabled => patch({ enabled })} />
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="idp-alias" label="Alias" value={form.alias} onChange={e => patch({ alias: e.target.value })} readOnly={!isNew} required spellCheck={false} hint={isNew ? 'Letters, digits, - and _. It names the scheme and the callback path.' : 'The alias cannot change.'} error={form.alias && !aliasValid ? 'Only letters, digits, - and _.' : undefined} autoFocus={isNew} />
            <Field id="idp-displayName" label="Display name" value={form.displayName} onChange={e => patch({ displayName: e.target.value })} required hint="The button on the login page." />
          </div>
          <Field id="idp-authority" label="Authority" type="url" value={form.authority} onChange={e => patch({ authority: e.target.value })} required hint="The issuer URL; discovery is read from /.well-known/openid-configuration." placeholder="https://accounts.example.com" />
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="idp-clientId" label="Client id" value={form.clientId} onChange={e => patch({ clientId: e.target.value })} required spellCheck={false} autoComplete="off" />
            <Field id="idp-clientSecret" label="Client secret" type="password" value={form.clientSecret} onChange={e => patch({ clientSecret: e.target.value })} autoComplete="new-password" hint={isNew ? 'Optional for public clients with PKCE.' : provider.hasClientSecret ? 'A secret is stored. Leave empty to keep it.' : 'No secret stored.'} />
          </div>
          <ListField id="idp-scopes" label="Scopes" value={form.scopes} onChange={scopes => patch({ scopes })} hint="One per line; openid is always requested." />
          <Field id="idp-order" label="Order" type="number" value={form.order} onChange={e => patch({ order: Number(e.target.value) || 0 })} hint="Position on the login page; lower first." className="sm:w-40" />
        </Card>

        <Card title="Behaviour">
          <Toggle label="Use PKCE" hint="Recommended; the provider must support it." checked={form.usePkce} onChange={usePkce => patch({ usePkce })} />
          <Toggle label="Get claims from the userinfo endpoint" hint="For providers that put few claims in the identity token." checked={form.getClaimsFromUserInfoEndpoint} onChange={getClaimsFromUserInfoEndpoint => patch({ getClaimsFromUserInfoEndpoint })} />
          <Toggle label="Auto-provision users" hint="Create a local account on the first sign-in; otherwise only users that linked the provider can use it." checked={form.autoProvision} onChange={autoProvision => patch({ autoProvision })} />
          <Toggle label="Trust the e-mail" hint="Accept the provider's e-mail as verified, and link to an existing local account with that e-mail." checked={form.trustEmail} onChange={trustEmail => patch({ trustEmail })} />
          <Toggle label="Show on the login page" hint="Off: only reachable through a client restricted to this provider or an idp hint." checked={form.showOnLoginPage} onChange={showOnLoginPage => patch({ showOnLoginPage })} />
        </Card>

        <Card title="Registration at the provider">
          {redirectUri
            ? (
              <>
                <Muted>Register this redirect URI at the provider:</Muted>
                <div className="flex flex-wrap items-center gap-2 text-sm">
                  <Code className="break-all">{redirectUri}</Code>
                  <CopyButton value={redirectUri} />
                </div>
                {provider && <Muted>Callback path <Code>{provider.callbackPath}</Code> · created {formatDate(provider.createdAt)} · updated {formatDate(provider.updatedAt)}</Muted>}
              </>
            )
            : <Muted>The redirect URI to register at the provider is shown after the provider is created.</Muted>}
        </Card>

        <div className="flex justify-end">
          <Button type="submit" loading={save.busy} disabled={!valid}>{isNew ? 'Create provider' : 'Save changes'}</Button>
        </div>
      </form>

      {deleting && provider && (
        <ConfirmDialog
          title="Delete identity provider"
          message={<>Delete <strong>{provider.displayName}</strong>? {provider.linkedUsers > 0 ? <>{provider.linkedUsers} user{provider.linkedUsers === 1 ? '' : 's'} keep a link to it but can no longer sign in with it.</> : 'No users are linked to it.'}</>}
          confirmLabel="Delete"
          busy={save.busy}
          onConfirm={remove}
          onCancel={() => setDeleting(false)}
        />
      )}
    </>
  );
}

