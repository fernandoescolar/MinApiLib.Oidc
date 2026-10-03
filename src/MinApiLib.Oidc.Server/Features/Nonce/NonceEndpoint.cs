using MinApiLib.Endpoints;

namespace MinApiLib.Oidc.Server.Features.Nonce;

/// <summary>
/// The nonce endpoint: issues the nonces a client signs into the request objects of the nonce pattern.
/// </summary>
internal abstract record NonceEndpoint(string Path) : PostHandlerAsync<NonceEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientSecretValidator ClientValidator,
        [FromServices] INonceRequestValidator RequestValidator,
        [FromServices] INonceResponseGenerator ResponseGenerator,
        [FromServices] IEventService Events,
        [FromServices] ILogger<NonceEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Nonce, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingNonceRequest();

        if (!context.Request.HasApplicationFormContentType())
        {
            logger.InvalidHttpRequestForNonceEndpoint();
            return Error(OidcConstants.TokenErrors.InvalidRequest);
        }

        logger.StartNonceRequest();

        var clientResult = await request.ClientValidator.ValidateAsync(context);
        if (clientResult.Client == null)
        {
            return Error(OidcConstants.TokenErrors.InvalidClient);
        }

        if (!clientResult.Client.AllowedGrantTypes.Contains(GrantType.NoncePattern))
        {
            return Error(OidcConstants.TokenErrors.InvalidGrant, $"client should be allowed to use grant type {GrantType.NoncePattern}");
        }

        var form = (await context.Request.ReadFormAsync(cancellationToken)).AsNameValueCollection();
        logger.CallingIntoNonceRequestValidator(request.RequestValidator.GetType().FullName);

        var requestResult = await request.RequestValidator.ValidateRequestAsync(form, clientResult);
        if (requestResult.IsError)
        {
            await request.Events.RaiseAsync(new NonceIssuedFailureEvent(requestResult));
            return Error(requestResult.Error, requestResult.ErrorDescription);
        }

        logger.CallingIntoNonceRequestResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.ProcessAsync(requestResult);

        await request.Events.RaiseAsync(new NonceIssuedSuccessEvent(response, requestResult));
        logger.NonceRequestSuccess();

        return new NonceResult(response);
    }

    private static NonceErrorResult Error(string message, string? errorDescription = null)
        => new(message, errorDescription);
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record NonceRoute() : NonceEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Nonce));
internal sealed record NonceMutualTlsRoute() : NonceEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Nonce));
