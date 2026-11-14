// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using System.Diagnostics.CodeAnalysis;

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Extension methods for hashing strings
/// </summary>
public static class HashExtensions
{
    /// <summary>
    /// Creates a SHA256 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string Sha256(this string? input)
    {
        if (input.IsMissing()) return string.Empty;

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>
    /// Creates a SHA256 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash.</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static byte[]? Sha256(this byte[]? input)
    {
        if (input == null)
        {
            return null;
        }

        return SHA256.HashData(input);
    }

    /// <summary>
    /// Creates a SHA512 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string Sha512(this string? input)
    {
        if (input.IsMissing()) return string.Empty;

        return Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
