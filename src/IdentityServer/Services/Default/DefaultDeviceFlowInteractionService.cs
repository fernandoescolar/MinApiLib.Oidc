namespace IdentityServer.Services;

internal class DefaultDeviceFlowInteractionService : IDeviceFlowInteractionService
{
    private readonly IClientStore _clients;
    private readonly IUserSession _session;
    private readonly IDeviceFlowCodeService _devices;
    private readonly IResourceStore _resourceStore;
    private readonly IScopeParser _scopeParser;
    private readonly IDistributedCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityServerOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<DefaultDeviceFlowInteractionService> _logger;

    private const string FailedAttemptsKeyPrefix = "devicecode_userfailures_";

    public DefaultDeviceFlowInteractionService(
        IClientStore clients,
        IUserSession session,
        IDeviceFlowCodeService devices,
        IResourceStore resourceStore,
        IScopeParser scopeParser,
        IDistributedCache cache,
        IHttpContextAccessor httpContextAccessor,
        IdentityServerOptions options,
        TimeProvider clock,
        ILogger<DefaultDeviceFlowInteractionService> logger)
    {
        _clients = clients;
        _session = session;
        _devices = devices;
        _resourceStore = resourceStore;
        _scopeParser = scopeParser;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DeviceFlowAuthorizationRequest?> GetAuthorizationContextAsync(string userCode)
    {
        var deviceAuth = await FindByUserCodeAsync(userCode);
        if (deviceAuth == null) return null;

        var client = await _clients.FindClientByIdAsync(deviceAuth.ClientId);
        if (client == null) return null;

        var parsedScopesResult = _scopeParser.ParseScopeValues(deviceAuth.RequestedScopes);
        var validatedResources = await _resourceStore.CreateResourceValidationResult(parsedScopesResult);

        return new DeviceFlowAuthorizationRequest
        {
            Client = client,
            ValidatedResources = validatedResources
        };
    }

    public async Task<DeviceFlowInteractionResult> HandleRequestAsync(string userCode, ConsentResponse consent)
    {
        ArgumentNullException.ThrowIfNull(userCode);
        ArgumentNullException.ThrowIfNull(consent);

        var deviceAuth = await FindByUserCodeAsync(userCode);
        if (deviceAuth == null) return LogAndReturnError("Invalid user code", "Device authorization failure - user code is invalid");

        var client = await _clients.FindClientByIdAsync(deviceAuth.ClientId);
        if (client == null) return LogAndReturnError("Invalid client", "Device authorization failure - requesting client is invalid");

        var subject = await _session.GetUserAsync();
        if (subject == null) return LogAndReturnError("No user present in device flow request", "Device authorization failure - no user found");

        var requestedScopes = deviceAuth.RequestedScopes ?? [];
        if (consent.ScopesValuesConsented?.Any(scope => !requestedScopes.Contains(scope)) == true)
        {
            return LogAndReturnError("Invalid scopes", "Device authorization failure - consented scopes were not requested by the device");
        }

        var sid = await _session.GetSessionIdAsync();

        deviceAuth.IsAuthorized = true;
        deviceAuth.Subject = subject;
        deviceAuth.SessionId = sid;
        deviceAuth.Description = consent.Description;
        deviceAuth.AuthorizedScopes = consent.ScopesValuesConsented;

        if (consent.RememberConsent)
        {
        }

        await _devices.UpdateByUserCodeAsync(userCode, deviceAuth);

        return new DeviceFlowInteractionResult();
    }

    // Counts failed lookups per user (or per client IP address) to limit user code brute forcing (RFC 8628 section 5.1).
    private async Task<DeviceCode?> FindByUserCodeAsync(string userCode)
    {
        var maxAttempts = _options.DeviceFlow.UserCodeMaxFailedAttempts;
        if (maxAttempts <= 0)
        {
            return await _devices.FindByUserCodeAsync(userCode);
        }

        var key = await GetFailedAttemptsKeyAsync();
        var failures = int.TryParse(await _cache.GetStringAsync(key), out var count) ? count : 0;
        if (failures >= maxAttempts)
        {
            ServicesLog.TooManyFailedUserCodeAttempts(_logger);
            return null;
        }

        var deviceAuth = await _devices.FindByUserCodeAsync(userCode);
        if (deviceAuth == null)
        {
            await _cache.SetStringAsync(key, (failures + 1).ToString(CultureInfo.InvariantCulture), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = _clock.GetUtcNow().Add(_options.DeviceFlow.UserCodeFailedAttemptsWindow)
            });
        }

        return deviceAuth;
    }

    private async Task<string> GetFailedAttemptsKeyAsync()
    {
        var user = await _session.GetUserAsync();
        var identifier = user?.GetSubjectId()
            ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return FailedAttemptsKeyPrefix + identifier.Sha256();
    }

    private DeviceFlowInteractionResult LogAndReturnError(string error, string errorDescription)
    {
        ServicesLog.DeviceFlowInteractionError(_logger, errorDescription);
        return DeviceFlowInteractionResult.Failure(error);
    }
}
