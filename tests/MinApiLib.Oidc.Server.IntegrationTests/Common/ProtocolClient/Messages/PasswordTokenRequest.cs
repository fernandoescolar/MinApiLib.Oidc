// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable

using System.Collections.Generic;

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Request for token using password
/// </summary>
/// <seealso cref="TokenRequest" />
public class PasswordTokenRequest : TokenRequest
{
    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    /// <value>
    /// The name of the user.
    /// </value>
    public string UserName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    /// <value>
    /// The password.
    /// </value>
    public string? Password { get; set; }

    /// <summary>
    /// Space separated list of the requested scopes
    /// </summary>
    /// <value>
    /// The scope.
    /// </value>
    public string? Scope { get; set; }
        
    /// <summary>
    /// List of requested resources
    /// </summary>
    /// <value>
    /// The scope.
    /// </value>
    public ICollection<string> Resource { get; set; } = new HashSet<string>();
}