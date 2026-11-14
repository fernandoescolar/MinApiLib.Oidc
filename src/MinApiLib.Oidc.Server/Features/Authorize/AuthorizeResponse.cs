// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

public class AuthorizeResponse
{
    public ValidatedAuthorizeRequest Request { get; set; } = default!;
    public string? RedirectUri => Request?.RedirectUri;
    public string? State => Request?.State;
    public string? Scope => Request?.ValidatedResources?.RawScopeValues.ToSpaceSeparatedString();

    public string? IdentityToken { get; set; }
    public string? AccessToken { get; set; }
    public int AccessTokenLifetime { get; set; }
    public string? Code { get; set; }
    public string? SessionState { get; set; }

    /// <summary>
    /// The issuer identifier returned as the <c>iss</c> response parameter (RFC 9207).
    /// </summary>
    public string? Issuer { get; set; }

    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error.IsPresent();
}
