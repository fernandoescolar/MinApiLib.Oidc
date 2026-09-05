namespace MinApiLib.OidcServer.Stores.Serialization;

/// <summary>
/// Source-generated serialization metadata for the built-in persisted grant types. It produces the same JSON as the
/// reflection-based serializer it replaces; grant types it does not know fall back to reflection.
/// </summary>
[JsonSourceGenerationOptions(
    IgnoreReadOnlyProperties = true,
    Converters = [typeof(ClaimConverter), typeof(ClaimsPrincipalConverter)])]
[JsonSerializable(typeof(AuthorizationCode))]
[JsonSerializable(typeof(RefreshToken))]
[JsonSerializable(typeof(Token))]
[JsonSerializable(typeof(Consent))]
[JsonSerializable(typeof(DeviceCode))]
[JsonSerializable(typeof(AuthorizationNonce))]
[JsonSerializable(typeof(PushedAuthorizationRequest))]
internal sealed partial class PersistentGrantSerializerContext : JsonSerializerContext
{
}
