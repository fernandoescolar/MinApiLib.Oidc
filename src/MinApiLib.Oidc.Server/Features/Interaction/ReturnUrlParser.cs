// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Interaction;

/// <summary>
/// Parses a return URL using all registered URL parsers
/// </summary>
[RegisterInIServiceCollection]
public class ReturnUrlParser
{
    private readonly IEnumerable<IReturnUrlParser> _parsers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReturnUrlParser"/> class.
    /// </summary>
    /// <param name="parsers">The parsers.</param>
    public ReturnUrlParser(IEnumerable<IReturnUrlParser> parsers)
    {
        _parsers = parsers;
    }

    /// <summary>
    /// Parses the return URL.
    /// </summary>
    /// <param name="returnUrl">The return URL.</param>
    /// <returns></returns>
    public virtual async Task<AuthorizationRequest?> ParseAsync(string returnUrl)
    {
        foreach (var parser in _parsers)
        {
            var result = await parser.ParseAsync(returnUrl);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a return URL is valid.
    /// </summary>
    /// <param name="returnUrl">The return URL.</param>
    /// <returns>
    ///   <c>true</c> if the return URL is valid; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsValidReturnUrl(string returnUrl)
    {
        foreach (var parser in _parsers)
        {
            if (parser.IsValidReturnUrl(returnUrl))
            {
                return true;
            }
        }

        return false;
    }
}
