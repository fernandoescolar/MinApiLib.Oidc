namespace MinApiLib.Oidc.Server.Features.Interaction;

/// <summary>
/// Options for aspects of the user interface.
/// </summary>
public class UserInteractionOptions
{
    /// <summary>
    /// Gets or sets the login URL. If a local URL, the value must start with a leading slash. Defaults to
    /// <c>/account/login</c>.
    /// </summary>
    /// <value>
    /// The login URL.
    /// </value>
    public string LoginUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.Login;

    /// <summary>
    /// Gets or sets the login return URL parameter. Defaults to <c>returnUrl</c>.
    /// </summary>
    /// <value>
    /// The login return URL parameter.
    /// </value>
    public string LoginReturnUrlParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Login;

    /// <summary>
    /// Gets or sets the logout URL. If a local URL, the value must start with a leading slash. Defaults to
    /// <c>/account/logout</c>.
    /// </summary>
    /// <value>
    /// The logout URL.
    /// </value>
    public string LogoutUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.Logout;

    /// <summary>
    /// Gets or sets the logout identifier parameter.
    /// </summary>
    /// <value>
    /// The logout identifier parameter.
    /// </value>
    public string LogoutIdParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Logout;

    /// <summary>
    /// Gets or sets the consent URL. If a local URL, the value must start with a leading slash.
    /// </summary>
    /// <value>
    /// The consent URL.
    /// </value>
    public string ConsentUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.Consent;

    /// <summary>
    /// Gets or sets the consent return URL parameter.
    /// </summary>
    /// <value>
    /// The consent return URL parameter.
    /// </value>
    public string ConsentReturnUrlParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Consent;

    /// <summary>
    /// Gets or sets the error URL. If a local URL, the value must start with a leading slash.
    /// </summary>
    /// <value>
    /// The error URL.
    /// </value>
    public string ErrorUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.Error;

    /// <summary>
    /// Gets or sets the error identifier parameter.
    /// </summary>
    /// <value>
    /// The error identifier parameter.
    /// </value>
    public string ErrorIdParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Error;

    /// <summary>
    /// Gets or sets the custom redirect return URL parameter.
    /// </summary>
    /// <value>
    /// The custom redirect return URL parameter.
    /// </value>
    public string CustomRedirectReturnUrlParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Custom;

    /// <summary>
    /// Gets or sets the cookie message threshold. This limits how many cookies are created, and older ones will be purged.
    /// </summary>
    /// <value>
    /// The cookie message threshold.
    /// </value>
    public int CookieMessageThreshold { get; set; } = Constants.UIConstants.CookieMessageThreshold;

    /// <summary>
    /// Gets or sets the device verification URL.  If a local URL, the value must start with a leading slash.
    /// </summary>
    /// <value>
    /// The device verification URL.
    /// </value>
    public string DeviceVerificationUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.DeviceVerification;

    /// <summary>
    /// Gets or sets the device verification user code paramater.
    /// </summary>
    /// <value>
    /// The device verification user code parameter.
    /// </value>
    public string DeviceVerificationUserCodeParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.UserCode;

    /// <summary>
    /// Gets the <c>prompt</c> values the authorize endpoint accepts and discovery publishes as
    /// <c>prompt_values_supported</c>. Defaults to <c>none</c>, <c>login</c>, <c>consent</c> and <c>select_account</c>.
    /// </summary>
    /// <remarks>
    /// <c>AddPromptHandler</c> and <c>AddPromptRedirect</c> add their value here. A value added by hand needs a custom
    /// <see cref="MinApiLib.Oidc.Server.Features.Authorize.IAuthorizeInteractionResponseGenerator"/> to act on it; otherwise it is
    /// accepted and ignored.
    /// </remarks>
    public ICollection<string> PromptValuesSupported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        OidcConstants.PromptModes.None,
        OidcConstants.PromptModes.Login,
        OidcConstants.PromptModes.Consent,
        OidcConstants.PromptModes.SelectAccount
    };
}
