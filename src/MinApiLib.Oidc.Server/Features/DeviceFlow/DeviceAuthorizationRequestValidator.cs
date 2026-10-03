namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

[RegisterInIServiceCollection(typeof(IDeviceAuthorizationRequestValidator))]
internal class DeviceAuthorizationRequestValidator : IDeviceAuthorizationRequestValidator
{
    private readonly OidcServerOptions _options;
    private readonly IResourceValidator _resourceValidator;
    private readonly ILogger<DeviceAuthorizationRequestValidator> _logger;

    public DeviceAuthorizationRequestValidator(
        OidcServerOptions options,
        IResourceValidator resourceValidator,
        ILogger<DeviceAuthorizationRequestValidator> logger)
    {
        _options = options;
        _resourceValidator = resourceValidator;
        _logger = logger;
    }

    public async Task<DeviceAuthorizationRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult)
    {
        RequestValidationLog.StartDeviceAuthorizationValidation(_logger);

        var request = new ValidatedDeviceAuthorizationRequest
        {
            Raw = parameters ?? throw new ArgumentNullException(nameof(parameters)),
            Options = _options
        };

        var clientResult = ValidateClient(request, clientValidationResult);
        if (clientResult.IsError)
        {
            return clientResult;
        }

        var scopeResult = await ValidateScopeAsync(request);
        if (scopeResult.IsError)
        {
            return scopeResult;
        }

        RequestValidationLog.DeviceAuthorizationValidationSuccess(_logger, request.Client.ClientId);
        return Valid(request);
    }

    private DeviceAuthorizationRequestValidationResult Valid(ValidatedDeviceAuthorizationRequest request)
    {
        return new DeviceAuthorizationRequestValidationResult(request);
    }

    private DeviceAuthorizationRequestValidationResult Invalid(ValidatedDeviceAuthorizationRequest request, string error = OidcConstants.AuthorizeErrors.InvalidRequest, string? description = null)
    {
        return new DeviceAuthorizationRequestValidationResult(request, error, description);
    }

    private void LogError(string message, ValidatedDeviceAuthorizationRequest request)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            var requestDetails = new DeviceAuthorizationRequestValidationLog(request);
            RequestValidationLog.DeviceAuthorizationRequestError(_logger, message, requestDetails);
        }
    }

    private void LogError(string message, string detail, ValidatedDeviceAuthorizationRequest request)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            var requestDetails = new DeviceAuthorizationRequestValidationLog(request);
            RequestValidationLog.DeviceAuthorizationRequestErrorWithDetail(_logger, message, detail, requestDetails);
        }
    }

    private DeviceAuthorizationRequestValidationResult ValidateClient(ValidatedDeviceAuthorizationRequest request, ClientSecretValidationResult clientValidationResult)
    {
        ArgumentNullException.ThrowIfNull(clientValidationResult);
        request.SetClient(clientValidationResult.Client, clientValidationResult.Secret);

        if (request.Client.ProtocolType != OidcServerConstants.ProtocolTypes.OpenIdConnect)
        {
            LogError("Invalid protocol type for OIDC authorize endpoint", request.Client.ProtocolType, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, "Invalid protocol");
        }

        if (!request.Client.AllowedGrantTypes.Contains(GrantType.DeviceFlow))
        {
            LogError("Client not configured for device flow", GrantType.DeviceFlow, request);
            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient);
        }

        return Valid(request);
    }

    private async Task<DeviceAuthorizationRequestValidationResult> ValidateScopeAsync(ValidatedDeviceAuthorizationRequest request)
    {
        var scope = request.Raw.Get(OidcConstants.AuthorizeRequest.Scope);
        if (scope.IsMissing())
        {
            RequestValidationLog.NoScopesProvided(_logger);

            if (request.Client.AllowedScopes != null && request.Client.AllowedScopes.Count > 0)
            {
                var clientAllowedScopes = new List<string>(request.Client.AllowedScopes);
                if (request.Client.AllowOfflineAccess)
                {
                    clientAllowedScopes.Add(OidcServerConstants.StandardScopes.OfflineAccess);
                }
                scope = clientAllowedScopes.ToSpaceSeparatedString();
                RequestValidationLog.DefaultingToScopes(_logger, scope);
            }
            else
            {
                LogError("No allowed scopes configured for client", request);
                return Invalid(request, OidcConstants.AuthorizeErrors.InvalidScope);
            }
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

        var validatedResources = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
        {
            Client = request.Client,
            Scopes = request.RequestedScopes
        });

        if (!validatedResources.Succeeded)
        {
            if (validatedResources.InvalidScopes.Count > 0)
            {
                return Invalid(request, OidcConstants.AuthorizeErrors.InvalidScope);
            }

            return Invalid(request, OidcConstants.AuthorizeErrors.UnauthorizedClient, "Invalid scope");
        }

        if (validatedResources.Resources.IdentityResources.Count > 0 && !request.IsOpenIdRequest)
        {
            LogError("Identity related scope requests, but no openid scope", request);
            return Invalid(request, OidcConstants.AuthorizeErrors.InvalidScope);
        }

        request.ValidatedResources = validatedResources;

        return Valid(request);
    }
}
