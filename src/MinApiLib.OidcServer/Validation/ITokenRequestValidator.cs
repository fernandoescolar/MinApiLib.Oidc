namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Interface for the token request validator
/// </summary>
public interface ITokenRequestValidator
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="clientValidationResult">The client validation result.</param>
    /// <returns></returns>
    Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult);

    /// <summary>
    /// Validates the request together with a validated DPoP proof (RFC 9449). The default implementation ignores the proof.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="clientValidationResult">The client validation result.</param>
    /// <param name="dpopProof">The validated DPoP proof, or <c>null</c> when the request carried none.</param>
    Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult, DPoPProofValidationResult? dpopProof)
        => ValidateRequestAsync(parameters, clientValidationResult);
}
