namespace MinApiLib.Oidc.Server.Features.Tokens;

/// <summary>
/// Default custom request validator
/// </summary>
[RegisterInIServiceCollection(typeof(ICustomTokenRequestValidator))]
internal class DefaultCustomTokenRequestValidator : ICustomTokenRequestValidator
{
    /// <summary>
    /// Custom validation logic for a token request.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>
    /// The validation result
    /// </returns>
    public Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
        return Task.CompletedTask;
    }
}
