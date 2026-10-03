using MinApiLib.Endpoints;

namespace MinApiLib.Oidc.Server.Features.UserInfo;

/// <summary>
/// The userinfo endpoint.
/// </summary>
internal abstract record UserInfoEndpoint(string Path) : EndpointHandlerAsync<UserInfoEndpoint.Request>([HttpMethods.Get, HttpMethods.Post], Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] BearerTokenUsageValidator TokenUsageValidator,
        [FromServices] IUserInfoRequestValidator RequestValidator,
        [FromServices] IUserInfoResponseGenerator ResponseGenerator,
        [FromServices] ILogger<UserInfoEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.UserInfo, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;

        logger.StartUserInfoRequest();

        // userinfo requires an access token on the request
        var tokenUsageResult = await request.TokenUsageValidator.ValidateAsync(context);
        if (tokenUsageResult.TokenFound == false)
        {
            logger.NoAccessTokenFound();
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        // validate the request
        logger.CallingIntoUserInfoRequestValidator(request.RequestValidator.GetType().FullName);
        var validationResult = await request.RequestValidator.ValidateRequestAsync(tokenUsageResult.Token);

        if (validationResult.IsError)
        {
            return Error(validationResult.Error);
        }

        // RFC 9449 section 7: a DPoP-bound token needs the DPoP scheme and a proof of the bound key.
        var dpopError = await DPoPRequest.ValidateProtectedResourceRequestAsync(
            context,
            tokenUsageResult.Token,
            tokenUsageResult.UsageType == BearerTokenUsageType.DPoPAuthorizationHeader,
            validationResult.TokenValidationResult?.Claims);
        if (dpopError != null)
        {
            SecretLog.UserInfoRequestRejected(logger, dpopError);
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, dpopError);
        }

        // generate response
        logger.CallingIntoUserInfoResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.ProcessAsync(validationResult);

        logger.EndUserInfoRequest();
        return new UserInfoResult(response);
    }

    private static ProtectedResourceErrorResult Error(string error, string? description = null)
        => new(error, description);
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record UserInfoRoute() : UserInfoEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.UserInfo));
internal sealed record UserInfoMutualTlsRoute() : UserInfoEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.UserInfo));
