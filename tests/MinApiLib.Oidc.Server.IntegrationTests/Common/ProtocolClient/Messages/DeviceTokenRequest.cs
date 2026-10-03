// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Request for token using urn:ietf:params:oauth:grant-type:device_code
/// </summary>
/// <seealso cref="TokenRequest" />
public class DeviceTokenRequest : TokenRequest
{
    /// <summary>
    /// Gets or sets the device code.
    /// </summary>
    /// <value>
    /// The scope.
    /// </value>
    public string DeviceCode { get; set; } = default!;
}