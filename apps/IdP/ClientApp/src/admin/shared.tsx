import { useState, type ReactNode } from 'react';
import { Alert, Badge, Button, Checkbox, Code, ConfirmDialog, CopyButton, Field, Modal, Spinner, Table, formatDate } from '../components/ui';
import { useBusy, type Busy, type Loaded } from './hooks';
import type { Claim, ClientClaim, EventModel, NewSecretResult, SecretModel } from './types';

export function Errors({ messages, onClose }: { messages: string[] | null; onClose?: () => void }) {
  if (!messages || messages.length === 0) return null;
  return (
    <Alert kind="error" onClose={onClose}>
      {messages.length === 1 ? messages[0] : <ul className="list-inside list-disc">{messages.map((m, i) => <li key={i}>{m}</li>)}</ul>}
    </Alert>
  );
}

/** The error and success alerts of a save or action. */
export function BusyAlerts({ state }: { state: Busy }) {
  return (
    <>
      <Errors messages={state.error} onClose={state.clear} />
      {state.success && <Alert kind="success" onClose={state.clear}>{state.success}</Alert>}
    </>
  );
}

export function Loading() {
  return <div className="flex justify-center py-10 text-indigo-600" aria-busy="true" aria-label="Loading"><Spinner /></div>;
}

/** Renders a loaded value, or the spinner or error while it is not there. */
export function Loader<T>({ state, children }: { state: Loaded<T>; children: (data: T) => ReactNode }) {
  if (state.error) return <Errors messages={state.error} />;
  if (state.data === null) return <Loading />;
  return (
    <div className={state.loading ? 'opacity-60 transition-opacity' : ''}>
      {children(state.data)}
    </div>
  );
}

export function EnabledBadge({ enabled }: { enabled: boolean }) {
  return enabled ? <Badge tone="green">Enabled</Badge> : <Badge tone="red">Disabled</Badge>;
}

export function EventTypeBadge({ type }: { type: EventModel['eventType'] }) {
  const tone = { Success: 'green', Failure: 'red', Error: 'red', Information: 'sky' } as const;
  return <Badge tone={tone[type] ?? 'slate'}>{type}</Badge>;
}

export function JsonPre({ value }: { value: unknown }) {
  return <pre className="max-h-96 overflow-auto rounded-lg bg-slate-900 p-3 font-mono text-xs leading-relaxed text-slate-100">{JSON.stringify(value, null, 2)}</pre>;
}

/** The audit events; a row expands to show its details. */
export function EventsTable({ events, showUser = true }: { events: EventModel[]; showUser?: boolean }) {
  const [expanded, setExpanded] = useState<string | null>(null);
  return (
    <Table<EventModel>
      rows={events}
      keyOf={e => e.id}
      onRowClick={e => setExpanded(expanded === e.id ? null : e.id)}
      empty="No events."
      columns={[
        { header: 'Time', cell: e => <span className="whitespace-nowrap">{formatDate(e.timeStamp)}</span> },
        { header: 'Type', cell: e => <EventTypeBadge type={e.eventType} /> },
        { header: 'Event', cell: e => <><span className="font-medium text-slate-800">{e.name}</span><span className="block text-xs text-slate-500">{e.category}</span></> },
        ...(showUser ? [{ header: 'User', cell: (e: EventModel) => e.username ?? (e.subjectId ? <Code>{e.subjectId}</Code> : <span className="text-slate-400">—</span>) }] : []),
        { header: 'Client', cell: e => e.clientId ? <Code>{e.clientId}</Code> : <span className="text-slate-400">—</span> },
        {
          header: 'Message',
          className: 'min-w-64',
          cell: e => (
            <div className="space-y-2">
              <span className="break-words">{e.message ?? <span className="text-slate-400">—</span>}</span>
              {expanded === e.id && (
                <div className="space-y-1 text-xs text-slate-500" onClick={ev => ev.stopPropagation()}>
                  <p>IP {e.remoteIpAddress ?? '—'} · activity {e.activityId ?? '—'} · event id {e.eventId}</p>
                  <JsonPre value={e.details ?? {}} />
                </div>
              )}
            </div>
          ),
        },
      ]}
    />
  );
}

/** Editable key/value pairs (attributes, properties). */
export function KeyValueEditor({ label, value, onChange, keyLabel = 'Key', valueLabel = 'Value' }: { label: string; value: Record<string, string>; onChange: (value: Record<string, string>) => void; keyLabel?: string; valueLabel?: string }) {
  const entries = Object.entries(value);
  const [draft, setDraft] = useState({ key: '', value: '' });

  const add = () => {
    const key = draft.key.trim();
    if (!key) return;
    onChange({ ...value, [key]: draft.value });
    setDraft({ key: '', value: '' });
  };
  const remove = (key: string) => {
    const next = { ...value };
    delete next[key];
    onChange(next);
  };

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-slate-700">{label}</legend>
      {entries.length === 0 && <p className="text-xs text-slate-500">None.</p>}
      {entries.map(([key, v]) => (
        <div key={key} className="flex items-center gap-2">
          <Code className="shrink-0">{key}</Code>
          <input aria-label={`Value of ${key}`} className="block min-w-0 flex-1 rounded-lg border border-slate-300 px-3 py-1.5 text-sm" value={v} onChange={e => onChange({ ...value, [key]: e.target.value })} />
          <Button type="button" variant="ghost" size="sm" onClick={() => remove(key)} aria-label={`Remove ${key}`}>Remove</Button>
        </div>
      ))}
      <div className="flex flex-wrap items-end gap-2">
        <input aria-label={keyLabel} placeholder={keyLabel} className="block w-40 rounded-lg border border-slate-300 px-3 py-1.5 text-sm" value={draft.key} onChange={e => setDraft({ ...draft, key: e.target.value })} />
        <input aria-label={valueLabel} placeholder={valueLabel} className="block min-w-0 flex-1 rounded-lg border border-slate-300 px-3 py-1.5 text-sm" value={draft.value} onChange={e => setDraft({ ...draft, value: e.target.value })} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); add(); } }} />
        <Button type="button" variant="secondary" size="sm" onClick={add} disabled={!draft.key.trim()}>Add</Button>
      </div>
    </fieldset>
  );
}

/** Editable claims rows; with `valueType` the rows are client claims. */
export function ClaimsEditor<T extends Claim | ClientClaim>({ label, value, onChange, withValueType }: { label: string; value: T[]; onChange: (value: T[]) => void; withValueType?: boolean }) {
  const [draft, setDraft] = useState({ type: '', value: '' });
  const inputClass = 'block min-w-0 flex-1 rounded-lg border border-slate-300 px-3 py-1.5 text-sm';

  const update = (index: number, patch: Partial<ClientClaim>) => onChange(value.map((c, i) => (i === index ? { ...c, ...patch } : c)));
  const add = () => {
    const type = draft.type.trim();
    if (!type) return;
    const claim = (withValueType ? { type, value: draft.value, valueType: 'http://www.w3.org/2001/XMLSchema#string' } : { type, value: draft.value }) as T;
    onChange([...value, claim]);
    setDraft({ type: '', value: '' });
  };

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-slate-700">{label}</legend>
      {value.length === 0 && <p className="text-xs text-slate-500">None.</p>}
      {value.map((claim, i) => (
        <div key={i} className="flex flex-wrap items-center gap-2">
          <input aria-label="Claim type" className={`${inputClass} w-40`} value={claim.type} onChange={e => update(i, { type: e.target.value })} />
          <input aria-label="Claim value" className={inputClass} value={claim.value} onChange={e => update(i, { value: e.target.value })} />
          {withValueType && <input aria-label="Value type" className={`${inputClass} w-56`} value={'valueType' in claim ? claim.valueType : ''} onChange={e => update(i, { valueType: e.target.value })} />}
          <Button type="button" variant="ghost" size="sm" onClick={() => onChange(value.filter((_, j) => j !== i))} aria-label={`Remove claim ${claim.type}`}>Remove</Button>
        </div>
      ))}
      <div className="flex flex-wrap items-end gap-2">
        <input aria-label="New claim type" placeholder="Type" className={`${inputClass} w-40`} value={draft.type} onChange={e => setDraft({ ...draft, type: e.target.value })} />
        <input aria-label="New claim value" placeholder="Value" className={inputClass} value={draft.value} onChange={e => setDraft({ ...draft, value: e.target.value })} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); add(); } }} />
        <Button type="button" variant="secondary" size="sm" onClick={add} disabled={!draft.type.trim()}>Add</Button>
      </div>
    </fieldset>
  );
}

export interface Option {
  value: string;
  label: ReactNode;
  hint?: string;
}

export function CheckboxList({ id, options, selected, onChange, empty = 'Nothing to pick.' }: { id: string; options: Option[]; selected: string[]; onChange: (selected: string[]) => void; empty?: string }) {
  if (options.length === 0) return <p className="text-sm text-slate-500">{empty}</p>;
  const toggle = (value: string, checked: boolean) => onChange(checked ? [...new Set([...selected, value])] : selected.filter(v => v !== value));
  return (
    <div className="grid gap-2 sm:grid-cols-2">
      {options.map(option => (
        <Checkbox key={option.value} id={`${id}-${option.value}`} label={option.label} hint={option.hint} checked={selected.includes(option.value)} onChange={e => toggle(option.value, e.target.checked)} />
      ))}
    </div>
  );
}

/** Secrets of a client or an API resource: fingerprints, an add dialog that shows the value once, and removal. */
export function SecretsPanel({ secrets, onAdd, onRemove, onChanged }: {
  secrets: SecretModel[];
  onAdd: (body: { description?: string; expiration?: string; value?: string }) => Promise<NewSecretResult>;
  onRemove: (id: string) => Promise<void>;
  onChanged: () => void;
}) {
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState<SecretModel | null>(null);
  const [created, setCreated] = useState<NewSecretResult | null>(null);
  const [form, setForm] = useState({ description: '', expiration: '', value: '' });
  const state = useBusy();

  const add = async () => {
    const result = await state.run(() => onAdd({
      description: form.description.trim() || undefined,
      expiration: form.expiration ? new Date(form.expiration).toISOString() : undefined,
      value: form.value || undefined,
    }));
    if (result) {
      setAdding(false);
      setForm({ description: '', expiration: '', value: '' });
      setCreated(result);
      onChanged();
    }
  };

  const remove = async () => {
    if (!removing) return;
    const ok = await state.run(() => onRemove(removing.id).then(() => true));
    if (ok) { setRemoving(null); onChanged(); }
  };

  return (
    <div className="space-y-4">
      <BusyAlerts state={state} />
      <div className="flex justify-end"><Button type="button" variant="secondary" onClick={() => { state.clear(); setAdding(true); }}>Add secret</Button></div>
      <Table<SecretModel>
        rows={secrets}
        keyOf={s => s.id}
        empty="No secrets. A client without secrets must be public (no client secret required)."
        columns={[
          { header: 'Fingerprint', cell: s => <Code>{s.id}</Code> },
          { header: 'Description', cell: s => s.description ?? <span className="text-slate-400">—</span> },
          { header: 'Type', cell: s => s.type },
          { header: 'Expires', cell: s => s.expiration ? formatDate(s.expiration) : 'Never' },
          { header: '', className: 'text-right', cell: s => <Button type="button" variant="danger" size="sm" onClick={() => setRemoving(s)}>Remove</Button> },
        ]}
      />
      {adding && (
        <Modal title="Add secret" onClose={() => setAdding(false)} footer={<><Button variant="secondary" onClick={() => setAdding(false)}>Cancel</Button><Button onClick={add} loading={state.busy}>Add</Button></>}>
          <Errors messages={state.error} />
          <Field id="secret-description" label="Description" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} placeholder="Where it is used" />
          <Field id="secret-expiration" label="Expiration" type="datetime-local" value={form.expiration} onChange={e => setForm({ ...form, expiration: e.target.value })} hint="Leave empty for a secret that never expires." />
          <Field id="secret-value" label="Value" type="text" autoComplete="off" spellCheck={false} value={form.value} onChange={e => setForm({ ...form, value: e.target.value })} hint="Leave empty to generate a random one." />
        </Modal>
      )}
      {created && <SecretRevealModal title="Secret added" value={created.value} onClose={() => setCreated(null)} />}
      {removing && (
        <ConfirmDialog
          title="Remove secret"
          message={<>Remove the secret <Code>{removing.id}</Code>? Anything using it stops authenticating.</>}
          confirmLabel="Remove"
          busy={state.busy}
          onConfirm={remove}
          onCancel={() => setRemoving(null)}
        />
      )}
    </div>
  );
}

/** Shows a generated secret the only time it is visible. */
export function SecretRevealModal({ title, value, onClose, children }: { title: string; value: string; onClose: () => void; children?: ReactNode }) {
  return (
    <Modal title={title} onClose={onClose} footer={<Button onClick={onClose}>Done</Button>}>
      {children}
      <Alert kind="warning">Copy this secret now. It is stored hashed and cannot be shown again.</Alert>
      <div className="flex items-center gap-2">
        <code className="block min-w-0 flex-1 break-all rounded-lg bg-slate-100 p-3 font-mono text-sm">{value}</code>
        <CopyButton value={value} />
      </div>
    </Modal>
  );
}

/** Converts an ISO date to the value of a datetime-local input, and back. */
export function toDateTimeLocal(iso: string | null | undefined): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function fromDateTimeLocal(local: string): string | undefined {
  if (!local) return undefined;
  const date = new Date(local);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

export function Dash() {
  return <span className="text-slate-400">—</span>;
}
