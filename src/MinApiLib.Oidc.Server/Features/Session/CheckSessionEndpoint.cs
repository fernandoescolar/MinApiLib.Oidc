// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using MinApiLib.Endpoints;

namespace MinApiLib.Oidc.Server.Features.Session;

/// <summary>
/// The check session endpoint: the iframe of OpenID Connect Session Management.
/// </summary>
internal abstract record CheckSessionEndpoint(string Path) : GetHandlerAsync<CheckSessionEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] ILogger<CheckSessionEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.CheckSession, Path, GetType());

    protected override Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        request.Logger.RenderingCheckSessionResult();
        return Task.FromResult<IResult>(new CheckSessionResult());
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record CheckSessionRoute() : CheckSessionEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.CheckSession));
internal sealed record CheckSessionMutualTlsRoute() : CheckSessionEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.CheckSession));
