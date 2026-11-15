import { Actions, Card, Code, Layout, Link, LinkButton, Pre } from '../components/ui';

interface SessionModel {
  name: string;
  scheme: string;
  amr: string[];
  acr: string | null;
  tokenType: string;
  token: { header: string; payload: string };
  key: { publicJwk: string; thumbprint: string } | null;
  passkeysUrl: string;
}

/** Signed in: how the user signed in, the access token and, with DPoP, the session key and the attacks to try. */
export function SessionPage({ model }: { model: SessionModel }) {
  const dpop = model.tokenType.toLowerCase() === 'dpop';
  return (
    <Layout title={`Signed in as ${model.name}`}>
      <Card>
        <p>
          Signed in with the <strong>{model.scheme}</strong> scheme, by <Code>amr</Code> = <Code>{model.amr.join(', ')}</Code> and{' '}
          <Code>acr</Code> = <Code>{model.acr ?? '(none)'}</Code>. <Link href={model.passkeysUrl}>Your passkeys</Link> on the server.
        </p>
        <p>The token endpoint answered <Code>"token_type": "{model.tokenType}"</Code>.</p>
        <Actions>
          {dpop ? (
            <>
              <LinkButton href="/call-api" variant="primary">Call the API with a proof</LinkButton>
              <LinkButton href="/call-api?mode=bearer">Send the token as a bearer token</LinkButton>
              <LinkButton href="/call-api?mode=other-key">Sign the proof with another key</LinkButton>
              <LinkButton href="/call-api?mode=replay">Replay the proof</LinkButton>
            </>
          ) : (
            <LinkButton href="/call-api" variant="primary">Call the API</LinkButton>
          )}
          <LinkButton href="/logout">Sign out</LinkButton>
        </Actions>
      </Card>

      <Card title="Access token">
        <Pre>{model.token.header}</Pre>
        <Pre>{model.token.payload}</Pre>
      </Card>

      {model.key && (
        <Card title="Session key">
          <p>
            The public key, sent in every proof, and its thumbprint, which the access token carries in <Code>cnf.jkt</Code>. The
            authorization request sent the thumbprint too (<Code>dpop_jkt</Code>), which bound the code to the key:
          </p>
          <Pre>{model.key.publicJwk}</Pre>
          <Pre>jkt = {model.key.thumbprint}</Pre>
        </Card>
      )}
    </Layout>
  );
}
