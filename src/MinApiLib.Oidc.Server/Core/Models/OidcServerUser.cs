// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.


namespace MinApiLib.Oidc.Server.Core.Models;

/// <summary>
/// Model properties of an OidcServer user
/// </summary>
public class OidcServerUser
{
    /// <summary>
    /// Subject ID (mandatory)
    /// </summary>
    public string SubjectId { get; }

    /// <summary>
    /// Display name (optional)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Identity provider (optional)
    /// </summary>
    public string? IdentityProvider { get; set; }

    /// <summary>
    /// Authentication methods
    /// </summary>
    public ICollection<string> AuthenticationMethods { get; set; } = new HashSet<string>();

    /// <summary>
    /// Authentication time
    /// </summary>
    public DateTime? AuthenticationTime { get; set; }

    /// <summary>
    /// Additional claims
    /// </summary>
    public ICollection<Claim> AdditionalClaims { get; set; } = new HashSet<Claim>(new ClaimComparer());

    /// <summary>
    /// Initializes a user identity
    /// </summary>
    /// <param name="subjectId">The subject ID</param>
    public OidcServerUser(string subjectId)
    {
        if (subjectId.IsMissing()) throw new ArgumentException("SubjectId is mandatory", nameof(subjectId));

        SubjectId = subjectId;
    }

    /// <summary>
    /// Creates an OidcServer claims principal
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public ClaimsPrincipal CreatePrincipal()
    {
        if (SubjectId.IsMissing()) throw new ArgumentException("SubjectId is mandatory", nameof(SubjectId));
        var claims = new List<Claim> { new Claim(JwtClaimTypes.Subject, SubjectId) };

        if (DisplayName.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.Name, DisplayName));
        }

        if (IdentityProvider.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.IdentityProvider, IdentityProvider));
        }

        if (AuthenticationTime.HasValue)
        {
            claims.Add(new Claim(JwtClaimTypes.AuthenticationTime, new DateTimeOffset(AuthenticationTime.Value).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (AuthenticationMethods.Count > 0)
        {
            foreach (var amr in AuthenticationMethods)
            {
                claims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, amr));
            }
        }

        claims.AddRange(AdditionalClaims);

        var id = new ClaimsIdentity(claims.Distinct(new ClaimComparer()), ModelConstants.OidcServerAuthenticationType, JwtClaimTypes.Name, JwtClaimTypes.Role);
        return new ClaimsPrincipal(id);
    }
}
