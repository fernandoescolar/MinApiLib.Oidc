[[go back to index]](../index.md#documentation-structure)

<a id="refpersistedgrants"></a>

# Persisted Grants

Many grant types require persistence. These include authorization codes, refresh tokens, reference tokens,
remembered user consents, pushed authorization requests and authorization nonces. The server keeps them in a common
store, the persisted grant store, an implementation of `IPersistedGrantStore` (namespace
`MinApiLib.Oidc.Server.Core.Stores`). The device codes of the device flow live in a separate store,
`IDeviceFlowStore`.

## Stores

**In memory.** `AddOidcServer` registers `InMemoryPersistedGrantStore` and an in-memory `IDeviceFlowStore`
(`AddInMemoryPersistedGrants`). They suit development and tests: the grants are lost on restart and are not shared
between instances. Storing a grant or a device code also removes the expired ones, at most once a minute. The server
logs a warning when it runs with the in-memory grant store outside the Development environment.

**MongoDB.** The `MinApiLib.Oidc.Server.MongoDB` package stores grants in its `persistedGrants` collection and device
codes in `deviceCodes`, with TTL indexes on `Expiration`, so MongoDB deletes expired entries:

```csharp
builder.Services.AddOidcServer()
    .AddMongoStores(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Mongo");
        options.UseForConfiguration = false;   // keep clients and resources elsewhere
    });
```

See [its README](../../src/MinApiLib.Oidc.Server.MongoDB/README.md) and the [MongoDB quickstart](../quickstarts/5_mongodb.md).

**Your own.** Implement `IPersistedGrantStore` (and `IDeviceFlowStore` for the device flow) and register it with
`AddPersistedGrantStore<T>()` (and `AddDeviceFlowStore<T>()`):

```csharp
public interface IPersistedGrantStore
{
    Task StoreAsync(PersistedGrant grant);
    Task<PersistedGrant?> GetAsync(string key);
    Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter);
    Task RemoveAsync(string key);
    Task RemoveAllAsync(PersistedGrantFilter filter);
}
```

`PersistedGrantFilter` selects grants by `SubjectId`, `SessionId`, `ClientId` and `Type`. Keep expired grants from
piling up, for example with a periodic clean-up job or the database's own expiration.

## Persisted Grant

The persisted grant is the data type that maintains the values for a grant.
It has these properties:

`Key`
: The unique identifier for the persisted grant in the store. It is a SHA-256 hash of the handle the client holds and
  the grant type, so the store never holds usable handles.

`Type`
: The type of the grant, one of `OidcServerConstants.PersistedGrantTypes`: `authorization_code`, `reference_token`,
  `refresh_token`, `user_consent`, `authorization_nonce` or `pushed_authorization_request`.

`SubjectId`
: The subject id to which the grant belongs.

`SessionId`
: The session id of the user's session at the server, if any.

`ClientId`
: The client identifier for which the grant was created.

`Description`
: The description the user assigned to the grant or device being authorized.

`CreationTime`
: The date/time the grant was created.

`Expiration`
: The expiration of the grant.

`ConsumedTime`
: The date/time the grant was “consumed” (see below).

`Data`
: The grant specific serialized data.

#### NOTE
The `Data` property contains a copy of all of the values (and more) and is considered authoritative by the server,
thus the above values, by default, are considered informational and read-only.

The presence of the record in the store without a `ConsumedTime` and while still within the `Expiration` represents the validity of the grant.
Setting either of these two values, or removing the record from the store effectively revokes the grant.

## Grant Consumption

Some grant types are one-time use only (either by definition or configuration).
Once they are “used”, rather than deleting the record, the `ConsumedTime` value is set in the store, marking them as having been used.
This “soft delete” allows for custom implementations to either have flexibility in allowing a grant to be re-used (typically within a short window of time),
or to be used in risk assessment and threat mitigation scenarios (where suspicious activity is detected) to revoke access.
For refresh tokens, this sort of custom logic would be performed in the `IRefreshTokenService` (see [Refresh Tokens](refresh_tokens.md)).

## Persisted Grant Service

Working with the grants store directly might be too low level.
As such, a higher level service called `IPersistedGrantService` (namespace `MinApiLib.Oidc.Server.Features.Interaction`) is provided.
It abstracts and aggregates the different grant types into one concept, and allows querying and revoking the persisted grants for a user.

It contains these APIs:

`GetAllGrantsAsync`
: Gets all the grants for a user based upon subject id.

`RemoveAllGrantsAsync`
: Removes grants from the store based on the subject id and optionally a client id and/or a session id.

`IOidcServerInteractionService` builds on it for the pages of your server: `GetAllUserGrantsAsync` and
`RevokeUserConsentAsync`, for example, to show the user the applications they gave access to and let them revoke it.

[[go back to index]](../index.md#documentation-structure)
