// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable

using System.Text.Json.Serialization;

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient.Jwk;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonWebKey))]
[JsonSerializable(typeof(JsonWebKeySet))]
internal partial class JwkSourceGenerationContext : JsonSerializerContext
{
}
