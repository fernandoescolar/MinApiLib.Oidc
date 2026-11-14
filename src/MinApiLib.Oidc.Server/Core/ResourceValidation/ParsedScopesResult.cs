// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ResourceValidation;

/// <summary>
/// Represents the result of scope parsing.
/// </summary>
public class ParsedScopesResult
{
    /// <summary>
    /// The valid parsed scopes.
    /// </summary>
    public ICollection<ParsedScopeValue> ParsedScopes { get; set; } = new HashSet<ParsedScopeValue>();

    /// <summary>
    /// The errors encountered while parsing.
    /// </summary>
    public ICollection<ParsedScopeValidationError> Errors { get; set; } = new HashSet<ParsedScopeValidationError>();

    /// <summary>
    /// Indicates if the result of parsing the scopes was successful.
    /// </summary>
    public bool Succeeded => Errors == null || Errors.Count == 0;
}
