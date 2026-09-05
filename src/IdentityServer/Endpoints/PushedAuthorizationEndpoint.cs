namespace IdentityServer.Endpoints;

/// <summary>
/// The pushed authorization request endpoint (RFC 9126).
/// </summary>
internal class PushedAuthorizationEndpoint : IEndpointHandler
{
    private readonly IClientSecretValidator _clientValidator;
    private readonly IAuthorizeRequestValidator _requestValidator;
    private readonly IPushedAuthorizationRequestStore _store;
    private readonly IDPoPProofValidator _dpopProofValidator;
    private readonly IdentityServerOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    public PushedAuthorizationEndpoint(
        IClientSecretValidator clientValidator,
        IAuthorizeRequestValidator requestValidator,
        IPushedAuthorizationRequestStore store,
        IDPoPProofValidator dpopProofValidator,
        IdentityServerOptions options,
        TimeProvider clock,
        ILogger<PushedAuthorizationEndpoint> logger)
    {
        _clientValidator = clientValidator;
        _requestValidator = requestValidator;
        _store = store;
        _dpopProofValidator = dpopProofValidator;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        SecretLog.ProcessingPushedAuthorizationRequest(_logger);

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        if (!context.Request.HasApplicationFormContentType())
        {
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        // RFC 9126 section 2.1: the client authenticates exactly as it would at the token endpoint.
        var clientResult = await _clientValidator.ValidateAsync(context);
        if (clientResult.IsError)
        {
            return new PushedAuthorizationErrorResult(HttpStatusCode.Unauthorized, OidcConstants.TokenErrors.InvalidClient, "Client authentication failed");
        }

        var client = clientResult.Client;
        var parameters = (await context.Request.ReadFormAsync()).AsNameValueCollection();

        // GHSA-mxv6-xwqj-ww2p: the pushed parameters must belong to the authenticated client (RFC 9126 section 2.1).
        var clientId = parameters.Get(OidcConstants.AuthorizeRequest.ClientId);
        if (clientId != null && !string.Equals(clientId, client.ClientId, StringComparison.Ordinal))
        {
            SecretLog.PushedClientIdMismatch(_logger, clientId, client.ClientId);
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
            var dpop = await _dpopProofValidator.ValidateAsync(new DPoPProofValidationContext
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

        var validation = await _requestValidator.ValidateAsync(parameters, null, AuthorizeRequestType.PushedAuthorization);
        if (validation.IsError)
        {
            SecretLog.PushedAuthorizationValidationFailed(_logger, validation.Error, validation.ErrorDescription);
            return new PushedAuthorizationErrorResult(HttpStatusCode.BadRequest, validation.Error, validation.ErrorDescription);
        }

        var lifetime = _options.PushedAuthorization.Lifetime;
        var handle = await _store.StorePushedAuthorizationRequestAsync(new PushedAuthorizationRequest
        {
            ClientId = client.ClientId,
            CreationTime = _clock.GetUtcNow().UtcDateTime,
            Lifetime = lifetime,
            Parameters = parameters.ToFullDictionary()
        });

        SecretLog.PushedAuthorizationRequestStored(_logger, client.ClientId);

        return new PushedAuthorizationResult(Constants.PushedAuthorization.RequestUriPrefix + handle, lifetime);
    }
}
