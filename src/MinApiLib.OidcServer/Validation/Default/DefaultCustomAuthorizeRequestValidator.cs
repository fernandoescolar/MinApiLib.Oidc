namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Default custom request validator
/// </summary>
[RegisterInIServiceCollection(typeof(ICustomAuthorizeRequestValidator))]
internal class DefaultCustomAuthorizeRequestValidator : ICustomAuthorizeRequestValidator
{
    /// <summary>
    /// Custom validation logic for the authorize request.
    /// </summary>
    /// <param name="context">The context.</param>
    public Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
    {
        return Task.CompletedTask;
    }
}
