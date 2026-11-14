// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Parses secret according to MTLS spec
/// </summary>
public class MutualTlsSecretParser : ISecretParser
{
    private readonly OidcServerOptions _options;
    private readonly ILogger<MutualTlsSecretParser> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public MutualTlsSecretParser(OidcServerOptions options, ILogger<MutualTlsSecretParser> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Name of authentication method (blank to suppress in discovery since we do special handling)
    /// </summary>
    public string AuthenticationMethod => String.Empty;

    /// <summary>
    /// Parses the HTTP context
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<ParsedSecret?> ParseAsync(HttpContext context)
    {
        SecretLog.StartParsingClientIdInPostBody(_logger);

        if (!context.Request.HasApplicationFormContentType())
        {
            SecretLog.ContentTypeNotForm(_logger);
            return null;
        }

        var body = await context.Request.ReadFormAsync();

        if (body != null)
        {
            var id = body["client_id"].FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(id))
            {
                if (id.Length > _options.InputLengthRestrictions.ClientId)
                {
                    SecretLog.ClientIdTooLong(_logger);
                    return null;
                }

                // Only trust a certificate that the client certificate authentication scheme validated on an
                // mTLS endpoint. Otherwise any certificate the server accepted at the TLS layer, including a
                // self-signed one, could impersonate a client registered with an X.509 name secret.
                if (!_options.MutualTls.Enabled ||
                    !context.Items.ContainsKey(Constants.EnvironmentKeys.MutualTlsClientCertificateAuthenticated))
                {
                    SecretLog.ClientCertificateNotAuthenticatedByMtls(_logger);
                    return null;
                }

                var clientCertificate = await context.Connection.GetClientCertificateAsync();

                if (clientCertificate is null)
                {
                    SecretLog.ClientCertificateNotPresent(_logger);
                    return null;
                }

                return new ParsedSecret
                {
                    Id = id,
                    Credential = clientCertificate,
                    Type = OidcServerConstants.ParsedSecretTypes.X509Certificate
                };
            }
        }

        SecretLog.NoPostBodyFound(_logger);
        return null;
    }
}
