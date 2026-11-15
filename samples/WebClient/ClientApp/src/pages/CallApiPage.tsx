import { Actions, Card, Layout, LinkButton, Pre } from '../components/ui';

interface Exchange {
  request: string;
  proof: { header: string; payload: string } | null;
  status: number;
  response: string;
}

interface CallApiModel {
  description: string;
  exchanges: Exchange[];
}

/** The HTTP requests the web client sent to the API, with the DPoP proofs decoded, and the API's answers. */
export function CallApiPage({ model }: { model: CallApiModel }) {
  const numbered = model.exchanges.length > 1;
  return (
    <Layout title="Calling the API">
      <Card>
        <p>{model.description}</p>
        <Actions><LinkButton href="/">Back</LinkButton></Actions>
      </Card>
      {model.exchanges.map((exchange, i) => {
        const suffix = numbered ? ` ${i + 1}` : '';
        return (
          <div key={i} className="space-y-6">
            <Card title={`Request${suffix}`}>
              <Pre>{exchange.request}</Pre>
              {exchange.proof && (
                <>
                  <p>The DPoP proof, decoded:</p>
                  <Pre>{exchange.proof.header}</Pre>
                  <Pre>{exchange.proof.payload}</Pre>
                </>
              )}
            </Card>
            <Card title={`Response${suffix}`} tone={exchange.status < 300 ? 'ok' : 'fail'}>
              <Pre>{exchange.response}</Pre>
            </Card>
          </div>
        );
      })}
    </Layout>
  );
}
