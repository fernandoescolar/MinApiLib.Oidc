// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using MinApiLib.Endpoints;

namespace MinApiLib.Oidc.Server.Features.Discovery;

/// <summary>
/// The discovery document endpoint.
/// </summary>
internal abstract record DiscoveryEndpoint(string Path) : GetHandlerAsync<DiscoveryEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] OidcServerOptions Options,
        [FromServices] IDiscoveryResponseGenerator ResponseGenerator,
        [FromServices] ILogger<DiscoveryEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Discovery, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingDiscoveryRequest();

        logger.StartDiscoveryRequest();

        if (!request.Options.Endpoints.EnableDiscoveryEndpoint)
        {
            logger.DiscoveryEndpointDisabled();
            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        var baseUrl = context.GetOidcServerBaseUrl().EnsureTrailingSlash();
        var issuerUri = context.GetOidcServerIssuerUri();

        logger.CallingIntoDiscoveryResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

        return new DiscoveryDocumentResult(response, request.Options.Discovery.ResponseCacheInterval);
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record DiscoveryRoute() : DiscoveryEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.DiscoveryConfiguration));
