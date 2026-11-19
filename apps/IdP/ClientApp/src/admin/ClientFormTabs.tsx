import type { ChangeEvent, ReactNode } from 'react';
import { Checkbox, Code, Field, ListField, Muted, Select, TextArea, Toggle } from '../components/ui';
import { useAdmin, useLoad } from './hooks';
import { CheckboxList, ClaimsEditor, KeyValueEditor, Loader } from './shared';
import { grantTypes, type ClientModel, type IdentityProviderModel, type ResourceModel } from './types';

export type Patch = (patch: Partial<ClientModel>) => void;

interface TabProps {
  client: ClientModel;
  patch: Patch;
  isNew: boolean;
}

/** The combinations the server rejects: code, hybrid and implicit exclude each other. */
export function grantTypeProblems(types: string[]): string[] {
  const problems: string[] = [];
  const has = (t: string) => types.includes(t);
  if (has('authorization_code') && has('hybrid')) problems.push('Authorization code and hybrid cannot be combined.');
  if (has('authorization_code') && has('implicit')) problems.push('Authorization code and implicit cannot be combined.');
  if (has('hybrid') && has('implicit')) problems.push('Hybrid and implicit cannot be combined.');
  if (types.length === 0) problems.push('Pick at least one grant type.');
  return problems;
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="space-y-3">
      <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">{title}</h3>
      {children}
    </section>
  );
}

const text = (value: string) => (value.trim() === '' ? null : value);

export function GeneralTab({ client, patch, isNew }: TabProps) {
  return (
    <div className="space-y-6">
      <Toggle label="Enabled" hint="A disabled client is refused at every endpoint." checked={client.enabled} onChange={enabled => patch({ enabled })} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="clientId" label="Client id" value={client.clientId} onChange={e => patch({ clientId: e.target.value })} required readOnly={!isNew} hint={isNew ? 'Unique; sent by the application in every request.' : 'The id cannot change.'} spellCheck={false} autoFocus={isNew} />
        <Field id="clientName" label="Display name" value={client.clientName ?? ''} onChange={e => patch({ clientName: text(e.target.value) })} hint="Shown on the consent page." />
      </div>
      <TextArea id="description" label="Description" value={client.description ?? ''} onChange={e => patch({ description: text(e.target.value) })} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="clientUri" label="Client URI" type="url" value={client.clientUri ?? ''} onChange={e => patch({ clientUri: text(e.target.value) })} hint="Linked from the consent page." />
        <Field id="logoUri" label="Logo URI" type="url" value={client.logoUri ?? ''} onChange={e => patch({ logoUri: text(e.target.value) })} />
      </div>
      <Section title="Consent">
        <Toggle label="Require consent" hint="Ask the user before releasing claims and scopes." checked={client.requireConsent} onChange={requireConsent => patch({ requireConsent })} />
        <Toggle label="Allow remembering consent" checked={client.allowRememberConsent} onChange={allowRememberConsent => patch({ allowRememberConsent })} disabled={!client.requireConsent} />
        <NullableNumber id="consentLifetime" label="Consent lifetime (seconds)" value={client.consentLifetime} onChange={consentLifetime => patch({ consentLifetime })} hint="Empty: the remembered consent never expires." />
      </Section>
      <Section title="Sign-in">
        <Toggle label="Enable local login" hint="Users can sign in with their password or passkey; off restricts them to external providers." checked={client.enableLocalLogin} onChange={enableLocalLogin => patch({ enableLocalLogin })} />
        <NullableNumber id="userSsoLifetime" label="Maximum session age (seconds)" value={client.userSsoLifetime} onChange={userSsoLifetime => patch({ userSsoLifetime })} hint="Users signed in longer than this must authenticate again for this client. Empty: no limit." />
      </Section>
    </div>
  );
}

export function FlowsTab({ client, patch }: TabProps) {
  const problems = grantTypeProblems(client.allowedGrantTypes);
  const toggleGrant = (value: string, checked: boolean) =>
    patch({ allowedGrantTypes: checked ? [...client.allowedGrantTypes, value] : client.allowedGrantTypes.filter(g => g !== value) });

  return (
    <div className="space-y-6">
      <Section title="Grant types">
        <div className="grid gap-3 sm:grid-cols-2">
          {grantTypes.map(g => (
            <Checkbox key={g.value} id={`grant-${g.value}`} label={g.label} hint={g.hint} checked={client.allowedGrantTypes.includes(g.value)} onChange={e => toggleGrant(g.value, e.target.checked)} />
          ))}
        </div>
        {problems.map(p => <p key={p} className="text-xs text-amber-700">{p}</p>)}
      </Section>
      <Section title="Client authentication">
        <Toggle label="Require a client secret" hint="Off makes a public client (SPA, mobile app): it authenticates with PKCE only." checked={client.requireClientSecret} onChange={requireClientSecret => patch({ requireClientSecret })} />
        <Toggle label="Require DPoP" hint="Tokens are bound to the client's key (RFC 9449)." checked={client.requireDPoP} onChange={requireDPoP => patch({ requireDPoP })} />
      </Section>
      <Section title="Authorization requests">
        <Toggle label="Require PKCE" checked={client.requirePkce} onChange={requirePkce => patch({ requirePkce })} />
        <Toggle label="Allow plain-text PKCE" hint="Not recommended; S256 is the default." checked={client.allowPlainTextPkce} onChange={allowPlainTextPkce => patch({ allowPlainTextPkce })} />
        <Toggle label="Require a request object" hint="JAR: parameters must come signed in a request or request_uri." checked={client.requireRequestObject} onChange={requireRequestObject => patch({ requireRequestObject })} />
        <Toggle label="Require pushed authorization requests" hint="PAR (RFC 9126): the request must be pushed first." checked={client.requirePushedAuthorization} onChange={requirePushedAuthorization => patch({ requirePushedAuthorization })} />
        <Toggle label="Allow access tokens via the browser" hint="Access tokens in the authorization response (implicit/hybrid)." checked={client.allowAccessTokensViaBrowser} onChange={allowAccessTokensViaBrowser => patch({ allowAccessTokensViaBrowser })} />
      </Section>
      <Section title="Deprecated flows">
        <Toggle label="Allow implicit flow" hint="Needed for the implicit grant type." checked={client.allowImplicitFlow} onChange={allowImplicitFlow => patch({ allowImplicitFlow })} />
        <Toggle label="Allow resource owner password flow" hint="Needed for the password grant type." checked={client.allowResourceOwnerPasswordFlow} onChange={allowResourceOwnerPasswordFlow => patch({ allowResourceOwnerPasswordFlow })} />
      </Section>
      <Section title="Device flow">
        <Field id="userCodeType" label="User code type" value={client.userCodeType ?? ''} onChange={e => patch({ userCodeType: text(e.target.value) })} hint="Empty: the server default." />
        <Field id="deviceCodeLifetime" label="Device code lifetime (seconds)" type="number" min={1} value={client.deviceCodeLifetime} onChange={e => patch({ deviceCodeLifetime: Number(e.target.value) })} />
      </Section>
    </div>
  );
}

export function UrisTab({ client, patch }: TabProps) {
  return (
    <div className="space-y-6">
      <ListField id="redirectUris" label="Redirect URIs" value={client.redirectUris} onChange={redirectUris => patch({ redirectUris })} placeholder="https://app.example.com/signin-oidc" />
      <ListField id="postLogoutRedirectUris" label="Post-logout redirect URIs" value={client.postLogoutRedirectUris} onChange={postLogoutRedirectUris => patch({ postLogoutRedirectUris })} />
      <ListField id="allowedCorsOrigins" label="Allowed CORS origins" value={client.allowedCorsOrigins} onChange={allowedCorsOrigins => patch({ allowedCorsOrigins })} hint="Origins (scheme, host, port) that may call the token and userinfo endpoints from a browser. One per line." placeholder="https://app.example.com" />
      <ListField id="requestUris" label="Allowed request URIs" value={client.requestUris} onChange={requestUris => patch({ requestUris })} hint="For request_uri in JAR requests. One per line." />
      <Section title="Logout">
        <Field id="frontChannelLogoutUri" label="Front-channel logout URI" type="url" value={client.frontChannelLogoutUri ?? ''} onChange={e => patch({ frontChannelLogoutUri: text(e.target.value) })} />
        <Toggle label="Front-channel logout needs the session id" checked={client.frontChannelLogoutSessionRequired} onChange={frontChannelLogoutSessionRequired => patch({ frontChannelLogoutSessionRequired })} />
        <Field id="backChannelLogoutUri" label="Back-channel logout URI" type="url" value={client.backChannelLogoutUri ?? ''} onChange={e => patch({ backChannelLogoutUri: text(e.target.value) })} />
        <Toggle label="Back-channel logout needs the session id" checked={client.backChannelLogoutSessionRequired} onChange={backChannelLogoutSessionRequired => patch({ backChannelLogoutSessionRequired })} />
      </Section>
    </div>
  );
}

export function ScopesTab({ client, patch }: TabProps) {
  const { api } = useAdmin();
  const scopes = useLoad(async () => {
    const [identity, apiScopes, providers] = await Promise.all([
      api.get<ResourceModel[]>('/api/admin/identity-resources'),
      api.get<ResourceModel[]>('/api/admin/api-scopes'),
      api.get<IdentityProviderModel[]>('/api/admin/identity-providers'),
    ]);
    return { identity, apiScopes, providers };
  }, [api]);

  const option = (r: ResourceModel) => ({ value: r.name, label: <>{r.displayName ?? r.name} <Code>{r.name}</Code>{!r.enabled && <span className="ml-1 text-xs text-red-600">disabled</span>}</>, hint: r.description ?? undefined });

  return (
    <Loader state={scopes}>
      {({ identity, apiScopes, providers }) => {
        // Scopes that exist on the client but not on the server stay listed so they are not lost silently.
        const known = new Set([...identity, ...apiScopes].map(r => r.name));
        const unknown = client.allowedScopes.filter(s => !known.has(s));
        return (
          <div className="space-y-6">
            <Section title="Identity resources">
              <CheckboxList id="idres" options={identity.map(option)} selected={client.allowedScopes} onChange={allowedScopes => patch({ allowedScopes })} empty="No identity resources defined." />
            </Section>
            <Section title="API scopes">
              <CheckboxList id="apiscope" options={apiScopes.map(option)} selected={client.allowedScopes} onChange={allowedScopes => patch({ allowedScopes })} empty="No API scopes defined." />
            </Section>
            {unknown.length > 0 && (
              <Section title="Unknown scopes">
                <Muted>These scopes are allowed but no longer defined on the server:</Muted>
                <CheckboxList id="unknown" options={unknown.map(s => ({ value: s, label: <Code>{s}</Code> }))} selected={client.allowedScopes} onChange={allowedScopes => patch({ allowedScopes })} />
              </Section>
            )}
            <Section title="Refresh tokens">
              <Toggle label="Allow offline access" hint="The client may request the offline_access scope and receive refresh tokens." checked={client.allowOfflineAccess} onChange={allowOfflineAccess => patch({ allowOfflineAccess })} />
            </Section>
            <Section title="Identity provider restrictions">
              <Muted>When some are checked, only those external providers are offered to users of this client.</Muted>
              <CheckboxList id="idp" options={providers.map(p => ({ value: p.alias, label: p.displayName, hint: p.alias }))} selected={client.identityProviderRestrictions} onChange={identityProviderRestrictions => patch({ identityProviderRestrictions })} empty="No external identity providers configured." />
            </Section>
          </div>
        );
      }}
    </Loader>
  );
}

export function TokensTab({ client, patch }: TabProps) {
  const number = (key: keyof ClientModel) => (e: ChangeEvent<HTMLInputElement>) => patch({ [key]: Math.max(0, Number(e.target.value) || 0) });
  return (
    <div className="space-y-6">
      <Section title="Access tokens">
        <div className="grid gap-4 sm:grid-cols-2">
          <Select id="accessTokenType" label="Access token type" value={client.accessTokenType} onChange={e => patch({ accessTokenType: e.target.value as ClientModel['accessTokenType'] })} hint="Reference tokens are opaque and validated by introspection.">
            <option value="jwt">JWT</option>
            <option value="reference">Reference</option>
          </Select>
          <Field id="accessTokenLifetime" label="Access token lifetime (seconds)" type="number" min={1} value={client.accessTokenLifetime} onChange={number('accessTokenLifetime')} />
        </div>
        <Toggle label="Include a JWT id (jti)" checked={client.includeJwtId} onChange={includeJwtId => patch({ includeJwtId })} />
      </Section>
      <Section title="Identity tokens">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field id="identityTokenLifetime" label="Identity token lifetime (seconds)" type="number" min={1} value={client.identityTokenLifetime} onChange={number('identityTokenLifetime')} />
          <Field id="authorizationCodeLifetime" label="Authorization code lifetime (seconds)" type="number" min={1} value={client.authorizationCodeLifetime} onChange={number('authorizationCodeLifetime')} />
          <Field id="authorizationNonceLifetime" label="Authorization nonce lifetime (seconds)" type="number" min={1} value={client.authorizationNonceLifetime} onChange={number('authorizationNonceLifetime')} />
        </div>
        <Toggle label="Always include user claims in the identity token" hint="Otherwise they come from the userinfo endpoint." checked={client.alwaysIncludeUserClaimsInIdToken} onChange={alwaysIncludeUserClaimsInIdToken => patch({ alwaysIncludeUserClaimsInIdToken })} />
        <ListField id="allowedIdentityTokenSigningAlgorithms" label="Allowed identity token signing algorithms" value={client.allowedIdentityTokenSigningAlgorithms} onChange={allowedIdentityTokenSigningAlgorithms => patch({ allowedIdentityTokenSigningAlgorithms })} hint="Empty: the server default. One per line (RS256, ES256...)." />
      </Section>
      <Section title="Refresh tokens">
        <div className="grid gap-4 sm:grid-cols-2">
          <Select id="refreshTokenUsage" label="Usage" value={client.refreshTokenUsage} onChange={e => patch({ refreshTokenUsage: e.target.value as ClientModel['refreshTokenUsage'] })}>
            <option value="oneTimeOnly">One time only (rotate)</option>
            <option value="reUse">Re-use</option>
          </Select>
          <Select id="refreshTokenExpiration" label="Expiration" value={client.refreshTokenExpiration} onChange={e => patch({ refreshTokenExpiration: e.target.value as ClientModel['refreshTokenExpiration'] })}>
            <option value="absolute">Absolute</option>
            <option value="sliding">Sliding</option>
          </Select>
          <Field id="absoluteRefreshTokenLifetime" label="Absolute lifetime (seconds)" type="number" min={0} value={client.absoluteRefreshTokenLifetime} onChange={number('absoluteRefreshTokenLifetime')} hint="0: no absolute limit (with sliding expiration)." />
          <Field id="slidingRefreshTokenLifetime" label="Sliding lifetime (seconds)" type="number" min={1} value={client.slidingRefreshTokenLifetime} onChange={number('slidingRefreshTokenLifetime')} />
        </div>
        <Toggle label="Update access token claims on refresh" checked={client.updateAccessTokenClaimsOnRefresh} onChange={updateAccessTokenClaimsOnRefresh => patch({ updateAccessTokenClaimsOnRefresh })} />
      </Section>
    </div>
  );
}

export function ClaimsTab({ client, patch }: TabProps) {
  return (
    <div className="space-y-6">
      <Section title="Client claims">
        <Muted>Claims added to every access token of this client.</Muted>
        <ClaimsEditor label="Claims" value={client.claims} onChange={claims => patch({ claims })} withValueType />
        <Toggle label="Always send client claims" hint="Also in tokens of user flows, not only client credentials." checked={client.alwaysSendClientClaims} onChange={alwaysSendClientClaims => patch({ alwaysSendClientClaims })} />
        <Field id="clientClaimsPrefix" label="Client claims prefix" value={client.clientClaimsPrefix} onChange={e => patch({ clientClaimsPrefix: e.target.value })} hint="Prepended to the claim types above." />
      </Section>
      <Section title="Properties">
        <KeyValueEditor label="Custom properties" value={client.properties} onChange={properties => patch({ properties })} />
      </Section>
    </div>
  );
}

function NullableNumber({ id, label, value, onChange, hint }: { id: string; label: string; value: number | null; onChange: (value: number | null) => void; hint?: string }) {
  return <Field id={id} label={label} type="number" min={0} value={value ?? ''} onChange={e => onChange(e.target.value === '' ? null : Math.max(0, Number(e.target.value) || 0))} hint={hint} />;
}
