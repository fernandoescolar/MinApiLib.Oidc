namespace MinApiLib.OidcServer.Core.Logging;

/// <summary>
/// Options for configuring logging behavior
/// </summary>
public class LoggingOptions
{
    /// <summary>
    ///
    /// </summary>
    public ICollection<string> TokenRequestSensitiveValuesFilter { get; set; } =
        new HashSet<string>
        {
            OidcConstants.TokenRequest.ClientSecret,
            OidcConstants.TokenRequest.Password,
            OidcConstants.TokenRequest.ClientAssertion,
            OidcConstants.TokenRequest.RefreshToken,
            OidcConstants.TokenRequest.DeviceCode,
            OidcConstants.TokenRequest.Code,
            OidcConstants.TokenRequest.CodeVerifier
        };

    /// <summary>
    ///
    /// </summary>
    public ICollection<string> AuthorizeRequestSensitiveValuesFilter { get; set; } =
        new HashSet<string>
        {
            OidcConstants.AuthorizeRequest.IdTokenHint,
            OidcServerConstants.NonceAuthorization.NonceTokenParameter
        };
}
