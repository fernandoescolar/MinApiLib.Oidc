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

var result = await deviceInteraction.HandleRequestAsync(userCode, new ConsentResponse
{
    ScopesValuesConsented = allowed ? request.ValidatedResources.RawScopeValues : []
});
```

## IDeviceFlowInteractionService APIs

`GetAuthorizationContextAsync(string userCode)`
: Returns the `DeviceFlowAuthorizationRequest` for the user code, or `null` if the code or its client is unknown.

`HandleRequestAsync(string userCode, ConsentResponse consent)`
: Completes device authorization for the user code, for the signed-in user, with the scopes in
  `consent.ScopesValuesConsented`, which must all have been requested by the device. With no consented scopes, the
  device gets `access_denied` at the token endpoint. Returns a `DeviceFlowInteractionResult`.

Both methods count failed user code lookups: after `DeviceFlowOptions.UserCodeMaxFailedAttempts` failures (5 by
default) for a user, or for a client IP address when nobody is signed in, lookups fail until
`DeviceFlowOptions.UserCodeFailedAttemptsWindow` (5 minutes by default) passes. See [options](options.md#device-flow).

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
: Whether access was denied. The default implementation does not set it.

[[go back to index]](../index.md#documentation-structure)
