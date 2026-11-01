[[go back to index]](../index.md#documentation-structure)

<a id="refgrantvalidationresult"></a>

# GrantValidationResult

The `GrantValidationResult` class (namespace `MinApiLib.Oidc.Server.Features.Tokens`) models the outcome of grant
validation for [extension grants](../topics/extension_grants.md) (`IExtensionGrantValidator`) and
[resource owner password grants](../topics/resource_owner.md) (`IResourceOwnerPasswordValidator`). The validator
assigns it to `context.Result`.

The most common usage is to create it from a subject id (success case):

```csharp
context.Result = new GrantValidationResult(
    subject: "818727",
    authenticationMethod: "custom",
    claims: optionalClaims);
```

This builds a principal with the `sub`, `amr`, `idp` (`local` unless you pass `identityProvider`) and `auth_time`
(now, unless you pass `authTime`) claims, plus the claims you pass. Those extra claims stay in the principal; to put
them in tokens, return them from your [profile service](profileservice.md).

Or from an error and description (failure case):

```csharp
context.Result = new GrantValidationResult(
    TokenRequestErrors.InvalidGrant,
    "invalid custom credential");
```

`TokenRequestErrors` has `InvalidRequest`, `InvalidClient`, `InvalidGrant`, `UnauthorizedClient`,
`UnsupportedGrantType`, `InvalidScope` and `InvalidTarget`.

The other constructors take a `ClaimsPrincipal` (it must have a single identity with the `sub`, `idp`, `amr` and
`auth_time` claims, or the constructor throws), or nothing at all: the access token then only carries the client's
identity.

Every constructor takes an optional `customResponse` dictionary, whose entries are added to the token response.

## Properties

`Subject`
: The `ClaimsPrincipal` that represents the user, or `null` when there is none.

`CustomResponse`
: Custom fields for the token response.

`IsError`, `Error`, `ErrorDescription`
: Inherited from `ValidationResult` (namespace `MinApiLib.Oidc.Server.Core.Validation`): whether validation failed,
  and the error code and description sent to the client.

[[go back to index]](../index.md#documentation-structure)
