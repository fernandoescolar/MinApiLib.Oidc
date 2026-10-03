// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Models a client assertion
/// </summary>
public class ClientAssertion
{
    /// <summary>
    /// Gets or sets the assertion type.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the assertion value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    public string Value { get; set; } = default!;
}