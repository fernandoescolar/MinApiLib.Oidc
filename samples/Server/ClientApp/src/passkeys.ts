// The browser side of passkeys (WebAuthn): ask the server for options, let the browser create or use a passkey, and
// return its answer as JSON for a form to post. It uses the JSON helpers of WebAuthn Level 3
// (parseRequestOptionsFromJSON, parseCreationOptionsFromJSON, toJSON).

export function passkeysSupported(): boolean {
  return typeof PublicKeyCredential !== 'undefined'
    && typeof PublicKeyCredential.parseRequestOptionsFromJSON === 'function'
    && typeof PublicKeyCredential.parseCreationOptionsFromJSON === 'function';
}

export async function conditionalMediationAvailable(): Promise<boolean> {
  return passkeysSupported() && (await PublicKeyCredential.isConditionalMediationAvailable?.()) === true;
}

/** Signs in with a passkey of this server. The form carries the antiforgery token the options endpoint checks. */
export async function getPasskey(form: HTMLFormElement, mediation: CredentialMediationRequirement, signal: AbortSignal): Promise<string> {
  const options = await postForm('/account/passkey/request-options', form);
  const publicKey = PublicKeyCredential.parseRequestOptionsFromJSON(options);
  const credential = await navigator.credentials.get({ publicKey, mediation, signal });
  return JSON.stringify(credential);
}

/** Creates a passkey for the signed-in user. */
export async function createPasskey(form: HTMLFormElement): Promise<string> {
  const options = await postForm('/account/passkey/creation-options', form);
  const publicKey = PublicKeyCredential.parseCreationOptionsFromJSON(options);
  const credential = await navigator.credentials.create({ publicKey });
  return JSON.stringify(credential);
}

/** What to tell the user about a failed ceremony, or null when it was aborted on purpose. */
export function describeFailure(error: unknown, cancelled: string): string | null {
  if (error instanceof DOMException && error.name === 'AbortError') return null;
  if (error instanceof DOMException && error.name === 'NotAllowedError') return cancelled;
  return error instanceof Error ? error.message : String(error);
}

async function postForm(url: string, form: HTMLFormElement) {
  const response = await fetch(url, { method: 'POST', body: new FormData(form) });
  if (!response.ok) throw new Error(`The server answered ${response.status}.`);
  return response.json();
}
