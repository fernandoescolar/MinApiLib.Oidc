// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using MinApiLib.Endpoints;

namespace MinApiLib.Oidc.Server.Features.Discovery;

/// <summary>
/// The JSON Web Key Set endpoint of discovery.
/// </summary>
internal abstract record DiscoveryKeyEndpoint(string Path) : GetHandlerAsync<DiscoveryKeyEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] OidcServerOptions Options,
        [FromServices] IDiscoveryResponseGenerator ResponseGenerator,
        [FromServices] ILogger<DiscoveryKeyEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Discovery, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var logger = request.Logger;
        logger.ProcessingKeyDiscoveryRequest();

        logger.StartKeyDiscoveryRequest();

        if (request.Options.Discovery.ShowKeySet == false)
        {
            logger.KeyDiscoveryDisabled();
            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        logger.CallingIntoDiscoveryResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.CreateJwkDocumentAsync();

        return new JsonWebKeysResult(response, request.Options.Discovery.ResponseCacheInterval);
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record DiscoveryKeyRoute() : DiscoveryKeyEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.DiscoveryWebKeys));
