import { useState } from 'react';
import { Alert, AntiforgeryField, Button, Card, Checkbox, FlowLayout, Muted } from '../components/ui';
import type { Antiforgery, Product } from '../spa';

interface Scope {
  value: string;
  displayName: string;
  description: string | null;
  required: boolean;
  emphasize: boolean;
}

interface ConsentModel {
  returnUrl: string;
  userCode: string | null;
  client: { name: string; uri: string | null; logoUri: string | null; description: string | null };
  allowRemember: boolean;
  identityScopes: Scope[];
  apiScopes: Scope[];
  offlineAccess: boolean;
  userName: string | null;
  error: string | null;
  antiforgery: Antiforgery;
  product: Product;
}

/** Lets the user grant a client (or a device) access to the requested resources. */
export function ConsentPage({ model }: { model: ConsentModel }) {
  const device = model.userCode != null;
  const [chosen, setChosen] = useState<Set<string>>(new Set([...model.identityScopes, ...model.apiScopes].map(s => s.value)));

  function toggle(value: string, checked: boolean) {
    const next = new Set(chosen);
    if (checked) next.add(value); else next.delete(value);
    setChosen(next);
  }

  return (
    <FlowLayout title={device ? 'Connect a device' : 'Allow access?'} product={model.product}>
      {model.error && <Alert kind="error">{model.error}</Alert>}
      <Card>
        <div className="flex items-center gap-3">
          {model.client.logoUri && <img src={model.client.logoUri} alt="" className="size-10 rounded-lg" />}
          <div>
            <p className="font-medium text-slate-900">{model.client.uri ? <a href={model.client.uri} className="underline">{model.client.name}</a> : model.client.name}</p>
            <p className="text-sm text-slate-500">{model.client.description ?? 'wants to access your account'}{model.userName ? ` (${model.userName})` : ''}.</p>
          </div>
        </div>
        <form method="post" action={device ? '/device/consent' : '/consent'} className="space-y-5">
          <AntiforgeryField antiforgery={model.antiforgery} />
          {device ? <input type="hidden" name="userCode" value={model.userCode!} /> : <input type="hidden" name="returnUrl" value={model.returnUrl} />}

          {model.identityScopes.length > 0 && (
            <fieldset className="space-y-2">
              <legend className="text-sm font-medium text-slate-700">Personal information</legend>
              {model.identityScopes.map(scope => <ScopeRow key={scope.value} scope={scope} checked={chosen.has(scope.value)} onChange={c => toggle(scope.value, c)} />)}
            </fieldset>
          )}
          {model.apiScopes.length > 0 && (
            <fieldset className="space-y-2">
              <legend className="text-sm font-medium text-slate-700">Application access</legend>
              {model.apiScopes.map(scope => <ScopeRow key={scope.value} scope={scope} checked={chosen.has(scope.value)} onChange={c => toggle(scope.value, c)} />)}
            </fieldset>
          )}
          {model.offlineAccess && <Muted>The application also asks to keep its access while you are away (offline access).</Muted>}

          {model.allowRemember && <Checkbox id="remember" name="remember" value="true" label="Remember my decision" hint="You will not be asked again for this application." />}

          <div className="flex gap-3">
            <Button type="submit" name="button" value="allow" className="flex-1">Allow</Button>
            <Button type="submit" name="button" value="deny" variant="secondary" className="flex-1">Deny</Button>
          </div>
        </form>
      </Card>
    </FlowLayout>
  );
}

function ScopeRow({ scope, checked, onChange }: { scope: Scope; checked: boolean; onChange: (checked: boolean) => void }) {
  return (
    <Checkbox
      id={`scope-${scope.value}`}
      name="scopes"
      value={scope.value}
      checked={scope.required || checked}
      disabled={scope.required}
      onChange={e => onChange(e.target.checked)}
      label={<span className={scope.emphasize ? 'text-indigo-700' : ''}>{scope.displayName}{scope.required && <span className="ml-1 text-xs text-slate-400">(required)</span>}</span>}
      hint={scope.description ?? undefined}
    />
  );
}
