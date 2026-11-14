// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Clients;

/// <summary>
/// Extension methods for client.
/// </summary>
public static class ClientExtensions
{
    /// <summary>
    /// Constructs a list of SecurityKey from a Secret collection
    /// </summary>
    /// <param name="secrets">The secrets</param>
    /// <returns></returns>
    public static Task<List<SecurityKey>> GetKeysAsync(this IEnumerable<Secret> secrets)
    {
        var secretList = secrets.ToList().AsReadOnly();
        var keys = new List<SecurityKey>();

        var certificates = GetCertificates(secretList)
                            .Select(c => (SecurityKey)new X509SecurityKey(c))
                            .ToList();
        keys.AddRange(certificates);

        var jwks = secretList
                    .Where(s => s.Type == OidcServerConstants.SecretTypes.JsonWebKey)
                    .Select(s => new Microsoft.IdentityModel.Tokens.JsonWebKey(s.Value))
                    .ToList();
        keys.AddRange(jwks);

        return Task.FromResult(keys);
    }

    private static List<X509Certificate2> GetCertificates(IEnumerable<Secret> secrets)
    {
        return secrets
            .Where(s => s.Type == OidcServerConstants.SecretTypes.X509CertificateBase64)
            .Select(s => X509CertificateLoader.LoadCertificate(Convert.FromBase64String(s.Value)))
            .Where(c => c != null)
            .ToList();
    }
}
