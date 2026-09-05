using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

/// <summary>
/// The token introspection endpoint (RFC 7662).
/// </summary>
internal abstract record IntrospectionEndpoint(string Path) : PostHandlerAsync<IntrospectionEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IApiSecretValidator ApiSecretValidator,
        [FromServices] IIntrospectionRequestValidator RequestValidator,
        [FromServices] IIntrospectionResponseGenerator ResponseGenerator,
        [FromServices] IEventService Events,
        [FromServices] ILogger<IntrospectionEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Introspection, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingIntrospectionRequest();

        if (!context.Request.HasApplicationFormContentType())
        {
            logger.InvalidMediaTypeForIntrospectionEndpoint();
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        logger.StartingIntrospectionRequest();

        var apiResult = await request.ApiSecretValidator.ValidateAsync(context);
        if (apiResult.Resource == null)
        {
            logger.APIUnauthorizedToCallIntrospectionEndpoint();
            return new StatusCodeResult(HttpStatusCode.Unauthorized);
        }

        var body = await context.Request.ReadFormAsync(cancellationToken);

        logger.CallingIntoIntrospectionRequestValidator(request.RequestValidator.GetType().FullName);
        var validationResult = await request.RequestValidator.ValidateAsync(body.AsNameValueCollection(), apiResult.Resource);
        if (validationResult.IsError)
        {
            logger.FailedTokenIntrospection(validationResult.Error, apiResult.Resource.Name);
            await request.Events.RaiseAsync(new TokenIntrospectionFailureEvent(apiResult.Resource.Name, validationResult.Error));

            return new BadRequestResult(validationResult.Error);
        }

        logger.CallingIntoIntrospectionResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.ProcessAsync(validationResult);

        logger.SuccessTokenIntrospection(validationResult.IsActive, validationResult.Api.Name);
        return new IntrospectionResult(response);
    }
}
