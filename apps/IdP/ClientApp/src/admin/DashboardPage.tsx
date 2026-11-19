import { useNavigate } from 'react-router';
import { Badge, Button, Card, Code, CopyButton, DescriptionList, PageHeader, Stat } from '../components/ui';
import { useAdmin, useLoad } from './hooks';
import { EventsTable, Loader } from './shared';
import type { Dashboard } from './types';

export function DashboardPage() {
  const { api } = useAdmin();
  const navigate = useNavigate();
  const state = useLoad(() => api.get<Dashboard>('/api/admin/dashboard'), [api]);

  return (
    <>
      <PageHeader title="Dashboard" description="What the server is, and what happened lately." actions={<Button variant="secondary" size="sm" onClick={state.reload} loading={state.loading && state.data !== null}>Refresh</Button>} />
      <Loader state={state}>
        {({ server, counts, signInsByDay, recentEvents }) => (
          <div className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <Stat label="Users" value={counts.users} hint={<>{counts.newUsersThisWeek} new this week · {counts.disabledUsers} disabled</>} />
              <Stat label="Clients" value={counts.clients} hint={<>{counts.disabledClients} disabled</>} />
              <Stat label="Identity providers" value={counts.identityProviders} hint="Enabled" />
              <Stat label="Active grants" value={counts.activeGrants} hint={<>{counts.refreshTokens} refresh tokens</>} />
              <Stat label="Events, last 24 h" value={counts.eventsLast24h.successes + counts.eventsLast24h.failures} hint={<><span className="text-emerald-700">{counts.eventsLast24h.successes} successes</span> · <span className="text-red-700">{counts.eventsLast24h.failures} failures</span></>} />
            </div>

            <div className="grid gap-6 lg:grid-cols-2">
              <Card title="Sign-ins, last 7 days" description="Successful sign-ins and failures per day.">
                <SignInsChart days={signInsByDay} />
              </Card>
              <Card title="Server">
                <DescriptionList items={[
                  { label: 'Name', value: server.name },
                  { label: 'Version', value: server.version },
                  { label: 'Issuer', value: <Code>{server.issuer}</Code> },
                  { label: 'Events retention', value: `${server.eventsRetentionDays} days` },
                ]} />
                <div className="flex flex-wrap items-center gap-2 text-sm">
                  <span className="text-xs uppercase tracking-wide text-slate-500">Discovery</span>
                  <a href={server.discovery} target="_blank" rel="noreferrer" className="min-w-0 break-all font-medium text-indigo-600 hover:text-indigo-500">{server.discovery}</a>
                  <CopyButton value={server.discovery} />
                </div>
                <div className="flex flex-wrap gap-2">
                  {server.registrationEnabled ? <Badge tone="green">Registration open</Badge> : <Badge>Registration closed</Badge>}
                  {server.requireEmailVerification && <Badge tone="sky">E-mail verification required</Badge>}
                  {server.emailConfigured ? <Badge tone="green">E-mail configured</Badge> : <Badge tone="amber">E-mail not configured</Badge>}
                </div>
              </Card>
            </div>

            <Card title="Recent events" actions={<Button variant="secondary" size="sm" onClick={() => navigate('/events')}>All events</Button>}>
              <EventsTable events={recentEvents} />
            </Card>
          </div>
        )}
      </Loader>
    </>
  );
}

/** Stacked bars per day: successes and failures. */
function SignInsChart({ days }: { days: Dashboard['signInsByDay'] }) {
  const max = Math.max(1, ...days.map(d => d.signIns + d.failures));
  const width = 420;
  const height = 160;
  const padding = { left: 28, bottom: 22, top: 6 };
  const barWidth = (width - padding.left) / Math.max(1, days.length);

  return (
    <div className="space-y-2">
      <svg viewBox={`0 0 ${width} ${height}`} className="w-full" role="img" aria-label={`Sign-ins per day: ${days.map(d => `${d.date} ${d.signIns} ok, ${d.failures} failed`).join('; ')}`}>
        {[0, 0.5, 1].map(f => {
          const y = padding.top + (1 - f) * (height - padding.bottom - padding.top);
          return (
            <g key={f}>
              <line x1={padding.left} x2={width} y1={y} y2={y} className="stroke-slate-200" strokeDasharray="3 3" />
              <text x={padding.left - 4} y={y + 3} textAnchor="end" className="fill-slate-400 text-[9px]">{Math.round(max * f)}</text>
            </g>
          );
        })}
        {days.map((d, i) => {
          const scale = (height - padding.bottom - padding.top) / max;
          const okHeight = d.signIns * scale;
          const failHeight = d.failures * scale;
          const x = padding.left + i * barWidth + barWidth * 0.2;
          const w = barWidth * 0.6;
          const base = height - padding.bottom;
          return (
            <g key={d.date}>
              <title>{`${d.date}: ${d.signIns} sign-ins, ${d.failures} failures`}</title>
              <rect x={x} y={base - okHeight} width={w} height={okHeight} className="fill-indigo-500" rx={2} />
              <rect x={x} y={base - okHeight - failHeight} width={w} height={failHeight} className="fill-red-400" rx={2} />
              <text x={x + w / 2} y={height - 6} textAnchor="middle" className="fill-slate-500 text-[9px]">{d.date.slice(5)}</text>
            </g>
          );
        })}
      </svg>
      <div className="flex gap-4 text-xs text-slate-500">
        <span className="flex items-center gap-1"><span className="inline-block size-2.5 rounded-sm bg-indigo-500" /> Sign-ins</span>
        <span className="flex items-center gap-1"><span className="inline-block size-2.5 rounded-sm bg-red-400" /> Failures</span>
      </div>
    </div>
  );
}
