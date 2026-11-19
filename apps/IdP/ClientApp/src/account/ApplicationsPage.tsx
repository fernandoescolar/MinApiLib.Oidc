import { useCallback, useState } from 'react';
import { Alert, Badge, Button, Card, ConfirmDialog, EmptyState, Link, Muted, PageHeader, formatDate } from '../components/ui';
import { useAction, useLoad, type ApplicationModel } from './api';
import { useAccount } from './context';
import { Loading } from './common';

/** The applications the user granted access to: remembered consents and refresh tokens. */
export function ApplicationsPage() {
  const { api } = useAccount();
  const apps = useLoad(useCallback(() => api.get<ApplicationModel[]>('/api/account/applications'), [api]));
  const [revoking, setRevoking] = useState<ApplicationModel | null>(null);

  const revoke = useAction(useCallback(async (app: ApplicationModel) => {
    await api.delete(`/api/account/applications/${encodeURIComponent(app.clientId)}`);
    setRevoking(null);
    apps.reload();
  }, [api, apps]));

  return (
    <>
      <PageHeader title="Applications" description="The applications you allowed to access your account, and what they can see." />
      {revoke.error && <div className="mb-4"><Alert kind="error" onClose={revoke.clearError}>{revoke.error}</Alert></div>}
      <Loading of={apps}>
        {items => items.length === 0 ? (
          <EmptyState title="No applications have access">When you sign in to an application and allow it to use your account, it appears here.</EmptyState>
        ) : (
          <div className="grid gap-4 lg:grid-cols-2">
            {items.map(app => (
              <Card
                key={app.clientId}
                title={app.clientName}
                description={app.description ?? undefined}
                actions={<Button variant="danger" size="sm" onClick={() => setRevoking(app)}>Revoke access</Button>}
              >
                <div className="flex items-start gap-3">
                  {app.logoUri && <img src={app.logoUri} alt="" className="size-10 shrink-0 rounded-md object-contain" />}
                  <div className="min-w-0 flex-1 space-y-2 text-sm">
                    {app.clientUri && <Link href={app.clientUri} className="block truncate">{app.clientUri}</Link>}
                    <div>
                      <p className="mb-1 text-xs uppercase tracking-wide text-slate-500">Permissions</p>
                      {app.scopes.length === 0 ? <Muted>None.</Muted> : <div className="flex flex-wrap gap-1">{app.scopes.map(s => <Badge key={s} tone="indigo">{s}</Badge>)}</div>}
                    </div>
                    <p className="text-xs text-slate-500">
                      Granted {formatDate(app.creationTime)}{app.expiration && <> · expires {formatDate(app.expiration)}</>}
                    </p>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}
      </Loading>
      {revoking && (
        <ConfirmDialog
          title={`Revoke access for ${revoking.clientName}?`}
          message="The application loses the access you granted and its tokens stop working. It will ask you again the next time you use it."
          confirmLabel="Revoke"
          busy={revoke.busy}
          onCancel={() => setRevoking(null)}
          onConfirm={() => void revoke.run(revoking)}
        />
      )}
    </>
  );
}
