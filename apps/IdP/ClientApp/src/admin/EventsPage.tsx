import { Button, Card, Field, PageHeader, Pagination, Select } from '../components/ui';
import { query, type Page } from '../api';
import { pageNumber, useAdmin, useDebounced, useLoad, useQueryParams } from './hooks';
import { EventsTable, Loader, fromDateTimeLocal } from './shared';
import type { EventFilters, EventModel } from './types';

const filterKeys = ['category', 'type', 'name', 'subjectId', 'clientId', 'q', 'from', 'to', 'page'] as const;

export function EventsPage() {
  const { api } = useAdmin();
  const [filters, setFilters] = useQueryParams(filterKeys);
  const options = useLoad(() => api.get<EventFilters>('/api/admin/events/filters'), [api]);
  const q = useDebounced(filters.q);
  const page = pageNumber(filters.page);
  const events = useLoad(
    () => api.get<Page<EventModel>>(`/api/admin/events${query({
      category: filters.category,
      type: filters.type,
      name: filters.name,
      subjectId: filters.subjectId,
      clientId: filters.clientId,
      q,
      from: fromDateTimeLocal(filters.from),
      to: fromDateTimeLocal(filters.to),
      page,
      pageSize: 50,
    })}`),
    [api, filters.category, filters.type, filters.name, filters.subjectId, filters.clientId, q, filters.from, filters.to, page],
  );
  const hasFilter = filterKeys.some(k => k !== 'page' && filters[k]);

  return (
    <>
      <PageHeader title="Events" description="The audit log: sign-ins, tokens, consents and what administrators did. Click a row for its details." actions={<Button variant="secondary" size="sm" onClick={events.reload}>Refresh</Button>} />
      <Card>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Select id="f-category" label="Category" value={filters.category} onChange={e => setFilters({ category: e.target.value, page: null })}>
            <option value="">Any</option>
            {options.data?.categories.map(c => <option key={c} value={c}>{c}</option>)}
          </Select>
          <Select id="f-type" label="Type" value={filters.type} onChange={e => setFilters({ type: e.target.value, page: null })}>
            <option value="">Any</option>
            {(options.data?.types ?? ['Success', 'Failure', 'Information', 'Error']).map(t => <option key={t} value={t}>{t}</option>)}
          </Select>
          <Select id="f-name" label="Event" value={filters.name} onChange={e => setFilters({ name: e.target.value, page: null })}>
            <option value="">Any</option>
            {options.data?.names.map(n => <option key={n} value={n}>{n}</option>)}
          </Select>
          <Field id="f-q" label="Search" type="search" value={filters.q} onChange={e => setFilters({ q: e.target.value, page: null })} placeholder="User, message, IP address" />
          <Field id="f-subject" label="Subject id" value={filters.subjectId} onChange={e => setFilters({ subjectId: e.target.value, page: null })} />
          <Field id="f-client" label="Client id" value={filters.clientId} onChange={e => setFilters({ clientId: e.target.value, page: null })} />
          <Field id="f-from" label="From" type="datetime-local" value={filters.from} onChange={e => setFilters({ from: e.target.value, page: null })} />
          <Field id="f-to" label="To" type="datetime-local" value={filters.to} onChange={e => setFilters({ to: e.target.value, page: null })} />
        </div>
        {hasFilter && (
          <div className="flex justify-end">
            <Button variant="ghost" size="sm" onClick={() => setFilters(Object.fromEntries(filterKeys.map(k => [k, null])) as Record<typeof filterKeys[number], null>)}>Clear filters</Button>
          </div>
        )}
        <Loader state={events}>
          {result => (
            <>
              <EventsTable events={result.items} />
              <Pagination page={result.pageNumber} pageSize={result.pageSize} total={result.total} onChange={p => setFilters({ page: String(p) })} />
            </>
          )}
        </Loader>
      </Card>
    </>
  );
}
