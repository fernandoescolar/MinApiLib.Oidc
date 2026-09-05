namespace IdentityServer.ResponseHandling;

/// <summary>
/// Handles a custom <c>prompt</c> value at the authorize endpoint, such as the OpenID Connect <c>create</c> value or an
/// application-specific one like <c>register</c>.
/// </summary>
/// <remarks>
/// Register a handler with <c>AddPromptHandler&lt;T&gt;(prompt)</c>, which also adds the value to
/// <see cref="IdentityServer.Configuration.UserInteractionOptions.PromptValuesSupported"/>. The handler runs before the
/// login and consent checks. When it returns a response that needs interaction (login, consent, a redirect or an
/// error), the prompt is removed from the request, so the return URL handed to the page does not trigger it again.
/// Returning an empty <see cref="InteractionResponse"/> lets the request continue with the normal login and consent
/// processing.
/// </remarks>
public interface IAuthorizePromptHandler
{
    /// <summary>
    /// Processes the prompt.
    /// </summary>
    /// <param name="request">The validated authorize request.</param>
    /// <param name="prompt">The prompt value being handled.</param>
    Task<InteractionResponse> ProcessAsync(ValidatedAuthorizeRequest request, string prompt);
}

/// <summary>
/// Sends the user to a page, with the authorize request as return URL. Registered by <c>AddPromptRedirect</c>.
/// </summary>
internal sealed class RedirectPromptHandler : IAuthorizePromptHandler
{
    private readonly string _url;

    public RedirectPromptHandler(string url)
    {
        _url = url;
    }

    public Task<InteractionResponse> ProcessAsync(ValidatedAuthorizeRequest request, string prompt)
        => Task.FromResult(new InteractionResponse { RedirectUrl = _url });
}
