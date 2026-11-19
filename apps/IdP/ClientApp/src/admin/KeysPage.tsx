import { Badge, Card, Code, CopyButton, PageHeader, Table, formatDate } from '../components/ui';
import { useAdmin, useLoad } from './hooks';
import { Loader } from './shared';
import type { KeysModel } from './types';

export function KeysPage() {
  const { api } = useAdmin();
  const state = useLoad(() => api.get<KeysModel>('/api/admin/keys'), [api]);

  return (
    <>
      <PageHeader title="Signing keys" description="The keys of automatic key management. Their public parts are published at the JWKS endpoint." />
      <Loader state={state}>
        {({ jwksUri, keys }) => (
          <div className="space-y-6">
            <Card title="JWKS">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <a href={jwksUri} target="_blank" rel="noreferrer" className="min-w-0 break-all font-medium text-indigo-600 hover:text-indigo-500">{jwksUri}</a>
                <CopyButton value={jwksUri} />
              </div>
            </Card>
            <Card title="Keys" description="Newest first. The newest key signs; the older ones stay published until their retention ends.">
              <Table<KeysModel['keys'][number]>
                rows={keys}
                keyOf={k => k.id}
                empty="No keys yet. The first one is created on the first token."
                columns={[
                  { header: 'Key id', cell: k => <Code>{k.id}</Code> },
                  { header: 'Algorithm', cell: k => <Badge tone="indigo">{k.algorithm}</Badge> },
                  { header: 'Created', cell: k => formatDate(k.created) },
                  { header: 'Protected at rest', cell: k => k.dataProtected ? <Badge tone="green">Yes</Badge> : <Badge tone="amber">No</Badge> },
                  { header: 'Version', cell: k => k.version },
                ]}
              />
            </Card>
          </div>
        )}
      </Loader>
    </>
  );
}
