namespace MinApiLib.Oidc.Server.Features.Nonce;

/// <summary>
/// Validator for nonce requests
/// </summary>
public interface INonceRequestValidator
{
    /// <summary>
    /// Validates a nonce request.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="clientValidationResult">The client validation result.</param>
    /// <returns></returns>
    Task<NonceRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult);
}
