[[go back to index]](../index.md#documentation-structure)

# Consent

During an authorization request, if IdentityServer requires user consent the browser will be redirected to the consent page.

Consent is used to allow an end user to grant a client access to resources ([identity](../reference/identity_resource.md#refidentityresource) or [API](../reference/api_resource.md#refapiresource)).
This is typically only necessary for third-party clients, and can be enabled/disabled per-client on the [client settings](../reference/client.md#refclient).

## Consent Page

In order for the user to grant consent, a consent page must be provided by the hosting application.
The [quickstart UI](https://github.com/IdentityServer/IdentityServer4.Quickstart.UI) has a basic implementation of a consent page.

A consent page normally renders the display name of the current user,
the display name of the client requesting access,
the logo of the client,
a link for more information about the client,
and the list of resources the client is requesting access to.
It’s also common to allow the user to indicate that their consent should be “remembered” so they are not prompted again in the future for the same client.

Once the user has provided consent, the consent page must inform IdentityServer of the consent, and then the browser must be redirected back to the authorization endpoint.

## Authorization Context

IdentityServer will pass a returnUrl parameter (configurable on the [user interaction options](../reference/options.md#refoptions)) to the consent page which contains the parameters of the authorization request.
These parameters provide the context for the consent page, and can be read with help from the [interaction service](../reference/interactionservice.md#refinteractionservice).
The `GetAuthorizationContextAsync` API will return an instance of `AuthorizationRequest`.

Additional details about the client or resources can be obtained using the `IClientStore` and `IResourceStore` interfaces.

## Informing IdentityServer of the consent result

The `GrantConsentAsync` API on the [interaction service](../reference/interactionservice.md#refinteractionservice) allows the consent page to inform IdentityServer of the outcome of consent (which might also be to deny the client access).

IdentityServer will temporarily persist the outcome of the consent.
This persistence uses a cookie by default, as it only needs to last long enough to convey the outcome back to the authorization endpoint.
This temporary persistence is different than the persistence used for the “remember my consent” feature (and it is the authorization endpoint which persists the “remember my consent” for the user).
If you wish to use some other persistence between the consent page and the authorization redirect, then you can implement `IMessageStore<ConsentResponse>` and register the implementation in DI.

## Returning the user to the authorization endpoint

Once the consent page has informed IdentityServer of the outcome, the user can be redirected back to the returnUrl.
Your consent page should protect against open redirects by verifying that the returnUrl is valid.
This can be done by calling `IsValidReturnUrl` on the [interaction service](../reference/interactionservice.md#refinteractionservice).
Also, if `GetAuthorizationContextAsync` returns a non-null result, then you can also trust that the returnUrl is valid.

[[go back to index]](../index.md#documentation-structure)
