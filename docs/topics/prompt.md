[[go back to index]](../index.md#documentation-structure)

# Custom prompt values

The `prompt` parameter of the authorize endpoint tells the server which interaction the client wants. The built-in
values are `none`, `login`, `consent` and `select_account`. You can add your own, such as the `create` value of
[OpenID Connect Initiating User Registration](https://openid.net/specs/openid-connect-prompt-create-1_0.html) or an
application-specific `register`.

The accepted values are in `OidcServerOptions.UserInteraction.PromptValuesSupported`, which the discovery
document publishes as `prompt_values_supported`. A request with any other value fails with `invalid_request`, and
`none` still cannot be combined with other values.

## Sending the user to a page

For the common case, map a value to a page:

```csharp
builder.Services.AddOidcServer()
    .AddPromptRedirect("create", "/account/create");
```

A request with `prompt=create` redirects to `/account/create`. The page receives the authorize request as return URL
in the `returnUrl` query parameter (`UserInteraction.CustomRedirectReturnUrlParameter`), with the prompt removed, so
following it after the account is created and the user signed in continues the flow instead of showing the page
again. `IOidcServerInteractionService.GetAuthorizationContextAsync(returnUrl)` gives the page the client and the
rest of the request (see [Sign-in](signin.md)).

## Writing a handler

For more control, implement `IAuthorizePromptHandler` and register it for a value. It lives in the
`MinApiLib.Oidc.Server.Features.Authorize` namespace, with `InteractionResponse` and `ValidatedAuthorizeRequest`:

```csharp
public class RegisterPromptHandler : IAuthorizePromptHandler
{
    public Task<InteractionResponse> ProcessAsync(ValidatedAuthorizeRequest request, string prompt)
    {
        // Signed-in users skip registration and continue with the normal login and consent checks.
        if (request.Subject.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new InteractionResponse());
        }

        return Task.FromResult(new InteractionResponse { RedirectUrl = "/account/register" });
    }
}

builder.Services.AddOidcServer()
    .AddPromptHandler<RegisterPromptHandler>("register");
```

`AddPromptHandler` and `AddPromptRedirect` add the value to `PromptValuesSupported`. Handlers are resolved as keyed
services, keyed by the prompt value, and run after the [nonce pattern](../reference/nonce_pattern.md) and before the
login and consent checks:

- A response that needs interaction (`IsLogin`, `IsConsent`, `RedirectUrl` or `Error`) is returned as is, and the
  prompt is removed from the request.
- An empty `InteractionResponse` lets the request continue; only that value is removed from the prompt.

The built-in values cannot get a handler. A value added to `PromptValuesSupported` by hand without a handler is
accepted and ignored, with a warning in the log; a custom `IAuthorizeInteractionResponseGenerator` can act on it by
overriding `ProcessCustomPromptAsync` in a class derived from `AuthorizeInteractionResponseGenerator`, registered with
`AddAuthorizeInteractionResponseGenerator<T>()`.

[[go back to index]](../index.md#documentation-structure)
