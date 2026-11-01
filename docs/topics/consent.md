[[go back to index]](../index.md#documentation-structure)

# Consent

When a client requires consent, the authorize endpoint redirects the browser to the consent page during the
authorization request.

Consent lets the user grant a client access to resources ([identity](../reference/identity_resource.md) or
[API](../reference/api_resource.md)). It is usually only needed for third-party clients, and is turned on per client
with `Client.RequireConsent` (off by default; see the [client settings](../reference/client.md)).
`Client.AllowRememberConsent` lets the user's decision be remembered, and `Client.ConsentLifetime` limits how long.

## Consent page

The host writes the consent page; the server has none, and the [samples](../../samples/README.md) do not need one
because their clients do not require consent.

A consent page usually shows the user's name, the client's display name, logo and a link with more information about
it (`ClientName`, `LogoUri`, `ClientUri`), and the resources the client asks for. It often lets the user choose to
remember the decision so the server does not ask again for the same client.

Once the user has decided, the page tells the server and redirects the browser back to the authorize endpoint.

## Authorization context

The server redirects to `UserInteraction.ConsentUrl` (default `/consent`) with the authorize request in the
`UserInteraction.ConsentReturnUrlParameter` query parameter (default `returnUrl`); see the
[options](../reference/options.md). `IOidcServerInteractionService.GetAuthorizationContextAsync(returnUrl)` reads
it and returns an `AuthorizationRequest`, with the client and the requested resources in `ValidatedResources`. See
the [interaction service](../reference/interactionservice.md).

`IClientStore` and `IResourceStore` give more details about the client or the resources.

## Informing the server of the consent result

`GrantConsentAsync` on the interaction service records the outcome: the scopes the user granted
(`ConsentResponse.ScopesValuesConsented`), whether to remember the decision (`RememberConsent`), or a denial
(`Error`, for example `AuthorizationError.AccessDenied`). `DenyAuthorizationAsync` is a shortcut for a denial:

```csharp
var context = await interaction.GetAuthorizationContextAsync(returnUrl);
if (context == null)
{
    return Results.Redirect("/home/error");
}

if (allowed)
{
    await interaction.GrantConsentAsync(context, new ConsentResponse
    {
        ScopesValuesConsented = scopes,
        RememberConsent = remember
    });
}
else
{
    await interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
}

return Results.Redirect(returnUrl);
```

The server keeps the outcome only until the browser is back at the authorize endpoint, in a cookie by default
(`IConsentMessageStore`). That is not the remembered consent: the authorize endpoint stores that one, as a persisted
grant, when the user asked to remember the decision.

## Returning the user to the authorization endpoint

After informing the server, the page redirects the browser to the return URL. Protect against open redirects: when
`GetAuthorizationContextAsync` returns a context the return URL is valid, and `IsValidReturnUrl` checks it without
loading the context.

[[go back to index]](../index.md#documentation-structure)
