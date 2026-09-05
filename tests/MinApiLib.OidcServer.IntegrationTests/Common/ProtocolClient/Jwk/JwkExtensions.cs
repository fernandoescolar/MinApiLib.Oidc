// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel): test-only protocol client.

#nullable enable


using System.Text;
using System.Text.Json;

namespace MinApiLib.OidcServer.IntegrationTests.ProtocolClient.Jwk;

/// <summary>
/// Extensions for JsonWebKey
/// </summary>
public static class JsonWebKeyExtensions
{
    /// <summary>
    /// Converts a JSON web key to a URL safe string.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public static string ToJwkString(this JsonWebKey key)
    {
        var json = JsonSerializer.Serialize(key, JwkSourceGenerationContext.Default.JsonWebKey);
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));
    }
}