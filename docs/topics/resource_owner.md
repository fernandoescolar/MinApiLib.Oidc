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

To use the grant you also need to implement and register the `IResourceOwnerPasswordValidator` interface with
`AddResourceOwnerValidator<T>()`. `AddAspNetIdentity<TUser>()` registers one for ASP.NET Core Identity, which counts
failed attempts towards lockout. Without a validator the token endpoint answers `unsupported_grant_type`.

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

On the context you will find already parsed protocol parameters like `UserName` and `Password`, but also the raw request if you want to look at other input data.

Your job is then to implement the password validation and set the `Result` on the context accordingly. See the [GrantValidationResult](../reference/grant_validation_result.md#refgrantvalidationresult) documentation.

[[go back to index]](../index.md#documentation-structure)
