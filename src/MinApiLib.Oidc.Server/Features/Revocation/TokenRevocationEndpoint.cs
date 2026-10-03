using MinApiLib.Endpoints;

namespace MinApiLib.Oidc.Server.Features.Revocation;

/// <summary>
/// The token revocation endpoint (RFC 7009).
/// </summary>
internal abstract record TokenRevocationEndpoint(string Path) : PostHandlerAsync<TokenRevocationEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientSecretValidator ClientValidator,
        [FromServices] ITokenRevocationRequestValidator RequestValidator,
        [FromServices] ITokenRevocationResponseGenerator ResponseGenerator,
        [FromServices] IEventService Events,
        [FromServices] ILogger<TokenRevocationEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Revocation, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingRevocationRequest();

        if (!context.Request.HasApplicationFormContentType())
        {
            logger.InvalidMediaType();
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        logger.StartRevocationRequest();

        var clientValidationResult = await request.ClientValidator.ValidateAsync(context);

        if (clientValidationResult.IsError)
        {
            return new TokenRevocationErrorResult(OidcConstants.TokenErrors.InvalidClient);
        }

        logger.ClientValidationSuccessful();

        var form = (await context.Request.ReadFormAsync(cancellationToken)).AsNameValueCollection();

        logger.CallingIntoTokenRevocationRequestValidator(request.RequestValidator.GetType().FullName);
        var requestValidationResult = await request.RequestValidator.ValidateRequestAsync(form, clientValidationResult.Client);

        if (requestValidationResult.IsError)
        {
            return new TokenRevocationErrorResult(requestValidationResult.Error);
        }

        logger.CallingIntoTokenRevocationResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.ProcessAsync(requestValidationResult);

        if (response.Success)
        {
            logger.TokenRevocationComplete();
            await request.Events.RaiseAsync(new TokenRevokedSuccessEvent(requestValidationResult, requestValidationResult.Client));
        }
        else
        {
            logger.NoMatchingTokenFound();
        }

        if (response.Error.IsPresent()) return new TokenRevocationErrorResult(response.Error);

        return new StatusCodeResult(HttpStatusCode.OK);
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record TokenRevocationRoute() : TokenRevocationEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Revocation));
internal sealed record TokenRevocationMutualTlsRoute() : TokenRevocationEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Revocation));
