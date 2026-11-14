// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Infrastructure;

internal static class ObjectSerializer
{
    private static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToString(object o)
    {
        return System.Text.Json.JsonSerializer.Serialize(o, Options);
    }

    public static T? FromString<T>(string value)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(value, Options);
    }
}
