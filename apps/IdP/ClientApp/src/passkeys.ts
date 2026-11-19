// The browser side of passkeys (WebAuthn): ask the server for options, let the browser create or use a passkey, and
// return its answer as JSON. It uses the JSON helpers of WebAuthn Level 3.

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
  const response = await fetch('/login/passkey/options', { method: 'POST', body: new FormData(form) });
  if (!response.ok) throw new Error(`The server answered ${response.status}.`);
  const publicKey = PublicKeyCredential.parseRequestOptionsFromJSON(await response.json());
  const credential = await navigator.credentials.get({ publicKey, mediation, signal });
  return JSON.stringify(credential);
}

/** Creates a passkey for the signed-in user, from the options the account API gives. */
export async function createPasskey(options: unknown): Promise<string> {
  const publicKey = PublicKeyCredential.parseCreationOptionsFromJSON(options as PublicKeyCredentialCreationOptionsJSON);
  const credential = await navigator.credentials.create({ publicKey });
  return JSON.stringify(credential);
}

/** What to tell the user about a failed ceremony, or null when it was aborted on purpose. */
export function describeFailure(error: unknown, cancelled: string): string | null {
  if (error instanceof DOMException && error.name === 'AbortError') return null;
  if (error instanceof DOMException && error.name === 'NotAllowedError') return cancelled;
  return error instanceof Error ? error.message : String(error);
}
