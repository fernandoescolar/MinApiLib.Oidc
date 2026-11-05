[[go back to index]](../index.md#documentation-structure)

# Device Flow Interaction Service

The `IDeviceFlowInteractionService` interface (namespace `MinApiLib.Oidc.Server.Features.DeviceFlow`) provides the
services the host's device verification page uses to communicate with the server during device flow authorization
(RFC 8628). It is available from the dependency injection system.

The device shows the user the `verification_uri` (`UserInteractionOptions.DeviceVerificationUrl`, `/device` by
default) and a user code. The verification page signs the user in, reads the request with the user code, asks for
consent and completes the authorization:

```csharp
var request = await deviceInteraction.GetAuthorizationContextAsync(userCode);
if (request is null) return Results.BadRequest("Invalid user code.");

var result = await deviceInteraction.HandleRequestAsync(userCode, allowed
    ? new ConsentResponse { ScopesValuesConsented = request.ValidatedResources.RawScopeValues, RememberConsent = remember }
    : new ConsentResponse { Error = AuthorizationError.AccessDenied });
```

## IDeviceFlowInteractionService APIs

`GetAuthorizationContextAsync(string userCode)`
: Returns the `DeviceFlowAuthorizationRequest` for the user code, or `null` if the code or its client is unknown.

`HandleRequestAsync(string userCode, ConsentResponse consent)`
: Completes device authorization for the user code, for the signed-in user, with the scopes in
  `consent.ScopesValuesConsented`, which must all have been requested by the device. A consent with an `Error`, or
  with no consented scopes, denies the request: the device gets `access_denied` at the token endpoint and the result
  has `IsAccessDenied`. When the user grants it and the client has `AllowRememberConsent`, the consent is stored, as
  at the authorize endpoint, if `consent.RememberConsent` is set, and an earlier stored consent is removed if it is
  not. Returns a `DeviceFlowInteractionResult`.

Both methods count failed user code lookups: after `DeviceFlowOptions.UserCodeMaxFailedAttempts` failures (5 by
default) for a user, or for a client IP address when nobody is signed in, lookups fail until
`DeviceFlowOptions.UserCodeFailedAttemptsWindow` (5 minutes by default) passes. A limit of `0` turns the count off. See
[options](options.md#device-flow).

## DeviceFlowAuthorizationRequest

`Client`
: The client that initiated the request.

`ValidatedResources`
: The `ResourceValidationResult` with the scopes and resources the device requested. See
  [ResourceValidationResult](interactionservice.md#resourcevalidationresult).

## DeviceFlowInteractionResult

`IsError`
: Whether the authorization failed: unknown user code or client, no signed-in user, or consented scopes the device
  did not request.

`ErrorDescription`
: The error description upon failure.

`IsAccessDenied`
: Whether the user denied the request. The device gets `access_denied` when it next polls the token endpoint.

[[go back to index]](../index.md#documentation-structure)
