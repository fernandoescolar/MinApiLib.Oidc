// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Session;

/// <summary>
/// Represents a validated end session (logout) request
/// </summary>
public class ValidatedEndSessionRequest : ValidatedRequest
{
    /// <summary>
    /// Gets a value indicating whether the client was established by a valid <c>id_token_hint</c>.
    /// </summary>
    public bool IsAuthenticated => Client != null && IsIdTokenHintValidated;

    /// <summary>
    /// Gets or sets a value indicating whether a valid <c>id_token_hint</c> was presented. When the client is
    /// identified by <c>client_id</c> alone this is <c>false</c> and the sign-out must be confirmed by the user.
    /// </summary>
    public bool IsIdTokenHintValidated { get; set; }

    /// <summary>
    /// Gets or sets the post-logout URI.
    /// </summary>
    /// <value>
    /// The post-logout URI.
    /// </value>
    public string? PostLogOutUri { get; set; }

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    /// <value>
    /// The state.
    /// </value>
    public string? State { get; set; }

    /// <summary>
    ///  Ids of clients known to have an authentication session for user at end session time
    /// </summary>
    public IEnumerable<string>? ClientIds { get; set; }
}
