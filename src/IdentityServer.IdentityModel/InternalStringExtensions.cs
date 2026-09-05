// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel); modified for IdentityServer.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IdentityServer.IdentityModel.Internal;

internal static class InternalStringExtensions
{
    [DebuggerStepThrough]
    public static bool IsMissing(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    [DebuggerStepThrough]
    public static bool IsPresent(this string? value)
    {
        return !(value.IsMissing());
    }
}
