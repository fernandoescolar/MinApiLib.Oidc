import { Actions, Card, Code, Layout, Link, LinkButton, Muted } from '../components/ui';

interface HomeModel {
  passkeysUrl: string;
}

/** Signed out: the two clients of the server, and the passkey sign-in. */
export function HomePage({ model }: { model: HomeModel }) {
  return (
    <Layout title="Sample web client">
      <Card>
        <p>Sign in with the sample server. Both buttons sign in the same user; they use two clients of the server:</p>
        <ul className="list-disc space-y-1 pl-5">
          <li><strong>Bearer</strong>: the client <Code>web</Code> gets a plain bearer access token.</li>
          <li>
            <strong>DPoP</strong>: the client <Code>web.dpop</Code> proves possession of a key it creates for the session, and
            gets an access token bound to that key.
          </li>
        </ul>
        <Actions>
          <LinkButton href="/login?scheme=oidc" variant="primary">Sign in (Bearer)</LinkButton>
          <LinkButton href="/login?scheme=oidc-dpop" variant="primary">Sign in (DPoP)</LinkButton>
          <LinkButton href="/login?scheme=oidc-dpop&acr=phr">Sign in with a passkey (DPoP, acr_values=phr)</LinkButton>
        </Actions>
        <Muted>
          Users: alice / alice and bob / bob. Add a passkey on the server's <Link href={model.passkeysUrl}>passkeys page</Link> first.
        </Muted>
      </Card>
    </Layout>
  );
}
