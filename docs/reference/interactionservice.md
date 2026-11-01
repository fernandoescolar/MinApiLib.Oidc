[[go back to index]](../index.md#documentation-structure)

<a id="refinteractionservice"></a>

# Interaction Service

The `IOidcServerInteractionService` interface (namespace `MinApiLib.Oidc.Server.Features.Interaction`) provides the
services the host's UI pages (login, consent, logout, error) use to communicate with the server. It is available
from the dependency injection system. In a MinApiLib endpoint, add it to the request with `[FromServices]`, as the
login, logout and error pages of [samples/Server](../../samples/Server) do.

A consent page, for example, reads the request from its return URL and records the user's decision:

```csharp
var context = await interaction.GetAuthorizationContextAsync(returnUrl);
if (context is null) return Results.BadRequest();

if (allowed)
{
    await interaction.GrantConsentAsync(context, new ConsentResponse
    {
        ScopesValuesConsented = context.ValidatedResources.RawScopeValues,
        RememberConsent = remember
    });
}
else
{
    await interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
}

return Results.Redirect(returnUrl);
```

## IOidcServerInteractionService APIs

`GetAuthorizationContextAsync(string returnUrl)`
: Returns the `AuthorizationRequest` for the `returnUrl` passed to the login or consent pages, or `null` if the URL
  is not an authorize request of this server.

`IsValidReturnUrl(string returnUrl)`
: Indicates if the `returnUrl` is a valid URL to redirect to after login or consent.

`GetErrorContextAsync(string errorId)`
: Returns the `ErrorMessage` for the `errorId` passed to the error page, or `null`.

`GetLogoutContextAsync(string logoutId)`
: Returns the `LogoutRequest` for the `logoutId` passed to the logout page.

`CreateLogoutContextAsync()`
: Creates a `logoutId` when there is none. It stores the state needed for sign-out (the user, the session and the
  clients the user signed in to) and returns the id that identifies it, or `null` if the user has no session with
  any client. Use it when the logout page has no `logoutId` and must capture this state before it redirects to an
  external identity provider to sign out. Round-trip the new `logoutId` through the external provider, then use it
  on the sign-out callback page as the logout page would.

`GrantConsentAsync(AuthorizationRequest request, ConsentResponse consent, string? subject = null)`
: Informs the server of the user's consent to an `AuthorizationRequest`. Without `subject`, it uses the signed-in
  user; it throws if there is none and consent was granted.

`DenyAuthorizationAsync(AuthorizationRequest request, AuthorizationError error, string? errorDescription = null)`
: Makes the server return an error to the client for an `AuthorizationRequest`. A helper on top of
  `GrantConsentAsync`.

`GetAllUserGrantsAsync()`
: Returns the `Grant`s of the signed-in user: their consents and the clients' access to their resources.

`RevokeUserConsentAsync(string clientId)`
: Revokes all of the signed-in user's consents and grants for a client.

`RevokeTokensForCurrentSessionAsync()`
: Revokes all of the signed-in user's consents and grants for the clients they signed in to during their current
  session.

## AuthorizationRequest

Contextual information about an authorize request.

`Client`
: The client that initiated the request.

`RedirectUri`
: The URI to redirect the user to after successful authorization.

`DisplayMode`
: The display mode passed in the authorize request.

`UiLocales`
: The UI locales passed in the authorize request.

`IdP`
: The external identity provider requested, to bypass home realm discovery (HRD). It comes from the `idp:` prefix
  in the `acr_values` parameter of the authorize request.

`Tenant`
: The tenant requested. It comes from the `tenant:` prefix in the `acr_values` parameter of the authorize request.

`LoginHint`
: The expected username the user will use to sign in, from the `login_hint` parameter of the authorize request.

`PromptModes`
: The prompt modes requested in the authorize request.

`AcrValues`
: The `acr_values` passed in the authorize request, without the `idp:` and `tenant:` values.

`ValidatedResources`
: The `ResourceValidationResult` with the validated resources of the authorize request.

`Parameters`
: The entire parameter collection passed to the authorize request.

`RequestObjectValues`
: The validated contents of the request object, if present.

## ResourceValidationResult

Namespace `MinApiLib.Oidc.Server.Core.ResourceValidation`.

`Resources`
: The identity resources, API resources and API scopes of the result.

`ParsedScopes`
: The parsed scopes represented by the result.

`RawScopeValues`
: The original (raw) scope values represented by the result.

`InvalidScopes`
: The requested scopes that are invalid.

`InvalidResourceIndicators`
: The requested resource indicators that do not match an API resource of the requested scopes.

`Succeeded`
: Whether validation succeeded: there are parsed scopes and no invalid scopes or resource indicators.

`Filter(IEnumerable<string> scopeValues)`
: Returns a new result limited to the given scope values.

## ErrorMessage

The data for the error page.

`Error`
: The error code.

`ErrorDescription`
: The error description.

`RequestId`
: The per-request identifier. It can be shown to the end user and used in diagnostics.

`ClientId`
: The client that made the request, if available.

`RedirectUri`
: The redirect URI of the request.

`ResponseMode`
: The response mode of the request.

`DisplayMode`
: The display mode passed in the authorize request.

`UiLocales`
: The UI locales passed in the authorize request.

## LogoutRequest

The request from a client to sign the user out (namespace `MinApiLib.Oidc.Server.Features.Session`).

`ClientId`
: The client that initiated the request.

`ClientName`
: The name of that client.

`PostLogoutRedirectUri`
: The URL to redirect the user to after they have signed out.

`ClientAuthenticated`
: Whether the client was established by a valid `id_token_hint` rather than by a bare `client_id`.

`SubjectId`
: The user's subject id at logout time.

`SessionId`
: The user's session id at logout time.

`ClientIds`
: The clients known to have a session for the user at logout time.

`SignOutIFrameUrl`
: The URL to render in an `<iframe>` on the signed-out page to sign the user out of the clients (front-channel
  logout).

`Parameters`
: The entire parameter collection passed to the end session endpoint.

`ShowSignoutPrompt`
: Whether the user should be asked to confirm the sign-out. The prompt is skipped only when the request carried a
  valid `id_token_hint`; any site can forge a bare `client_id`.

## ConsentResponse

The user's response to the consent screen.

`ScopesValuesConsented`
: The scopes the user consented to.

`RememberConsent`
: Whether the user's consent is to be remembered.

`Description`
: Optional description the user can set for the grant (e.g. the name of the device being used when consent is
  given). It is returned in `Grant.Description`. See [persisted grants](../topics/persisted_grants.md).

`Error`
: The `AuthorizationError`, if any, for the consent response. It is returned to the client in the authorization
  response. Its values are `AccessDenied`, `InteractionRequired`, `LoginRequired`, `AccountSelectionRequired` and
  `ConsentRequired`.

`ErrorDescription`
: The error description. It is returned to the client in the authorization response.

`Granted`
: Whether consent was granted: there are consented scopes and no error.

## Grant

A grant the user has given.

`SubjectId`
: The subject id that allowed the grant.

`ClientId`
: The client the grant is for.

`Description`
: The description the user assigned to the grant, for example the name of the device or application.

`Scopes`
: The scopes granted.

`CreationTime`
: When the grant was given.

`Expiration`
: When the grant expires, or `null`.

[[go back to index]](../index.md#documentation-structure)
