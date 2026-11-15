import { Alert, Card, Layout, Muted } from '../components/ui';

interface ErrorModel {
  error: string | null;
  errorDescription: string | null;
  requestId: string | null;
}

/** Protocol errors that cannot be sent back to the client, such as an unknown redirect URI. */
export function ErrorPage({ model }: { model: ErrorModel }) {
  return (
    <Layout title="Error">
      <Card>
        <Alert kind="error"><strong className="font-mono">{model.error ?? 'unknown_error'}</strong></Alert>
        {model.errorDescription && <p>{model.errorDescription}</p>}
        <Muted>Request id: {model.requestId ?? '(none)'}</Muted>
      </Card>
    </Layout>
  );
}
