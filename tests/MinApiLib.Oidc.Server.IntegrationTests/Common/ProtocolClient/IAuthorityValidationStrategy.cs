// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable


using System.Collections.Generic;

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Authority validation strategy.
/// </summary>
public interface IAuthorityValidationStrategy
{
    /// <summary>
    /// Validate issuer name found in Discovery Document.
    /// </summary>
    /// <param name="expectedAuthority">Authority expected.</param>
    /// <param name="issuerName">Authority declared in Discovery Document.</param>
    /// <returns></returns>
    AuthorityValidationResult IsIssuerNameValid(string issuerName, string expectedAuthority);

    /// <summary>
    /// Validate end point found in Discovery Document.
    /// </summary>
    /// <param name="expectedAuthority">Authority expected.</param>
    /// <param name="endpoint">Endpoint declared in Discovery Document.</param>
    /// <returns></returns>
    AuthorityValidationResult IsEndpointValid(string endpoint, IEnumerable<string> expectedAuthority);
}