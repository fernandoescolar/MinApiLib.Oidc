using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Features.DeviceFlow;

/// <summary>
/// The device authorization endpoint (RFC 8628).
/// </summary>
internal abstract record DeviceAuthorizationEndpoint(string Path) : PostHandlerAsync<DeviceAuthorizationEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientSecretValidator ClientValidator,
        [FromServices] IDeviceAuthorizationRequestValidator RequestValidator,
        [FromServices] IDeviceAuthorizationResponseGenerator ResponseGenerator,
        [FromServices] IEventService Events,
        [FromServices] ILogger<DeviceAuthorizationEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.DeviceAuthorization, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingDeviceAuthorizeRequest();

        if (!context.Request.HasApplicationFormContentType())
        {
            logger.InvalidHttpRequestForDeviceAuthorizeEndpoint();
            return Error(logger, OidcConstants.TokenErrors.InvalidRequest);
        }

        logger.StartDeviceAuthorizeRequest();

        var clientResult = await request.ClientValidator.ValidateAsync(context);
        if (clientResult.Client == null) return Error(logger, OidcConstants.TokenErrors.InvalidClient);

        var form = (await context.Request.ReadFormAsync(cancellationToken)).AsNameValueCollection();
        var requestResult = await request.RequestValidator.ValidateAsync(form, clientResult);

        if (requestResult.IsError)
        {
            await request.Events.RaiseAsync(new DeviceAuthorizationFailureEvent(requestResult));
            return Error(logger, requestResult.Error, requestResult.ErrorDescription);
        }

        var baseUrl = context.GetOidcServerBaseUrl().EnsureTrailingSlash();

        logger.CallingIntoDeviceAuthorizeResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.ProcessAsync(requestResult, baseUrl);

        await request.Events.RaiseAsync(new DeviceAuthorizationSuccessEvent(requestResult));

        logger.DeviceAuthorizeRequestSuccess();
        return new DeviceAuthorizationResult(response);
    }

    private static TokenErrorResult Error(ILogger logger, string error, string? errorDescription = null)
    {
        var response = new TokenErrorResponse
        {
            Error = error,
            ErrorDescription = errorDescription
        };

        logger.DeviceAuthorizationError(error, error ?? "-no message-");

        return new TokenErrorResult(response);
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record DeviceAuthorizationRoute() : DeviceAuthorizationEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.DeviceAuthorization));
internal sealed record DeviceAuthorizationMutualTlsRoute() : DeviceAuthorizationEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.DeviceAuthorization));
