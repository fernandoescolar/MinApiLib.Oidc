// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Hosting;

/// <summary>
///     Middleware that authenticates the client certificate on the mutual TLS endpoints (the <c>/connect/mtls/...</c>
///     aliases mapped by <c>MapOidcServer()</c>, or every path on the mTLS domain)
/// </summary>
public class MutualTlsEndpointMiddleware
{
    private readonly ILogger<MutualTlsEndpointMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly OidcServerOptions _options;

    /// <summary>
    ///     ctor
    /// </summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public MutualTlsEndpointMiddleware(RequestDelegate next, OidcServerOptions options,
        ILogger<MutualTlsEndpointMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Invoke(HttpContext context)
    {
        if (_options.MutualTls.Enabled)
        {
            if (_options.MutualTls.DomainName.IsPresent())
            {
                if (_options.MutualTls.DomainName.Contains('.'))
                {
                    if (context.Request.Host.Host.Equals(_options.MutualTls.DomainName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var result = await TriggerCertificateAuthentication(context);
                        if (!result.Succeeded)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    if (context.Request.Host.Host.StartsWith(_options.MutualTls.DomainName + ".", StringComparison.OrdinalIgnoreCase))
                    {
                        var result = await TriggerCertificateAuthentication(context);
                        if (!result.Succeeded)
                        {
                            return;
                        }
                    }
                }
            }
            else if (context.Request.Path.StartsWithSegments(Constants.ProtocolRoutePaths.MtlsPathPrefix.EnsureLeadingSlash()))
            {
                var result = await TriggerCertificateAuthentication(context);
                if (!result.Succeeded)
                {
                    return;
                }
            }
        }

        await _next(context);
    }

    private async Task<AuthenticateResult> TriggerCertificateAuthentication(HttpContext context)
    {
        var x509AuthResult =
            await context.AuthenticateAsync(_options.MutualTls.ClientCertificateAuthenticationScheme);

        if (!x509AuthResult.Succeeded)
        {
            ResponseAndHostingLog.MtlsAuthenticationFailed(_logger,
                x509AuthResult.Failure?.Message);
            await context.ForbidAsync(_options.MutualTls.ClientCertificateAuthenticationScheme);
        }
        else
        {
            // Lets MutualTlsSecretParser trust the connection certificate only after the scheme validated it.
            context.Items[Constants.EnvironmentKeys.MutualTlsClientCertificateAuthenticated] = true;
        }

        return x509AuthResult;
    }
}
