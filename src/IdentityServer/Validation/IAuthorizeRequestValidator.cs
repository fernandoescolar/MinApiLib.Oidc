namespace IdentityServer.Validation;

/// <summary>
/// The kind of authorize request being validated.
/// </summary>
public enum AuthorizeRequestType
{
    /// <summary>
    /// A request to the authorize endpoint.
    /// </summary>
    Authorize,

    /// <summary>
    /// Parameters sent to the pushed authorization request endpoint (RFC 9126).
    /// </summary>
    PushedAuthorization
}

/// <summary>
///  Authorize endpoint request validator.
/// </summary>
public interface IAuthorizeRequestValidator
{
    /// <summary>
    ///  Validates authorize request parameters.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="subject"></param>
    /// <returns></returns>
    Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal? subject = null);

    /// <summary>
    /// Validates authorize request parameters of the given kind. The default implementation ignores the kind.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="subject">The current user, if any.</param>
    /// <param name="requestType">The kind of request.</param>
    Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal? subject, AuthorizeRequestType requestType)
        => ValidateAsync(parameters, subject);
}
