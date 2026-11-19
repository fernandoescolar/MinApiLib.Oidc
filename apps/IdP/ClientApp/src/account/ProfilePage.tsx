import { useState, type FormEvent } from 'react';
import { Alert, Button, Card, Field, Muted, PageHeader } from '../components/ui';
import { errorMessage, fieldErrors, type ProfileModel, type ProfileUpdateResult } from './api';
import { useAccount } from './context';
import { Loading } from './common';

export function ProfilePage() {
  const { profile } = useAccount();
  return (
    <>
      <PageHeader title="Profile" description="Your personal details and the attributes applications receive about you." />
      <Loading of={profile}>{data => <ProfileForm key={data.id} initial={data} />}</Loading>
    </>
  );
}

/** The identity claims have their own fields and are not free attributes. */
const reservedKeys = new Set(['role', 'groups', 'sub']);

interface AttributeRow {
  id: number;
  key: string;
  value: string;
}

function ProfileForm({ initial }: { initial: ProfileModel }) {
  const { api, profile } = useAccount();
  const [firstName, setFirstName] = useState(initial.firstName ?? '');
  const [lastName, setLastName] = useState(initial.lastName ?? '');
  const [phoneNumber, setPhoneNumber] = useState(initial.phoneNumber ?? '');
  const [email, setEmail] = useState(initial.email ?? '');
  const [rows, setRows] = useState<AttributeRow[]>(() => Object.entries(initial.attributes).filter(([key]) => !reservedKeys.has(key)).map(([key, value], id) => ({ id, key, value })));
  const [nextId, setNextId] = useState(rows.length);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState<ProfileUpdateResult | null>(null);

  const emailChanged = email.trim() !== (initial.email ?? '') && email.trim() !== '';

  function addRow() {
    setRows([...rows, { id: nextId, key: '', value: '' }]);
    setNextId(nextId + 1);
  }

  function updateRow(id: number, patch: Partial<AttributeRow>) {
    setRows(rows.map(row => row.id === id ? { ...row, ...patch } : row));
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setErrors({});
    setSaved(null);

    // Keys that were there and are gone now are sent empty: that is how the server removes them.
    const attributes: Record<string, string> = {};
    for (const key of Object.keys(initial.attributes)) if (!reservedKeys.has(key)) attributes[key] = '';
    for (const row of rows) {
      const key = row.key.trim();
      if (key && !reservedKeys.has(key)) attributes[key] = row.value;
    }

    try {
      const result = await api.put<ProfileUpdateResult>('/api/account/profile', {
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        phoneNumber: phoneNumber.trim() || null,
        email: email.trim() || null,
        attributes,
      });
      profile.setData(result.profile);
      setSaved(result);
      if (!result.emailChangePending) setEmail(result.profile.email ?? '');
    } catch (failure) {
      const fields = fieldErrors(failure);
      if (fields) setErrors(fields);
      setError(errorMessage(failure));
    } finally {
      setSaving(false);
    }
  }

  const duplicateKeys = new Set(rows.map(r => r.key.trim()).filter((key, i, all) => key && all.indexOf(key) !== i));

  return (
    <form onSubmit={save} className="max-w-3xl space-y-6" noValidate>
      {saved && (
        <Alert kind="success" onClose={() => setSaved(null)}>
          Profile saved.
          {saved.emailChangePending && <> We sent a verification link to <strong>{saved.pendingEmail}</strong>; your e-mail changes once you click it.</>}
        </Alert>
      )}
      {error && <Alert kind="error" onClose={() => setError(null)}>{error}</Alert>}

      <Card title="Personal details">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="First name" id="firstName" value={firstName} onChange={e => setFirstName(e.target.value)} error={errors.firstName} autoComplete="given-name" />
          <Field label="Last name" id="lastName" value={lastName} onChange={e => setLastName(e.target.value)} error={errors.lastName} autoComplete="family-name" />
        </div>
        <Field label="Phone number" id="phoneNumber" type="tel" value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)} error={errors.phoneNumber} autoComplete="tel" />
        <Field
          label="E-mail"
          id="email"
          type="email"
          value={email}
          onChange={e => setEmail(e.target.value)}
          error={errors.email}
          autoComplete="email"
          hint={emailChanged ? 'We will send a verification link to the new address. The change applies once you click it.' : 'Changing it requires verifying the new address.'}
        />
        <p className="text-xs text-slate-500">User name: <span className="font-medium text-slate-700">{initial.userName}</span>. It cannot be changed here.</p>
      </Card>

      <Card
        title="Attributes"
        description="Free key/value pairs about you. Applications may receive them as claims."
        actions={<Button type="button" variant="secondary" size="sm" onClick={addRow}>Add attribute</Button>}
      >
        {rows.length === 0 && <Muted>No attributes.</Muted>}
        {rows.map((row, index) => {
          const key = row.key.trim();
          const problem = reservedKeys.has(key) ? 'Reserved name.' : duplicateKeys.has(key) ? 'Duplicate name.' : undefined;
          return (
            <div key={row.id} className="grid gap-2 sm:grid-cols-[1fr_2fr_auto] sm:items-start">
              <Field label={index === 0 ? 'Name' : `Name ${index + 1}`} id={`attr-key-${row.id}`} value={row.key} onChange={e => updateRow(row.id, { key: e.target.value })} placeholder="department" error={problem} spellCheck={false} />
              <Field label={index === 0 ? 'Value' : `Value ${index + 1}`} id={`attr-value-${row.id}`} value={row.value} onChange={e => updateRow(row.id, { value: e.target.value })} placeholder="Engineering" />
              <Button type="button" variant="ghost" size="sm" className="sm:mt-7" onClick={() => setRows(rows.filter(r => r.id !== row.id))} aria-label={`Remove attribute ${row.key || index + 1}`}>Remove</Button>
            </div>
          );
        })}
        {errors.attributes && <p className="text-xs text-red-600">{errors.attributes}</p>}
      </Card>

      <div className="flex justify-end">
        <Button type="submit" loading={saving} disabled={duplicateKeys.size > 0}>Save changes</Button>
      </div>
    </form>
  );
}
