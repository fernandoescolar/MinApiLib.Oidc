import { useRef, useState } from 'react';
import { Alert, AntiforgeryField, Button, Card, Code, Field, Layout, Muted, type Antiforgery } from '../components/ui';
import { createPasskey, describeFailure, passkeysSupported } from '../passkeys';

interface Passkey {
  id: string;
  name: string | null;
  createdAt: string;
  synced: boolean;
  userVerified: boolean;
}

interface PasskeysModel {
  userName: string;
  message: string | null;
  error: string | null;
  passkeys: Passkey[];
  antiforgery: Antiforgery;
}

/** The signed-in user's passkeys: add one, see how it was registered, remove it. */
export function PasskeysPage({ model }: { model: PasskeysModel }) {
  return (
    <Layout title={`Passkeys of ${model.userName}`}>
      {model.error && <Alert kind="error">{model.error}</Alert>}
      {model.message && <Alert kind="success">{model.message}</Alert>}

      <Card>
        <p>
          A passkey signs you in without a password, and cannot be phished: the browser only uses it on this server. The
          tokens then say how you signed in: <Code>amr</Code> and <Code>"acr": "phr"</Code>.
        </p>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-slate-500">
              <tr><th className="py-2 font-medium">Name</th><th className="font-medium">Created</th><th className="font-medium">Kind</th><th /></tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {model.passkeys.length === 0 && (
                <tr><td colSpan={4} className="py-3 text-slate-500">No passkeys yet.</td></tr>
              )}
              {model.passkeys.map(passkey => (
                <tr key={passkey.id}>
                  <td className="py-2 font-medium text-slate-800">{passkey.name}</td>
                  <td className="whitespace-nowrap">{new Date(passkey.createdAt).toLocaleString()}</td>
                  <td>
                    {passkey.synced ? 'Synced (swk)' : 'This device (hwk)'}, {passkey.userVerified ? 'user verified (mfa)' : 'user present'}
                  </td>
                  <td className="text-right">
                    <form method="post" action="/account/passkey/remove">
                      <AntiforgeryField antiforgery={model.antiforgery} />
                      <input type="hidden" name="id" value={passkey.id} />
                      <Button type="submit" variant="danger" className="px-3 py-1">Remove</Button>
                    </form>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      <AddPasskey antiforgery={model.antiforgery} />

      <Muted>
        The sample keeps passkeys in memory: they are gone when the server restarts, so remove them from your browser or
        password manager too.
      </Muted>
    </Layout>
  );
}

function AddPasskey({ antiforgery }: { antiforgery: Antiforgery }) {
  const form = useRef<HTMLFormElement>(null);
  const credential = useRef<HTMLInputElement>(null);
  const [status, setStatus] = useState<string | null>(passkeysSupported() ? null : 'This browser does not support passkeys.');

  async function add() {
    try {
      credential.current!.value = await createPasskey(form.current!);
      form.current!.submit();
    } catch (error) {
      setStatus(describeFailure(error, 'The passkey was not created.'));
    }
  }

  return (
    <Card title="Add a passkey">
      <form ref={form} method="post" action="/account/passkey/register" className="space-y-4">
        <AntiforgeryField antiforgery={antiforgery} />
        <input ref={credential} type="hidden" name="credential" />
        <Field id="name" name="name" label="Name of the new passkey" placeholder="My laptop" />
        <Button type="button" disabled={!passkeysSupported()} onClick={() => void add()}>Add a passkey</Button>
        {status && <Muted>{status}</Muted>}
      </form>
    </Card>
  );
}
