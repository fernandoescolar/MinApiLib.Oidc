namespace Samples.Server.Passkeys;

/// <summary>
/// The browser side of passkeys (WebAuthn): ask the server for options, let the browser create or use a passkey, and
/// post its answer back in a form. It uses the JSON helpers of WebAuthn Level 3
/// (<c>parseRequestOptionsFromJSON</c>, <c>parseCreationOptionsFromJSON</c>, <c>toJSON</c>).
/// </summary>
public static class PasskeyScripts
{
    /// <summary>
    /// For <c>#passkey-form</c>: the button signs in with a passkey; on load, the browser also offers the user's
    /// passkeys in the user name field when it supports conditional UI.
    /// </summary>
    public const string SignIn = """
        <script type="module">
          const form = document.getElementById('passkey-form');
          const button = document.getElementById('passkey-button');
          const status = document.getElementById('passkey-status');
          let pending;

          async function signIn(mediation) {
            pending?.abort();
            pending = new AbortController();
            const response = await fetch('/account/passkey/request-options', { method: 'POST', body: new FormData(form) });
            if (!response.ok) throw new Error(`The server answered ${response.status}.`);
            const publicKey = PublicKeyCredential.parseRequestOptionsFromJSON(await response.json());
            const credential = await navigator.credentials.get({ publicKey, mediation, signal: pending.signal });
            form.credential.value = JSON.stringify(credential);
            form.submit();
          }

          function report(error) {
            if (error.name === 'AbortError') return;
            status.textContent = error.name === 'NotAllowedError' ? 'The passkey sign-in was cancelled.' : error.message;
          }

          if (!window.PublicKeyCredential?.parseRequestOptionsFromJSON) {
            button.disabled = true;
            status.textContent = 'This browser does not support passkeys.';
          } else {
            button.addEventListener('click', () => signIn('optional').catch(report));
            if (await PublicKeyCredential.isConditionalMediationAvailable?.()) signIn('conditional').catch(report);
          }
        </script>
        """;

    /// <summary>For <c>#add-passkey-form</c>: the button creates a passkey for the signed-in user.</summary>
    public const string Register = """
        <script type="module">
          const form = document.getElementById('add-passkey-form');
          const button = document.getElementById('add-passkey-button');
          const status = document.getElementById('passkey-status');

          async function register() {
            const response = await fetch('/account/passkey/creation-options', { method: 'POST', body: new FormData(form) });
            if (!response.ok) throw new Error(`The server answered ${response.status}.`);
            const publicKey = PublicKeyCredential.parseCreationOptionsFromJSON(await response.json());
            const credential = await navigator.credentials.create({ publicKey });
            form.credential.value = JSON.stringify(credential);
            form.submit();
          }

          if (!window.PublicKeyCredential?.parseCreationOptionsFromJSON) {
            button.disabled = true;
            status.textContent = 'This browser does not support passkeys.';
          } else {
            button.addEventListener('click', () => register().catch(error => {
              status.textContent = error.name === 'NotAllowedError' ? 'The passkey was not created.' : error.message;
            }));
          }
        </script>
        """;
}
