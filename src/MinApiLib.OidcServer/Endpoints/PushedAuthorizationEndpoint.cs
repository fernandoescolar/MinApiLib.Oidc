using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

/// <summary>
/// The pushed authorization request endpoint (RFC 9126).
/// </summary>
internal abstract record PushedAuthorizationEndpoint(string Path) : PostHandlerAsync<PushedAuthorizationEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientSecretValidator ClientValidator,
        [FromServices] IAuthorizeRequestValidator RequestValidator,
        [FromServices] IPushedAuthorizationRequestStore Store,
        [FromServices] IDPoPProofValidator DPoPProofValidator,
        [FromServices] OidcServerOptions Options,
        [FromServices] TimeProvider Clock,
        [FromServices] ILogger<PushedAuthorizationEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.PushedAuthorization, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        SecretLog.ProcessingPushedAuthorizationRequest(logger);

        if (!context.Request.HasApplicationFormContentType())
        {
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        // RFC 9126 section 2.1: the client authenticates exactly as it would at the token endpoint.
        var clientResult = await request.ClientValidator.ValidateAsync(context);
        if (clientResult.IsError)
        {
            return new PushedAuthorizationErrorResult(HttpStatusCode.Unauthorized, OidcConstants.TokenErrors.InvalidClient, "Client authentication failed");
        }

        var client = clientResult.Client;
        var parameters = (await context.Request.ReadFormAsync(cancellationToken)).AsNameValueCollection();

        // GHSA-mxv6-xwqj-ww2p: the pushed parameters must belong to the authenticated client (RFC 9126 section 2.1).
        var clientId = parameters.Get(OidcConstants.AuthorizeRequest.ClientId);
        if (clientId != null && !string.Equals(clientId, client.ClientId, StringComparison.Ordinal))
        {
            SecretLog.PushedClientIdMismatch(logger, clientId, client.ClientId);
            return new PushedAuthorizationErrorResult(HttpStatusCode.BadRequest, OidcConstants.AuthorizeErrors.InvalidRequest, "client_id does not match the authenticated client");
        }

        parameters.Set(OidcConstants.AuthorizeRequest.ClientId, client.ClientId);

        // Client credentials are not part of the authorize request and must not be stored with it.
        parameters.Remove(OidcConstants.TokenRequest.ClientSecret);
        parameters.Remove(OidcConstants.TokenRequest.ClientAssertion);
        parameters.Remove(OidcConstants.TokenRequest.ClientAssertionType);

        // RFC 9449 section 10.1: a DPoP proof sent with the pushed request binds the authorization code to its key.
        if (!DPoPRequest.TryGetProof(context, out var proof))
        {
            return new PushedAuthorizationErrorResult(HttpStatusCode.BadRequest, OidcConstants.TokenErrors.InvalidDPoPProof, "The DPoP header must not be repeated");
        }

        if (proof != null)
        {
            var dpop = await request.DPoPProofValidator.ValidateAsync(new DPoPProofValidationContext
            {
                ProofToken = proof,
                Method = HttpMethods.Post,
                Url = DPoPRequest.GetRequestUrl(context)
            });

            if (dpop.IsError)
            {
                return new PushedAuthorizationErrorResult(HttpStatusCode.BadRequest, dpop.Error, dpop.ErrorDescription);
            }

            var dpopJkt = parameters.Get(OidcConstants.AuthorizeRequest.DPoPKeyThumbprint);
            if (dpopJkt != null && !string.Equals(dpopJkt, dpop.JsonWebKeyThumbprint, StringComparison.Ordinal))
            {
                return new PushedAuthorizationErrorResult(HttpStatusCode.BadRequest, OidcConstants.AuthorizeErrors.InvalidRequest, "dpop_jkt does not match the DPoP proof key");
            }

            parameters.Set(OidcConstants.AuthorizeRequest.DPoPKeyThumbprint, dpop.JsonWebKeyThumbprint);
        }

        var validation = await request.RequestValidator.ValidateAsync(parameters, null, AuthorizeRequestType.PushedAuthorization);
        if (validation.IsError)
        {
            SecretLog.PushedAuthorizationValidationFailed(logger, validation.Error, validation.ErrorDescription);
            return new PushedAuthorizationErrorResult(HttpStatusCode.BadRequest, validation.Error, validation.ErrorDescription);
        }

        var lifetime = request.Options.PushedAuthorization.Lifetime;
        var handle = await request.Store.StorePushedAuthorizationRequestAsync(new PushedAuthorizationRequest
        {
            ClientId = client.ClientId,
            CreationTime = request.Clock.GetUtcNow().UtcDateTime,
            Lifetime = lifetime,
            Parameters = parameters.ToFullDictionary()
        });

        SecretLog.PushedAuthorizationRequestStored(logger, client.ClientId);

        return new PushedAuthorizationResult(Constants.PushedAuthorization.RequestUriPrefix + handle, lifetime);
    }
}
