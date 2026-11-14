// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Logging;

/// <summary>
/// Helper to JSON serialize object data for logging.
/// </summary>
internal static class LogSerializer
{
    static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    static LogSerializer()
    {
        Options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    /// <summary>
    /// Serializes the specified object.
    /// </summary>
    /// <param name="logObject">The object.</param>
    /// <returns></returns>
    public static string Serialize(object logObject)
    {
        return System.Text.Json.JsonSerializer.Serialize(logObject, Options);
    }
}
