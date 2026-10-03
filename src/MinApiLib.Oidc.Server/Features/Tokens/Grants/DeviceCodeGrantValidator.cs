namespace MinApiLib.Oidc.Server.Features.Tokens.Grants;

/// <summary>
/// Validates device code (<c>urn:ietf:params:oauth:grant-type:device_code</c>) token requests.
/// </summary>
internal sealed class DeviceCodeGrantValidator : ITokenRequestGrantValidator
{
    private readonly IDeviceCodeValidator _deviceCodeValidator;

    public DeviceCodeGrantValidator(IDeviceCodeValidator deviceCodeValidator)
    {
        _deviceCodeValidator = deviceCodeValidator;
    }

    public async Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context)
    {
        var request = context.Request;

        RequestValidationLog.StartDeviceCodeValidation(context.Logger);

        if (!request.Client.AllowedGrantTypes.ToList().Contains(GrantType.DeviceFlow))
        {
            context.LogError("Client not authorized for device flow");
            return context.Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        var deviceCode = context.Parameters.Get(OidcConstants.TokenRequest.DeviceCode);
        if (deviceCode.IsMissing())
        {
            context.LogError("Device code is missing");
            return context.Invalid(OidcConstants.TokenErrors.InvalidRequest);
        }

        if (deviceCode.Length > context.Options.InputLengthRestrictions.DeviceCode)
        {
            context.LogError("Device code too long");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        var deviceCodeContext = new DeviceCodeValidationContext { DeviceCode = deviceCode, Request = request };
        await _deviceCodeValidator.ValidateAsync(deviceCodeContext);

        if (deviceCodeContext.Result.IsError) return deviceCodeContext.Result;

        RequestValidationLog.AuthorizationCodeValidationSuccess(context.Logger);

        return context.Valid();
    }
}
