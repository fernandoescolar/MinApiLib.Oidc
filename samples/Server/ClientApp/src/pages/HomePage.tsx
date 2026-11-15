import { Card, Code, Layout, Link, Muted } from '../components/ui';

export function HomePage() {
  return (
    <Layout title="Sample OIDC server">
      <Card>
        <p>MinApiLib.Oidc.Server running the sample configuration.</p>
        <ul className="list-disc space-y-1 pl-5">
          <li><Link href="/.well-known/openid-configuration">Discovery document</Link></li>
          <li><Link href="/.well-known/openid-configuration/jwks">Signing keys (JWKS)</Link></li>
          <li><Link href="/account/passkeys">Your passkeys</Link>: sign in with a password and add a passkey</li>
        </ul>
        <Muted>Users: <Code>alice</Code> / <Code>alice</Code> and <Code>bob</Code> / <Code>bob</Code>.</Muted>
      </Card>
    </Layout>
  );
}
