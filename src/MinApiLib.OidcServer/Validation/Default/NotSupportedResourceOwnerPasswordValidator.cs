namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Default resource owner password validator: without an implementation the password grant is not supported.
/// </summary>
[RegisterInIServiceCollection(typeof(IResourceOwnerPasswordValidator))]
public class NotSupportedResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotSupportedResourceOwnerPasswordValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NotSupportedResourceOwnerPasswordValidator(ILogger<NotSupportedResourceOwnerPasswordValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Result = new GrantValidationResult(TokenRequestErrors.UnsupportedGrantType);
        RequestValidationLog.ResourceOwnerPasswordNotSupported(_logger);

        return Task.CompletedTask;
    }
}
