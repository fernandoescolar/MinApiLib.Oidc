using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

/// <summary>
/// The token endpoint.
/// </summary>
internal abstract record TokenEndpoint(string Path) : PostHandlerAsync<TokenEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientSecretValidator ClientValidator,
        [FromServices] ITokenRequestValidator RequestValidator,
        [FromServices] ITokenResponseGenerator ResponseGenerator,
        [FromServices] IEventService Events,
        [FromServices] IDPoPProofValidator DPoPProofValidator,
        [FromServices] ILogger<TokenEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Token, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingTokenRequest();

        if (!context.Request.HasApplicationFormContentType())
        {
            logger.InvalidHttpRequestForTokenEndpoint();
            return Error(OidcConstants.TokenErrors.InvalidRequest);
        }

        logger.StartTokenRequest();

        var clientResult = await request.ClientValidator.ValidateAsync(context);

        if (clientResult.Client == null)
        {
            return Error(OidcConstants.TokenErrors.InvalidClient);
        }

        // RFC 9449 section 5: a DPoP proof binds the issued tokens to the client's key.
        if (!DPoPRequest.TryGetProof(context, out var proof))
        {
            return Error(OidcConstants.TokenErrors.InvalidDPoPProof, "The DPoP header must not be repeated");
        }

        DPoPProofValidationResult? dpopResult = null;
        if (proof != null)
        {
            dpopResult = await request.DPoPProofValidator.ValidateAsync(new DPoPProofValidationContext
            {
                ProofToken = proof,
                Method = HttpMethods.Post,
                Url = DPoPRequest.GetRequestUrl(context)
            });

            if (dpopResult.IsError)
            {
                return Error(dpopResult.Error, dpopResult.ErrorDescription);
            }
        }

        var form = (await context.Request.ReadFormAsync(cancellationToken)).AsNameValueCollection();
        logger.CallingIntoTokenRequestValidator(request.RequestValidator.GetType().FullName);
        var requestResult = await request.RequestValidator.ValidateRequestAsync(form, clientResult, dpopResult);

        if (requestResult.IsError)
        {
            await request.Events.RaiseAsync(new TokenIssuedFailureEvent(requestResult));
            return Error(requestResult.Error, requestResult.ErrorDescription, requestResult.CustomResponse);
        }

        logger.CallingIntoTokenRequestResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.ProcessAsync(requestResult);

        await request.Events.RaiseAsync(new TokenIssuedSuccessEvent(response, requestResult));
        LogTokens(logger, response, requestResult);

        logger.TokenRequestSuccess();
        return new TokenResult(response);
    }

    private static TokenErrorResult Error(string error, string? errorDescription = null, Dictionary<string, object>? custom = null)
    {
        var response = new TokenErrorResponse
        {
            Error = error,
            ErrorDescription = errorDescription,
            Custom = custom
        };

        return new TokenErrorResult(response);
    }

    private static void LogTokens(ILogger logger, TokenResponse response, TokenRequestValidationResult requestResult)
    {
        var clientId = $"{requestResult.ValidatedRequest.Client.ClientId} ({requestResult.ValidatedRequest.Client?.ClientName ?? "no name set"})";
        var subjectId = requestResult.ValidatedRequest.Subject?.GetSubjectId() ?? "no subject";

        if (response.IdentityToken != null)
        {
            logger.IdentityTokenIssued(clientId, subjectId, response.IdentityToken);
        }
        if (response.RefreshToken != null)
        {
            logger.RefreshTokenIssued(clientId, subjectId, response.RefreshToken);
        }
        if (response.AccessToken != null)
        {
            logger.AccessTokenIssued(clientId, subjectId, response.AccessToken);
        }
    }
}
