// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Models;

/// <summary>
/// Models a refresh token.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    /// <value>
    /// The creation time.
    /// </value>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the life time.
    /// </summary>
    /// <value>
    /// The life time.
    /// </value>
    public int Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the consumed time.
    /// </summary>
    /// <value>
    /// The consumed time.
    /// </value>
    public DateTime? ConsumedTime { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    /// <value>
    /// The access token.
    /// </value>
    public Token AccessToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the original subject that requiested the token.
    /// </summary>
    /// <value>
    /// The subject.
    /// </value>
    public ClaimsPrincipal Subject
    {
        get
        {
            var user = new OidcServerUser(SubjectId!);
            if (AccessToken.Claims != null)
            {
                foreach (var claim in AccessToken.Claims)
                {
                    user.AdditionalClaims.Add(claim);
                }
            }
            return user.CreatePrincipal();
        }
    }

    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    public int Version { get; set; } = 4;

    /// <summary>
    /// Gets or sets the resource indicators (RFC 8707) the grant was authorized for. A refresh request may ask for a
    /// token for one of them with the <c>resource</c> parameter.
    /// </summary>
    public IEnumerable<string>? AuthorizedResourceIndicators { get; set; }

    /// <summary>
    /// Gets the client identifier.
    /// </summary>
    /// <value>
    /// The client identifier.
    /// </value>
    public string ClientId => AccessToken.ClientId;

    /// <summary>
    /// Gets the subject identifier.
    /// </summary>
    /// <value>
    /// The subject identifier.
    /// </value>
    public string? SubjectId => AccessToken.SubjectId;

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    /// <value>
    /// The session identifier.
    /// </value>
    public string? SessionId => AccessToken.SessionId;

    /// <summary>
    /// Gets or sets the description the user assigned to this grant, for example the name of the device or application.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string? Description => AccessToken.Description;

    /// <summary>
    /// Gets the scopes.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public IEnumerable<string> Scopes => AccessToken.Scopes;
}
