[[go back to index]](../index.md#documentation-structure)

# Custom Token Request Validation and Issuance

You can run custom code as part of the token issuance pipeline at the [token endpoint](../endpoints/token.md). This
allows e.g. for

* adding validation logic
* changing certain parameters (e.g. token lifetime) dynamically

For this purpose, implement the `ICustomTokenRequestValidator` interface (namespace
`MinApiLib.Oidc.Server.Features.Tokens`):

```csharp
/// <summary>
/// Allows inserting custom validation logic into token requests
/// </summary>
public interface ICustomTokenRequestValidator
{
    /// <summary>
    /// Custom validation logic for a token request.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>
    /// The validation result
    /// </returns>
    Task ValidateAsync(CustomTokenRequestValidationContext context);
}
```

The server calls it for every grant type, after the grant's own validation succeeded. The context's `Result`
(a `TokenRequestValidationResult`) gives you access to:

* the validated request (`Result.ValidatedRequest`): the client, the grant type, the raw parameters, the subject,
  the requested scopes and resources, and the parameters of the tokens to issue, e.g. `AccessTokenLifetime`,
  `AccessTokenType`, `ClientClaims` and `Confirmation`, which you can change;
* custom response parameters, in `Result.CustomResponse`;
* an error and error description: replace `Result` with an error result.

For example:

```csharp
public class TenantTokenRequestValidator : ICustomTokenRequestValidator
{
    public Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
        var request = context.Result.ValidatedRequest;

        var tenant = request.Raw.Get("tenant");
        if (tenant is null)
        {
            context.Result = new TokenRequestValidationResult(request, "invalid_request", "tenant is required");
            return Task.CompletedTask;
        }

        // shorter tokens for machine clients
        if (request.GrantType == GrantType.ClientCredentials)
        {
            request.AccessTokenLifetime = 600;
        }

        request.ClientClaims.Add(new Claim("tenant", tenant));
        context.Result.CustomResponse = new Dictionary<string, object> { ["tenant"] = tenant };

        return Task.CompletedTask;
    }
}
```

The error is returned to the client as a `400` with `error`, `error_description` and the custom response parameters.
Client claims go into the access token with the client's `ClientClaimsPrefix` (`client_` by default), for tokens
without a user or when the client has `AlwaysSendClientClaims`.

Register your implementation with the `AddCustomTokenRequestValidator<T>()` method of the builder:

```csharp
builder.Services.AddOidcServer()
    .AddCustomTokenRequestValidator<TenantTokenRequestValidator>();
```

[[go back to index]](../index.md#documentation-structure)
