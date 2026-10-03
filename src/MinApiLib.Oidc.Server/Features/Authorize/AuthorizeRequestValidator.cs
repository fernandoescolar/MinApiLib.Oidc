namespace MinApiLib.Oidc.Server.Features.Authorize;

[RegisterInIServiceCollection(typeof(IAuthorizeRequestValidator))]
internal class AuthorizeRequestValidator : IAuthorizeRequestValidator
{
    private readonly OidcServerOptions _options;
    private readonly IClientStore _clients;
    private readonly ICustomAuthorizeRequestValidator _customValidator;
    private readonly IRedirectUriValidator _uriValidator;
    private readonly IResourceValidator _resourceValidator;
    private readonly IUserSession _userSession;
    private readonly JwtRequestValidator _jwtRequestValidator;
    private readonly IJwtRequestUriHttpClient _jwtRequestUriHttpClient;
    private readonly IPushedAuthorizationRequestStore _pushedAuthorizationRequestStore;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;
    private readonly ResponseTypeEqualityComparer _responseTypeEqualityComparer = new();

    public AuthorizeRequestValidator(
        OidcServerOptions options,
        IClientStore clients,
        ICustomAuthorizeRequestValidator customValidator,
        IRedirectUriValidator uriValidator,
        IResourceValidator resourceValidator,
        IUserSession userSession,
        JwtRequestValidator jwtRequestValidator,
        IJwtRequestUriHttpClient jwtRequestUriHttpClient,
        IPushedAuthorizationRequestStore pushedAuthorizationRequestStore,
        TimeProvider clock,
        ILogger<AuthorizeRequestValidator> logger)
    {
        _options = options;
        _clients = clients;
        _customValidator = customValidator;
        _uriValidator = uriValidator;
        _resourceValidator = resourceValidator;
        _jwtRequestValidator = jwtRequestValidator;
        _userSession = userSession;
        _jwtRequestUriHttpClient = jwtRequestUriHttpClient;
        _pushedAuthorizationRequestStore = pushedAuthorizationRequestStore;
        _clock = clock;
        _logger = logger;
    }

    public Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal? subject = null)
        => ValidateAsync(parameters, subject, AuthorizeRequestType.Authorize);

    public async Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal? subject, AuthorizeRequestType requestType)
    {
        RequestValidationLog.StartAuthorizeRequestValidation(_logger);

        var request = new ValidatedAuthorizeRequest
        {
            Options = _options,
            Subject = subject ?? Principal.Anonymous,
            Raw = parameters ?? throw new ArgumentNullException(nameof(parameters))
        };

        var loadClientResult = await LoadClientAsync(request);
        if (loadClientResult.IsError)
        {
            return loadClientResult;
        }

        var pushedResult = await LoadPushedAuthorizationRequestAsync(request, requestType);
        if (pushedResult.IsError)
        {
            return pushedResult;
        }

        var roLoadResult = await LoadRequestObjectAsync(request);
        if (roLoadResult.IsError)
        {
            return roLoadResult;
        }

        var roValidationResult = await ValidateRequestObjectAsync(request);
        if (roValidationResult.IsError)
        {
            return roValidationResult;
        }

        var clientResult = await ValidateClientAsync(request);
        if (clientResult.IsError)
        {
            return clientResult;
        }

        var mandatoryResult = ValidateCoreParameters(request);
        if (mandatoryResult.IsError)
        {
            return mandatoryResult;
        }

        var scopeResult = await ValidateScopeAsync(request);
        if (scopeResult.IsError)
        {
            return scopeResult;
        }

        var optionalResult = await ValidateOptionalParametersAsync(request);
        if (optionalResult.IsError)
        {
            return optionalResult;
        }

        RequestValidationLog.CallingCustomAuthorizeValidator(_logger, _customValidator.GetType().FullName!);
        var context = new CustomAuthorizeRequestValidationContext
        {
            Result = new AuthorizeRequestValidationResult(request)
        };
        await _customValidator.ValidateAsync(context);

        var customResult = context.Result;
        if (customResult.IsError)
        {
            LogError("Error in custom validation", customResult.Error, request);
            return Invalid(request, customResult.Error, customResult.ErrorDescription);
        }

        RequestValidationLog.AuthorizeRequestValidationSuccess(_logger);

        return Valid(request);
    }

    /// <summary>
    /// RFC 9126 section 4: a request_uri issued by the pushed authorization request endpoint replaces the request
    /// parameters with the pushed ones. Only client_id is taken from the authorize request, and it must match.
    /// </summary>
    private async Task<AuthorizeRequestValidationResult> LoadPushedAuthorizationRequestAsync(ValidatedAuthorizeRequest request, AuthorizeRequestType requestType)
    {
        var requestUri = request.Raw.Get(OidcConstants.AuthorizeRequest.RequestUri);
        var isPushed = requestUri != null && requestUri.StartsWith(Constants.PushedAuthorization.RequestUriPrefix, StringComparison.Ordinal);

        if (requestType == AuthorizeRequestType.PushedAuthorization)
        {
            if (requestUri != null)
            {
                LogError("request_uri is not allowed in a pushed authorization request", request);
                return Invalid(request, description: "request_uri is not allowed in a pushed authorization request");
            }

            return Valid(request);
        }

        if (requestUri is null || !isPushed)
        {
            if (_options.Endpoints.EnablePushedAuthorizationEndpoint &&
                (_options.PushedAuthorization.Required || request.Client.RequirePushedAuthorization))
            {
                LogError("Pushed authorization is required", request);
                return Invalid(request, description: "Pushed authorization is required");
            }

            return Valid(request);
        }

        if (!_options.Endpoints.EnablePushedAuthorizationEndpoint)
        {
            LogError("Pushed authorization request_uri used while the endpoint is disabled", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidRequestUri, "Invalid request_uri");
        }

        if (request.Raw.Get(OidcConstants.AuthorizeRequest.Request) != null)
        {
            LogError("Both request and request_uri are present", request);
            return Invalid(request, description: "Only one request parameter is allowed");
        }

        var referenceValue = requestUri.Substring(Constants.PushedAuthorization.RequestUriPrefix.Length);
        if (referenceValue.IsMissing() || requestUri.Length > 512)
        {
            LogError("Invalid pushed authorization request_uri", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidRequestUri, "Invalid request_uri");
        }

        var pushed = await _pushedAuthorizationRequestStore.GetPushedAuthorizationRequestAsync(referenceValue);
        if (pushed == null || pushed.CreationTime.HasExceeded(pushed.Lifetime, _clock.GetUtcNow().UtcDateTime))
        {
            LogError("Unknown, used or expired pushed authorization request_uri", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidRequestUri, "Invalid request_uri");
        }

        if (!string.Equals(pushed.ClientId, request.Client.ClientId, StringComparison.Ordinal))
        {
            LogError("Pushed authorization request belongs to a different client", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidRequestUri, "Invalid request_uri");
        }

        var parameters = pushed.Parameters.FromFullDictionary();
        parameters.Remove(OidcConstants.AuthorizeRequest.RequestUri);
        parameters.Set(OidcConstants.AuthorizeRequest.ClientId, pushed.ClientId);

        request.Raw = parameters;
        request.PushedAuthorizationReferenceValue = referenceValue;
        request.PushedAuthorizationRequest = pushed;

        return Valid(request);
    }

    // OAuth 2.1 / RFC 9101 section 10.4: request_uri values must be pre-registered so the server cannot be
    // used to fetch arbitrary URLs. Matching is exact; a fragment (often a content hash) is ignored.
    private static bool IsRegisteredRequestUri(string requestUri, Client client)
    {
        if (client?.RequestUris == null)
        {
            return false;
        }

        static string WithoutFragment(string uri)
        {
            var index = uri.IndexOf('#', StringComparison.Ordinal);
            return index < 0 ? uri : uri.Substring(0, index);
        }

        var requested = WithoutFragment(requestUri);
        return client.RequestUris.Any(registered => registered != null && string.Equals(WithoutFragment(registered), requested, StringComparison.Ordinal));
    }

    private async Task<AuthorizeRequestValidationResult> LoadRequestObjectAsync(ValidatedAuthorizeRequest request)
    {
        var jwtRequest = request.Raw.Get(OidcConstants.AuthorizeRequest.Request);
        var jwtRequestUri = request.Raw.Get(OidcConstants.AuthorizeRequest.RequestUri);

        if (jwtRequest.IsPresent() && jwtRequestUri.IsPresent())
        {
            LogError("Both request and request_uri are present", request);
            return Invalid(request, description: "Only one request parameter is allowed");
        }

        if (_options.Endpoints.EnableJwtRequestUri)
        {
            if (jwtRequestUri.IsPresent())
            {
                if (jwtRequestUri.Length > 512)
                {
                    LogError("request_uri is too long", request);
                    return Invalid(request, error: OidcConstants.AuthorizeErrors.InvalidRequestUri, description: "request_uri is too long");
                }

                if (!IsRegisteredRequestUri(jwtRequestUri, request.Client))
                {
                    LogError("request_uri is not registered for the client", jwtRequestUri, request);
                    return Invalid(request, error: OidcConstants.AuthorizeErrors.InvalidRequestUri, description: "request_uri is not registered for the client");
                }

                var jwt = await _jwtRequestUriHttpClient.GetJwtAsync(jwtRequestUri, request.Client);
                if (jwt.IsMissing())
                {
                    LogError("no value returned from request_uri", request);
                    return Invalid(request, error: OidcConstants.AuthorizeErrors.InvalidRequestUri, description: "no value returned from request_uri");
                }

                jwtRequest = jwt;
            }
        }
        else if (jwtRequestUri.IsPresent())
        {
            LogError("request_uri present but config prohibits", request);
            return Invalid(request, error: OidcConstants.AuthorizeErrors.RequestUriNotSupported);
        }

        if (jwtRequest.IsPresent())
        {
            if (jwtRequest.Length >= _options.InputLengthRestrictions.Jwt)
            {
                LogError("request value is too long", request);
                return Invalid(request, error: OidcConstants.AuthorizeErrors.InvalidRequestObject, description: "Invalid request value");
            }
        }

        request.RequestObject = jwtRequest;
        return Valid(request);
    }

    private async Task<AuthorizeRequestValidationResult> LoadClientAsync(ValidatedAuthorizeRequest request)
    {
        var clientId = request.Raw.Get(OidcConstants.AuthorizeRequest.ClientId);

        if (clientId.IsMissingOrTooLong(_options.InputLengthRestrictions.ClientId))
        {
            LogError("client_id is missing or too long", request);
            return Invalid(request, description: "Invalid client_id");
        }

        request.ClientId = clientId;

        var client = await _clients.FindEnabledClientByIdAsync(request.ClientId);
        if (client == null)
        {
            LogError("Unknown client or not enabled", request.ClientId, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, "Unknown client or client not enabled");
        }

        request.SetClient(client);

        return Valid(request);
    }

    private async Task<AuthorizeRequestValidationResult> ValidateRequestObjectAsync(ValidatedAuthorizeRequest request)
    {
        if (request.RequestObject.IsPresent())
        {
            var jwtRequestValidationResult = await _jwtRequestValidator.ValidateAsync(request.Client, request.RequestObject);
            if (jwtRequestValidationResult.IsError)
            {
                LogError("request JWT validation failure", request);
                return Invalid(request, error: OidcConstants.AuthorizeErrors.InvalidRequestObject, description: "Invalid JWT request");
            }

            var responseType = request.Raw.Get(OidcConstants.AuthorizeRequest.ResponseType);
            if (responseType != null)
            {
                if (jwtRequestValidationResult.Payload.TryGetValue(OidcConstants.AuthorizeRequest.ResponseType, out var payloadResponseType))
                {
                    if (payloadResponseType != responseType)
                    {
                        LogError("response_type in JWT payload does not match response_type in request", request);
                        return Invalid(request, description: "Invalid JWT request");
                    }
                }
            }

            if (jwtRequestValidationResult.Payload.TryGetValue(OidcConstants.AuthorizeRequest.ClientId, out var payloadClientId))
            {
                if (!string.Equals(request.Client.ClientId, payloadClientId, StringComparison.Ordinal))
                {
                    LogError("client_id in JWT payload does not match client_id in request", request);
                    return Invalid(request, description: "Invalid JWT request");
                }
            }
            else
            {
                LogError("client_id is missing in JWT payload", request);
                return Invalid(request, error: OidcConstants.AuthorizeErrors.InvalidRequestObject, description: "Invalid JWT request");
            }

            var ignoreKeys = new[]
            {
                JwtClaimTypes.Issuer,
                JwtClaimTypes.Audience
            };

            foreach (var key in jwtRequestValidationResult.Payload.Keys)
            {
                if (ignoreKeys.Contains(key)) continue;

                var value = jwtRequestValidationResult.Payload[key];

                var qsValue = request.Raw.Get(key);
                if (qsValue != null)
                {
                    if (!string.Equals(value, qsValue, StringComparison.Ordinal))
                    {
                        LogError("parameter mismatch between request object and query string parameter.", request);
                        return Invalid(request, description: "Parameter mismatch in JWT request");
                    }
                }

                request.Raw.Set(key, value);
            }

            request.RequestObjectValues = jwtRequestValidationResult.Payload;
        }

        return Valid(request);
    }

    private async Task<AuthorizeRequestValidationResult> ValidateClientAsync(ValidatedAuthorizeRequest request)
    {
        if (request.Client.RequireRequestObject)
        {
            if (request.RequestObjectValues.Count == 0)
            {
                return Invalid(request, description: "Client must use request object, but no request or request_uri parameter present");
            }
        }

        var redirectUri = request.Raw.Get(OidcConstants.AuthorizeRequest.RedirectUri);

        if (redirectUri.IsMissingOrTooLong(_options.InputLengthRestrictions.RedirectUri))
        {
            LogError("redirect_uri is missing or too long", request);
            return Invalid(request, description: "Invalid redirect_uri");
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            LogError("malformed redirect_uri", redirectUri, request);
            return Invalid(request, description: "Invalid redirect_uri");
        }

        // RFC 6749 section 3.1.2: the redirection endpoint URI MUST NOT include a fragment component.
        if (redirectUri.Contains('#', StringComparison.Ordinal))
        {
            LogError("redirect_uri contains a fragment", redirectUri, request);
            return Invalid(request, description: "Invalid redirect_uri");
        }

        if (request.Client.ProtocolType != OidcServerConstants.ProtocolTypes.OpenIdConnect)
        {
            LogError("Invalid protocol type for OIDC authorize endpoint", request.Client.ProtocolType, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, description: "Invalid protocol");
        }

        if (await _uriValidator.IsRedirectUriValidAsync(redirectUri, request.Client) == false)
        {
            LogError("Invalid redirect_uri", redirectUri, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidRequest, "Invalid redirect_uri");
        }

        request.RedirectUri = redirectUri;

        return Valid(request);
    }

    private AuthorizeRequestValidationResult ValidateCoreParameters(ValidatedAuthorizeRequest request)
    {
        var state = request.Raw.Get(OidcConstants.AuthorizeRequest.State);
        if (state.IsPresent())
        {
            if (state.Length > _options.InputLengthRestrictions.State)
            {
                LogError("State too long", request);
                return Invalid(request, description: "Invalid state");
            }

            request.State = state;
        }

        var responseType = request.Raw.Get(OidcConstants.AuthorizeRequest.ResponseType);
        if (responseType.IsMissing())
        {
            LogError("Missing response_type", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnsupportedResponseType, "Missing response_type");
        }

        if (!Constants.SupportedResponseTypes.Contains(responseType, _responseTypeEqualityComparer))
        {
            LogError("Response type not supported", responseType, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnsupportedResponseType, "Response type not supported");
        }

        request.ResponseType = Constants.SupportedResponseTypes.First(
            supportedResponseType => _responseTypeEqualityComparer.Equals(supportedResponseType, responseType));

        request.GrantType = Constants.ResponseTypeToGrantTypeMapping[request.ResponseType];

        request.ResponseMode = Constants.AllowedResponseModesForGrantType[request.GrantType].First();

        if (!Constants.AllowedGrantTypesForAuthorizeEndpoint.Contains(request.GrantType))
        {
            LogError("Invalid grant type", request.GrantType, request);
            return Invalid(request, description: "Invalid response_type");
        }

        if (request.GrantType == GrantType.AuthorizationCode || request.GrantType == GrantType.Hybrid)
        {
            RequestValidationLog.CheckingPkceParameters(_logger);

            var proofKeyResult = ValidatePkceParameters(request);

            if (proofKeyResult.IsError)
            {
                return proofKeyResult;
            }
        }

        var responseMode = request.Raw.Get(OidcConstants.AuthorizeRequest.ResponseMode);
        if (responseMode.IsPresent())
        {
            if (Constants.SupportedResponseModes.Contains(responseMode))
            {
                if (Constants.AllowedResponseModesForGrantType[request.GrantType].Contains(responseMode))
                {
                    request.ResponseMode = responseMode;
                }
                else
                {
                    LogError("Invalid response_mode for response_type", responseMode, request);
                    return Invalid(request, OidcConstants.AuthorizeErrors.InvalidRequest, description: "Invalid response_mode for response_type");
                }
            }
            else
            {
                LogError("Unsupported response_mode", responseMode, request);
                return Invalid(request, OidcConstants.AuthorizeErrors.UnsupportedResponseType, description: "Invalid response_mode");
            }
        }

        if (!request.Client.AllowedGrantTypes.Contains(request.GrantType))
        {
            LogError("Invalid grant type for client", request.GrantType, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, "Invalid grant type for client");
        }

        if (request.GrantType == GrantType.Implicit && !request.Client.AllowImplicitFlow)
        {
            LogError("Client requested the deprecated implicit flow, but AllowImplicitFlow is not set", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, "Implicit flow not allowed for client");
        }

        // OAuth 2.1 / RFC 9700 section 2.1.2: access tokens from the authorize endpoint need an explicit opt-in.
        if (request.ResponseType.FromSpaceSeparatedString().Contains(OidcConstants.ResponseTypes.Token) &&
            !request.Client.AllowAccessTokensViaBrowser)
        {
            LogError("Client requested an access token from the authorize endpoint, but AllowAccessTokensViaBrowser is not set", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, "Client not configured to receive access tokens via browser");
        }

        return Valid(request);
    }

    private AuthorizeRequestValidationResult ValidatePkceParameters(ValidatedAuthorizeRequest request)
    {
        var fail = Invalid(request);

        var codeChallenge = request.Raw.Get(OidcConstants.AuthorizeRequest.CodeChallenge);
        if (codeChallenge.IsMissing())
        {
            if (request.Client.RequirePkce)
            {
                LogError("code_challenge is missing", request);
                fail.ErrorDescription = "code challenge required";
            }
            else
            {
                RequestValidationLog.NoPkceUsed(_logger);
                return Valid(request);
            }

            return fail;
        }

        if (codeChallenge.Length < _options.InputLengthRestrictions.CodeChallengeMinLength ||
            codeChallenge.Length > _options.InputLengthRestrictions.CodeChallengeMaxLength)
        {
            LogError("code_challenge is either too short or too long", request);
            fail.ErrorDescription = "Invalid code_challenge";
            return fail;
        }

        request.CodeChallenge = codeChallenge;

        var codeChallengeMethod = request.Raw.Get(OidcConstants.AuthorizeRequest.CodeChallengeMethod);
        if (codeChallengeMethod.IsMissing())
        {
            RequestValidationLog.DefaultingToPlainCodeChallengeMethod(_logger);
            codeChallengeMethod = OidcConstants.CodeChallengeMethods.Plain;
        }

        if (!Constants.SupportedCodeChallengeMethods.Contains(codeChallengeMethod))
        {
            LogError("Unsupported code_challenge_method", codeChallengeMethod, request);
            fail.ErrorDescription = "Transform algorithm not supported";
            return fail;
        }

        if (codeChallengeMethod == OidcConstants.CodeChallengeMethods.Plain)
        {
            if (!request.Client.AllowPlainTextPkce)
            {
                LogError("code_challenge_method of plain is not allowed", request);
                fail.ErrorDescription = "Transform algorithm not supported";
                return fail;
            }
        }

        request.CodeChallengeMethod = codeChallengeMethod;

        return Valid(request);
    }

    private async Task<AuthorizeRequestValidationResult> ValidateScopeAsync(ValidatedAuthorizeRequest request)
    {
        var scope = request.Raw.Get(OidcConstants.AuthorizeRequest.Scope);
        if (scope.IsMissing())
        {
            LogError("scope is missing", request);
            return Invalid(request, description: "Invalid scope");
        }

        if (scope.Length > _options.InputLengthRestrictions.Scope)
        {
            LogError("scopes too long.", request);
            return Invalid(request, description: "Invalid scope");
        }

        request.RequestedScopes = scope.FromSpaceSeparatedString().Distinct().ToList();

        if (request.RequestedScopes.Contains(OidcServerConstants.StandardScopes.OpenId))
        {
            request.IsOpenIdRequest = true;
        }

        var requirement = Constants.ResponseTypeToScopeRequirement[request.ResponseType];
        if (requirement == Constants.ScopeRequirement.Identity ||
            requirement == Constants.ScopeRequirement.IdentityOnly)
        {
            if (request.IsOpenIdRequest == false)
            {
                LogError("response_type requires the openid scope", request);
                return Invalid(request, description: "Missing openid scope");
            }
        }

        var resourceError = ResourceIndicators.TryRead(request.Raw, _options.InputLengthRestrictions, allowMultiple: true, out var resourceIndicators);
        if (resourceError != null)
        {
            LogError(resourceError, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidTarget, resourceError);
        }

        request.RequestedResourceIndicators = resourceIndicators;

        var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
        {
            Client = request.Client,
            Scopes = request.RequestedScopes,
            ResourceIndicators = resourceIndicators
        });

        if (!validatedResources.Succeeded)
        {
            if (validatedResources.InvalidScopes.Count == 0 && validatedResources.InvalidResourceIndicators.Count > 0)
            {
                LogError("Invalid resource indicator", request);
                return Invalid(request, OidcConstants.AuthorizeErrors.InvalidTarget, "Invalid resource indicator");
            }

            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidScope, "Invalid scope");
        }

        if (validatedResources.Resources.IdentityResources.Count > 0 && !request.IsOpenIdRequest)
        {
            LogError("Identity related scope requests, but no openid scope", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidScope, "Identity scopes requested, but openid scope is missing");
        }

        if (validatedResources.Resources.ApiScopes.Count > 0)
        {
            request.IsApiResourceRequest = true;
        }

        var responseTypeValidationCheck = true;
        switch (requirement)
        {
            case Constants.ScopeRequirement.Identity:
                if (validatedResources.Resources.IdentityResources.Count == 0)
                {
                    RequestValidationLog.IdTokenRequiresIdentityScopes(_logger);
                    responseTypeValidationCheck = false;
                }
                break;
            case Constants.ScopeRequirement.IdentityOnly:
                if (validatedResources.Resources.IdentityResources.Count == 0 || validatedResources.Resources.ApiScopes.Count > 0)
                {
                    RequestValidationLog.IdTokenOnlyMustNotIncludeResourceScopes(_logger);
                    responseTypeValidationCheck = false;
                }
                break;
            case Constants.ScopeRequirement.ResourceOnly:
                if (validatedResources.Resources.IdentityResources.Count > 0 || validatedResources.Resources.ApiScopes.Count == 0)
                {
                    RequestValidationLog.TokenOnlyRequiresResourceScopes(_logger);
                    responseTypeValidationCheck = false;
                }
                break;
        }

        if (!responseTypeValidationCheck)
        {
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidScope, "Invalid scope for response type");
        }

        request.ValidatedResources = validatedResources;

        return Valid(request);
    }

    private async Task<AuthorizeRequestValidationResult> ValidateOptionalParametersAsync(ValidatedAuthorizeRequest request)
    {
        var nonce = request.Raw.Get(OidcConstants.AuthorizeRequest.Nonce);
        if (nonce.IsPresent())
        {
            if (nonce.Length > _options.InputLengthRestrictions.Nonce)
            {
                LogError("Nonce too long", request);
                return Invalid(request, description: "Invalid nonce");
            }

            request.Nonce = nonce;
        }
        else
        {
            if (request.GrantType == GrantType.Implicit || request.GrantType == GrantType.Hybrid)
            {
                if (request.IsOpenIdRequest)
                {
                    LogError("Nonce required for implicit and hybrid flow with openid scope", request);
                    return Invalid(request, description: "Invalid nonce");
                }
            }
        }

        // RFC 9449 section 10: dpop_jkt binds the authorization code to the client's DPoP key.
        var dpopJkt = request.Raw.Get(OidcConstants.AuthorizeRequest.DPoPKeyThumbprint);
        if (dpopJkt != null)
        {
            if (dpopJkt.IsMissing() || dpopJkt.Length > 100 || request.GrantType != GrantType.AuthorizationCode)
            {
                LogError("Invalid dpop_jkt", request);
                return Invalid(request, description: "Invalid dpop_jkt");
            }

            request.DPoPKeyThumbprint = dpopJkt;
        }

        var prompt = request.Raw.Get(OidcConstants.AuthorizeRequest.Prompt);
        if (prompt.IsPresent())
        {
            var prompts = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (prompts.All(p => _options.UserInteraction.PromptValuesSupported.Contains(p)))
            {
                if (prompts.Contains(OidcConstants.PromptModes.None) && prompts.Length > 1)
                {
                    LogError("prompt contains 'none' and other values. 'none' should be used by itself.", request);
                    return Invalid(request, description: "Invalid prompt");
                }

                request.PromptModes = prompts;
            }
            else
            {
                LogError("Unsupported prompt mode", prompt, request);
                return Invalid(request, description: "Invalid prompt");
            }
        }

        var uilocales = request.Raw.Get(OidcConstants.AuthorizeRequest.UiLocales);
        if (uilocales.IsPresent())
        {
            if (uilocales.Length > _options.InputLengthRestrictions.UiLocale)
            {
                LogError("UI locale too long", request);
                return Invalid(request, description: "Invalid ui_locales");
            }

            request.UiLocales = uilocales;
        }

        var display = request.Raw.Get(OidcConstants.AuthorizeRequest.Display);
        if (display.IsPresent())
        {
            if (Constants.SupportedDisplayModes.Contains(display))
            {
                request.DisplayMode = display;
            }

            RequestValidationLog.UnsupportedDisplayMode(_logger, display);
        }

        var maxAge = request.Raw.Get(OidcConstants.AuthorizeRequest.MaxAge);
        if (maxAge.IsPresent())
        {
            if (int.TryParse(maxAge, out var seconds))
            {
                if (seconds >= 0)
                {
                    request.MaxAge = seconds;
                }
                else
                {
                    LogError("Invalid max_age.", request);
                    return Invalid(request, description: "Invalid max_age");
                }
            }
            else
            {
                LogError("Invalid max_age.", request);
                return Invalid(request, description: "Invalid max_age");
            }
        }

        var loginHint = request.Raw.Get(OidcConstants.AuthorizeRequest.LoginHint);
        if (loginHint.IsPresent())
        {
            if (loginHint.Length > _options.InputLengthRestrictions.LoginHint)
            {
                LogError("Login hint too long", request);
                return Invalid(request, description: "Invalid login_hint");
            }

            request.LoginHint = loginHint;
        }

        var loginHintToken = request.Raw.Get(NonceAuthorization.NonceTokenParameter);
        if (loginHintToken.IsPresent())
        {
            if (loginHintToken.Length > _options.InputLengthRestrictions.LoginHintToken)
            {
                LogError("Login hint token too long", request);
                return Invalid(request, description: "Invalid login_hint_token");
            }

            request.LoginHintToken = loginHintToken;
        }

        var acrValues = request.Raw.Get(OidcConstants.AuthorizeRequest.AcrValues);
        if (acrValues.IsPresent())
        {
            if (acrValues.Length > _options.InputLengthRestrictions.AcrValues)
            {
                LogError("Acr values too long", request);
                return Invalid(request, description: "Invalid acr_values");
            }

            request.AuthenticationContextReferenceClasses = acrValues.FromSpaceSeparatedString().Distinct().ToList();
        }

        var idp = request.GetIdP();
        if (idp.IsPresent())
        {
            if (request.Client.IdentityProviderRestrictions != null && request.Client.IdentityProviderRestrictions.Count > 0)
            {
                if (!request.Client.IdentityProviderRestrictions.Contains(idp))
                {
                    RequestValidationLog.IdpNotInClientRestrictions(_logger, idp);
                    request.RemoveIdP();
                }
            }
        }

        if (_options.Endpoints.EnableCheckSessionEndpoint)
        {
            if (request.Subject.IsAuthenticated())
            {
                var sessionId = await _userSession.GetSessionIdAsync();
                if (sessionId.IsPresent())
                {
                    request.SessionId = sessionId;
                }
                else
                {
                    LogError("Check session endpoint enabled, but SessionId is missing", request);
                }
            }
            else
            {
                request.SessionId = "";
            }
        }

        if (request.IsNoncePatternFlow)
        {
            if (request.LoginHintToken.IsMissing())
            {
                LogError("Acr value 'urn:auth:nonce' requires login_hint_token", request);
                return Invalid(request, description: "Missing login_hint_token");
            }
        }

        return Valid(request);
    }

    private AuthorizeRequestValidationResult Invalid(ValidatedAuthorizeRequest request, string? error = OidcConstants.AuthorizeErrors.InvalidRequest, string? description = null)
    {
        return new AuthorizeRequestValidationResult(request, error, description);
    }

    private AuthorizeRequestValidationResult Valid(ValidatedAuthorizeRequest request)
    {
        return new AuthorizeRequestValidationResult(request);
    }

    private void LogError(string message, ValidatedAuthorizeRequest request)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            var requestDetails = new AuthorizeRequestValidationLog(request, _options.Logging.AuthorizeRequestSensitiveValuesFilter);
            RequestValidationLog.AuthorizeRequestError(_logger, message, requestDetails);
        }
    }

    private void LogError(string message, string detail, ValidatedAuthorizeRequest request)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            var requestDetails = new AuthorizeRequestValidationLog(request, _options.Logging.AuthorizeRequestSensitiveValuesFilter);
            RequestValidationLog.AuthorizeRequestErrorWithDetail(_logger, message, detail, requestDetails);
        }
    }
}
