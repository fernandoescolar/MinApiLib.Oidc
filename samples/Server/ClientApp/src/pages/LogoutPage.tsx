import { Card, Layout, Link } from '../components/ui';

interface LogoutModel {
  postLogoutRedirectUri: string | null;
  clientName: string | null;
  signOutIFrameUrl: string | null;
}

/**
 * The server has signed the user out. The hidden iframe runs front-channel logout, which signs the user out of
 * every client of the session; then the page offers the way back to the client.
 */
export function LogoutPage({ model }: { model: LogoutModel }) {
  return (
    <Layout title="Signed out">
      <Card>
        <p>You are signed out.</p>
        {model.postLogoutRedirectUri && (
          <p><Link href={model.postLogoutRedirectUri}>Return to {model.clientName ?? 'the application'}</Link></p>
        )}
      </Card>
      {model.signOutIFrameUrl && <iframe src={model.signOutIFrameUrl} title="Front-channel logout" className="hidden" />}
    </Layout>
  );
}
