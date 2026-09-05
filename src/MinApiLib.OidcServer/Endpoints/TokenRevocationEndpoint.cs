using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

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
