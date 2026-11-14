// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Stores.Serialization;

public class ClaimConverter : JsonConverter<Claim>
{
    public override Claim? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var source = JsonSerializer.Deserialize<ClaimLite>(ref reader, options);

        if (source == null)
        {
            return null;
        }

        return new Claim(source.Type, source.Value, source.ValueType);
    }

    public override void Write(Utf8JsonWriter writer, Claim value, JsonSerializerOptions options)
    {
        var target = new ClaimLite
        {
            Type = value.Type,
            Value = value.Value,
            ValueType = value.ValueType
        };

        JsonSerializer.Serialize(writer, target, options);
    }
}
