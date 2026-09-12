namespace MinApiLib.OidcServer.Features.Tokens.Grants;

/// <summary>
/// Validates the grant-specific part of a token request.
/// </summary>
internal interface ITokenRequestGrantValidator
{
    Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context);
}
