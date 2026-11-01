[[go back to index]](../index.md#documentation-structure)

# Resource Owner Password Validation

#### WARNING
The resource owner password credentials grant is deprecated by OAuth 2.1 and RFC 9700 (OAuth 2.0 Security Best
Current Practice): the client sees the user's password and the flow cannot use multi-factor authentication or
federation. Use the authorization code flow with PKCE, or the device flow for devices without a browser.

The grant is off by default. A client can use it only when it lists `GrantType.ResourceOwnerPassword` (for example
`GrantTypes.ResourceOwnerPassword`) **and** sets `AllowResourceOwnerPasswordFlow = true`:

```csharp
new Client
{
    ClientId = "legacy.app",
    ClientSecrets = { new Secret("secret".Sha256()) },
    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
    AllowResourceOwnerPasswordFlow = true,
    AllowedScopes = { "api1" }
};
```

A client that lists the grant without the flag fails configuration validation. The grant is not published in the
discovery document, and the `password` parameter is removed from the logs.

To use the grant you also need to implement the `IResourceOwnerPasswordValidator` interface (namespace
`MinApiLib.Oidc.Server.Features.Tokens.Grants`) over your user store and register it with
`AddResourceOwnerValidator<T>()`. Without one, the default `NotSupportedResourceOwnerPasswordValidator` makes the
token endpoint answer `unsupported_grant_type`.

```csharp
public interface IResourceOwnerPasswordValidator
{
    /// <summary>
    /// Validates the resource owner password credential
    /// </summary>
    /// <param name="context">The context.</param>
    Task ValidateAsync(ResourceOwnerPasswordValidationContext context);
}
```

On the context you will find the already parsed `UserName` and `Password`, and the validated request (`Request`,
with the raw parameters in `Request.Raw`) if you want to look at other input data.

Your job is then to validate the password and set the `Result` on the context. See the
[GrantValidationResult](../reference/grant_validation_result.md) documentation. For example, over ASP.NET Core
Identity, counting failed attempts towards lockout:

```csharp
public class IdentityPasswordValidator(UserManager<AppUser> users) : IResourceOwnerPasswordValidator
{
    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var user = await users.FindByNameAsync(context.UserName);
        if (user is null || await users.IsLockedOutAsync(user))
        {
            return; // the default Result is invalid_grant
        }

        if (!await users.CheckPasswordAsync(user, context.Password))
        {
            await users.AccessFailedAsync(user);
            return;
        }

        await users.ResetAccessFailedCountAsync(user);
        context.Result = new GrantValidationResult(user.Id, OidcConstants.AuthenticationMethods.Password);
    }
}
```

The server raises a `UserLoginSuccessEvent` or `UserLoginFailureEvent` for each request (see [Events](events.md#refevents)).

[[go back to index]](../index.md#documentation-structure)
