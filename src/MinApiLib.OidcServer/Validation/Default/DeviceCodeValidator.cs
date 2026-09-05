namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validates an incoming token request using the device flow
/// </summary>
[RegisterInIServiceCollection(typeof(IDeviceCodeValidator))]
internal class DeviceCodeValidator : IDeviceCodeValidator
{
    private readonly IDeviceFlowCodeService _devices;
    private readonly IProfileService _profile;
    private readonly IDeviceFlowThrottlingService _throttlingService;
    private readonly TimeProvider _systemClock;
    private readonly ILogger<DeviceCodeValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceCodeValidator"/> class.
    /// </summary>
    /// <param name="devices">The devices.</param>
    /// <param name="profile">The profile.</param>
    /// <param name="throttlingService">The throttling service.</param>
    /// <param name="systemClock">The system clock.</param>
    /// <param name="logger">The logger.</param>
    public DeviceCodeValidator(
        IDeviceFlowCodeService devices,
        IProfileService profile,
        IDeviceFlowThrottlingService throttlingService,
        TimeProvider systemClock,
        ILogger<DeviceCodeValidator> logger)
    {
        _devices = devices;
        _profile = profile;
        _throttlingService = throttlingService;
        _systemClock = systemClock;
        _logger = logger;
    }

    /// <summary>
    /// Validates the device code.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public async Task ValidateAsync(DeviceCodeValidationContext context)
    {
        var deviceCode = await _devices.FindByDeviceCodeAsync(context.DeviceCode);

        if (deviceCode == null)
        {
            RequestValidationLog.InvalidDeviceCode(_logger);
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.InvalidGrant);
            return;
        }

        if (deviceCode.ClientId != context.Request.Client.ClientId)
        {
            RequestValidationLog.DeviceCodeClientMismatch(_logger, context.Request.Client.ClientId, deviceCode.ClientId);
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.InvalidGrant);
            return;
        }

        if (await _throttlingService.ShouldSlowDown(context.DeviceCode, deviceCode))
        {
            RequestValidationLog.DeviceCodePollingTooFast(_logger, deviceCode.ClientId);
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.SlowDown);
            return;
        }

        if (deviceCode.CreationTime.AddSeconds(deviceCode.Lifetime) < _systemClock.GetUtcNow())
        {
            RequestValidationLog.DeviceCodeExpired(_logger);
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.ExpiredToken);
            return;
        }

        if (deviceCode.IsAuthorized
            && (deviceCode.AuthorizedScopes == null || deviceCode.AuthorizedScopes.Any() == false))
        {
            RequestValidationLog.DeviceCodeNoScopesAuthorized(_logger);
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.AccessDenied);
            return;
        }

        if (!deviceCode.IsAuthorized || deviceCode.Subject == null)
        {
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.AuthorizationPending);
            return;
        }

        var isActiveCtx = new IsActiveContext(deviceCode.Subject, context.Request.Client, OidcServerConstants.ProfileIsActiveCallers.DeviceCodeValidation);
        await _profile.IsActiveAsync(isActiveCtx);

        if (isActiveCtx.IsActive == false)
        {
            RequestValidationLog.DeviceCodeUserDisabled(_logger, deviceCode.Subject.GetSubjectId());
            context.Result = new TokenRequestValidationResult(context.Request, OidcConstants.TokenErrors.InvalidGrant);
            return;
        }

        context.Request.DeviceCode = deviceCode;
        context.Request.SessionId = deviceCode.SessionId;

        context.Result = new TokenRequestValidationResult(context.Request);
        await _devices.RemoveByDeviceCodeAsync(context.DeviceCode);
    }
}
