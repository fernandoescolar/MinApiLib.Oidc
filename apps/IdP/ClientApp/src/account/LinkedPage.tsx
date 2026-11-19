import { useCallback, useState, type ReactNode } from 'react';
import { useSearchParams } from 'react-router';
import { Alert, Badge, Button, Card, ConfirmDialog, EmptyState, Muted, PageHeader, formatDate } from '../components/ui';
import { useAction, useLoad, type ExternalProviderModel } from './api';
import { useAccount } from './context';
import { Loading } from './common';

/** The external identity providers (Google, Microsoft...) the user can sign in with. */
export function LinkedPage() {
  const { api, profile } = useAccount();
  const providers = useLoad(useCallback(() => api.get<ExternalProviderModel[]>('/api/account/external'), [api]));
  const [params, setParams] = useSearchParams();
  const [unlinking, setUnlinking] = useState<ExternalProviderModel | null>(null);
  const callbackError = params.get('error');

  const unlink = useAction(useCallback(async (provider: ExternalProviderModel) => {
    await api.delete(`/api/account/external/${encodeURIComponent(provider.alias)}`);
    setUnlinking(null);
    providers.reload();
    profile.reload();
  }, [api, providers, profile]));

  return (
    <>
      <PageHeader title="Linked accounts" description="Sign in with an account you already have elsewhere." />
      <div className="mb-4 space-y-3">
        {callbackError && <Alert kind="error" onClose={() => setParams({}, { replace: true })}>{callbackError}</Alert>}
        {unlink.error && <Alert kind="error" onClose={unlink.clearError}>{unlink.error}</Alert>}
      </div>
      <Loading of={providers}>
        {items => items.length === 0 ? (
          <EmptyState title="No external providers">This server has no external identity providers to link.</EmptyState>
        ) : (
          <div className="grid gap-6 lg:grid-cols-2">
            <ProviderList
              title="Linked"
              description="Accounts you can already sign in with."
              items={items.filter(p => p.linked)}
              empty="None linked yet."
              action={p => <Button variant="danger" size="sm" onClick={() => setUnlinking(p)}>Unlink</Button>}
            />
            <ProviderList
              title="Available"
              description="Link one to sign in with it. You will be sent to the provider and back here."
              items={items.filter(p => !p.linked)}
              empty="Every provider is linked."
              action={p => <Button variant="secondary" size="sm" onClick={() => window.location.assign(p.linkUrl)}>Link</Button>}
            />
          </div>
        )}
      </Loading>
      {unlinking && (
        <ConfirmDialog
          title={`Unlink ${unlinking.displayName}?`}
          message="You will no longer be able to sign in with that account. You can link it again later."
          confirmLabel="Unlink"
          busy={unlink.busy}
          onCancel={() => setUnlinking(null)}
          onConfirm={() => void unlink.run(unlinking)}
        />
      )}
    </>
  );
}

function ProviderList({ title, description, items, empty, action }: { title: string; description: string; items: ExternalProviderModel[]; empty: string; action: (provider: ExternalProviderModel) => ReactNode }) {
  return (
    <Card title={title} description={description}>
      {items.length === 0 ? <Muted>{empty}</Muted> : (
        <ul className="divide-y divide-slate-100">
          {items.map(p => (
            <li key={p.alias} className="flex flex-wrap items-center justify-between gap-3 py-3">
              <div className="min-w-0">
                <p className="flex items-center gap-2 font-medium text-slate-800">
                  {p.displayName}
                  {p.linked && <Badge tone="green">Linked</Badge>}
                </p>
                {p.linked ? (
                  <p className="truncate text-xs text-slate-500">Linked {formatDate(p.linkedAt)}{p.providerKey && <> · ID {p.providerKey}</>}</p>
                ) : (
                  <p className="text-xs text-slate-500">Not linked.</p>
                )}
              </div>
              {action(p)}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
