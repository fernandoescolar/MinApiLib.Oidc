// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel), Copyright (c) Brock Allen & Dominick Baier,
// Apache-2.0: test-only protocol client.

#nullable enable

namespace MinApiLib.OidcServer.IntegrationTests.ProtocolClient;

/// <summary>
/// Specifies how parameter in the collection get replaced (or not).
/// </summary>
public enum ParameterReplaceBehavior
{
    /// <summary>
    /// Allow multiple
    /// </summary>
    None, 
        
    /// <summary>
    /// Replace a single parameter with the same key
    /// </summary>
    Single, 
        
    /// <summary>
    /// Replace all parameters with same key
    /// </summary>
    All
}