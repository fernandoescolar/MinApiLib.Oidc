[[go back to index]](../index.md#documentation-structure)

<a id="refprofileservice"></a>

# Profile Service

The server often needs identity information about users: when it creates identity and access tokens, and when it
answers the userinfo endpoint. By default, it only has the claims in the authentication cookie to draw upon.

It is impractical to put every claim a client may need into the cookie, so the server defines an extension point to
load claims as needed: `IProfileService` (namespace `MinApiLib.Oidc.Server.Core.Users`). Implement it to read the
identity data from your own user store, database or API, and register it with `AddProfileService<T>()`.

The default implementation, `DefaultProfileService`, issues the requested claims that are in the cookie and treats
every user as active. Its methods are virtual, so you can derive from it.

## IProfileService APIs

`GetProfileDataAsync`
: Loads claims for a user. It is called whenever claims about the user are requested: during token creation and
  at the userinfo endpoint. It is passed an instance of `ProfileDataRequestContext`.

`IsActiveAsync`
: Indicates if a user is currently allowed to obtain tokens (for example, the account has not been disabled since
  the user signed in). It is called during token issuance and validation: at the authorize endpoint, when a code,
  refresh token, device code or extension grant is redeemed, for the password grant, and when access tokens are
  validated (userinfo, introspection, nonce). It is passed an instance of `IsActiveContext`.

## Example

`IUserRepository` and `AppUser` stand for the host's own user store:

```csharp
public sealed class UserProfileService(IUserRepository users) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var user = await users.FindBySubjectAsync(context.Subject.GetSubjectId());
        if (user is null) return;

        context.AddRequestedClaims(
        [
            new Claim(JwtClaimTypes.Name, user.DisplayName),
            new Claim(JwtClaimTypes.Email, user.Email),
            new Claim(JwtClaimTypes.Role, user.Role)
        ]);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var user = await users.FindBySubjectAsync(context.Subject.GetSubjectId());
        context.IsActive = user is { IsEnabled: true };
    }
}
```

```csharp
builder.Services.AddOidcServer()
    // ...
    .AddProfileService<UserProfileService>();
```

`JwtClaimTypes` is in `MinApiLib.Oidc.Server.Core.IdentityModel`.

## ProfileDataRequestContext

Models the request for user claims and is the vehicle to return those claims. It contains these properties:

`Subject`
: The `ClaimsPrincipal` modeling the user.

`Client`
: The `Client` for which the claims are being requested.

`RequestedClaimTypes`
: The collection of claim types being requested.

`Caller`
: An identifier for the context in which the claims are being requested. The constants of
  `OidcServerConstants.ProfileDataCallers` are its values: `ClaimsProviderIdentityToken`,
  `ClaimsProviderAccessToken` and `UserInfoEndpoint`.

`RequestedResources`
: The validated resources (`ResourceValidationResult`) of the request, if available.

`ValidatedRequest`
: The validated protocol request (`ValidatedRequest`, namespace `MinApiLib.Oidc.Server.Core.Validation`), if
  available.

`IssuedClaims`
: The list of `Claim`s that will be returned. The `IProfileService` implementation is expected to populate it.

Two extension methods help to populate it:

`AddRequestedClaims`
: Filters the claims by `RequestedClaimTypes`, then adds them to `IssuedClaims`.

`FilterClaims`
: Returns the claims whose type is in `RequestedClaimTypes`.

## Requested scopes and claims mapping

The scopes requested by the client control which user claims are returned in the tokens to the client.
`GetProfileDataAsync` is responsible for obtaining those claims, based on the `RequestedClaimTypes` collection of the
`ProfileDataRequestContext`.

`RequestedClaimTypes` is populated from the user claims defined on the [resources](../topics/resources.md) that
model the scopes. For an identity token, it holds the user claim types of the requested
[identity resources](identity_resource.md). For an access token, it holds the user claim types of the requested
[API scopes](api_scope.md) and of their [API resources](api_resource.md).

## IsActiveContext

Models the request to determine if the user is currently allowed to obtain tokens. It contains these properties:

`Subject`
: The `ClaimsPrincipal` modeling the user.

`Client`
: The `Client` for which the check is made.

`Caller`
: An identifier for the context in which the check is made. The constants of
  `OidcServerConstants.ProfileIsActiveCallers` are its values: `AuthorizeEndpoint`, `IdentityTokenValidation`,
  `AccessTokenValidation`, `ExtensionGrantValidation`, `RefreshTokenValidation`, `AuthorizationCodeValidation`,
  `UserInfoRequestValidation`, `DeviceCodeValidation` and `ResourceOwnerValidation`.

`IsActive`
: Whether the user is allowed to obtain tokens. Defaults to `true`; the `IProfileService` implementation sets it.

[[go back to index]](../index.md#documentation-structure)
