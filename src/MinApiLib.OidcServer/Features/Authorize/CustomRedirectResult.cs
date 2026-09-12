namespace MinApiLib.OidcServer.Features.Authorize;

/// <summary>
/// Result for a custom redirect
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Core.Hosting.IEndpointResult" />
public class CustomRedirectResult : IEndpointResult
{
    private readonly ValidatedAuthorizeRequest _request;
    private readonly string _url;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRedirectResult"/> class.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="url">The URL.</param>
    /// <exception cref="System.ArgumentNullException">
    /// request
    /// or
    /// url
    /// </exception>
    public CustomRedirectResult(ValidatedAuthorizeRequest request, string url)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (url.IsMissing()) throw new ArgumentNullException(nameof(url));

        _request = request;
        _url = url;
    }

    internal CustomRedirectResult(
        ValidatedAuthorizeRequest request,
        string url,
        OidcServerOptions options,
        IAuthorizationParametersMessageStore? authorizationParametersMessageStore = null)
        : this(request, url)
    {
        _options = options;
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    private OidcServerOptions _options = null!;
    private IAuthorizationParametersMessageStore? _authorizationParametersMessageStore;

    private TimeProvider _clock = null!;

    private void Init(HttpContext context)
    {
        _clock = _clock ?? context.RequestServices?.GetService<TimeProvider>() ?? TimeProvider.System;
        _options = _options ?? context.RequestServices!.GetRequiredService<OidcServerOptions>();
        _authorizationParametersMessageStore = _authorizationParametersMessageStore ?? context.RequestServices!.GetService<IAuthorizationParametersMessageStore>();
    }

    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task ExecuteAsync(HttpContext context)
    {
        Init(context);

        var returnUrl = context.GetOidcServerBasePath().EnsureTrailingSlash() + Constants.ProtocolRoutePaths.Authorize;
        var pushedReturnUrl = await PushedAuthorizationReturnUrl.TryCreateAsync(context, _request, returnUrl);
        if (pushedReturnUrl != null)
        {
            returnUrl = pushedReturnUrl;
        }
        else if (_authorizationParametersMessageStore != null)
        {
            var msg = new Message<IDictionary<string, string[]>>(_request.Raw.ToFullDictionary(), _clock.GetUtcNow().UtcDateTime);
            var id = await _authorizationParametersMessageStore.WriteAsync(msg);
            returnUrl = returnUrl.AddQueryString(Constants.AuthorizationParamsStore.MessageStoreIdParameterName, id);
        }
        else
        {
            returnUrl = returnUrl.AddQueryString(_request.Raw.ToQueryString());
        }

        if (!_url.IsLocalUrl())
        {
            returnUrl = context.GetOidcServerBaseUrl().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
        }

        var url = _url.AddQueryString(_options.UserInteraction.CustomRedirectReturnUrlParameter, returnUrl);
        context.Response.RedirectToAbsoluteUrl(url);
    }
}
