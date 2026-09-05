namespace MinApiLib.OidcServer.ResponseHandling;

/// <summary>
/// Interface for the nonce response generator
/// </summary>
public interface INonceResponseGenerator
{
    /// <summary>
    /// Creates the response.
    /// </summary>
    /// <param name="validationResult">The nonce request validation result.</param>
    /// <returns></returns>
    Task<NonceResponse> ProcessAsync(NonceRequestValidationResult validationResult);
}
