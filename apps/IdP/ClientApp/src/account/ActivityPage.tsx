import { useCallback } from 'react';
import { Badge, Card, Code, Muted, PageHeader, Table, formatDate } from '../components/ui';
import { useLoad, type EventModel } from './api';
import { useAccount } from './context';
import { Loading } from './common';

/** The user's recent events: sign-ins, consents, account changes. */
export function ActivityPage() {
  const { api } = useAccount();
  const events = useLoad(useCallback(() => api.get<EventModel[]>('/api/account/events'), [api]));

  return (
    <>
      <PageHeader title="Activity" description="Your last 50 events: sign-ins, consents and changes to your account." />
      <Loading of={events}>
        {items => (
          <Card>
            <Table<EventModel>
              rows={items}
              keyOf={e => e.id}
              empty="No activity recorded yet."
              columns={[
                { header: 'When', cell: e => formatDate(e.timeStamp), className: 'whitespace-nowrap' },
                { header: 'Event', cell: e => <span className="font-medium text-slate-800">{e.name}</span> },
                { header: 'Result', cell: e => <ResultBadge type={e.eventType} /> },
                { header: 'Details', cell: e => e.message ? <span className="break-words">{e.message}</span> : <Muted>—</Muted>, className: 'min-w-48' },
                { header: 'IP address', cell: e => e.remoteIpAddress ? <Code>{e.remoteIpAddress}</Code> : <Muted>—</Muted>, className: 'whitespace-nowrap' },
                { header: 'Application', cell: e => e.clientId ? <Code>{e.clientId}</Code> : <Muted>—</Muted> },
              ]}
            />
          </Card>
        )}
      </Loading>
    </>
  );
}

function ResultBadge({ type }: { type: string }) {
  switch (type.toLowerCase()) {
    case 'success': return <Badge tone="green">Success</Badge>;
    case 'failure': return <Badge tone="red">Failure</Badge>;
    case 'error': return <Badge tone="red">Error</Badge>;
    case 'information': return <Badge tone="sky">Info</Badge>;
    default: return <Badge>{type}</Badge>;
  }
}
