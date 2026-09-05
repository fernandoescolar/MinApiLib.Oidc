// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel); modified for IdentityServer.

using IdentityServer.IdentityModel.Internal;
using System.Security.Cryptography;
using System.Text;

namespace IdentityServer.IdentityModel;

/// <summary>
/// Extensions for strings
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Creates a SHA256 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string ToSha256(this string input)
    {
        if (input.IsMissing()) return string.Empty;

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>
    /// Creates a SHA512 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string ToSha512(this string input)
    {
        if (input.IsMissing()) return string.Empty;

        return Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(input)));
    }
}