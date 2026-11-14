// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens;

internal class TokenRequestValidationLog
{
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? GrantType { get; set; }
    public string? UserName { get; set; }
    public string? Scopes { get; set; }

    public string AuthorizationCode { get; set; }
    public string RefreshToken { get; set; }

    public IEnumerable<string>? AuthenticationContextReferenceClasses { get; set; }
    public string? Tenant { get; set; }
    public string? IdP { get; set; }

    public Dictionary<string, string> Raw { get; set; }

    public TokenRequestValidationLog(ValidatedTokenRequest request, IEnumerable<string> sensitiveValuesFilter)
    {
        Raw = request.Raw.ToScrubbedDictionary(sensitiveValuesFilter.ToArray());

        if (request.Client != null)
        {
            ClientId = request.Client.ClientId;
            ClientName = request.Client.ClientName;
        }

        if (request.RequestedScopes != null)
        {
            Scopes = request.RequestedScopes.ToSpaceSeparatedString();
        }

        GrantType = request.GrantType;
        UserName = request.UserName;
        AuthorizationCode = request.AuthorizationCodeHandle.Obfuscate();
        RefreshToken = request.RefreshTokenHandle.Obfuscate();
    }

    public override string ToString()
    {
        return LogSerializer.Serialize(this);
    }
}
