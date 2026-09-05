namespace IdentityServer.Validation;

/// <summary>
/// Validates the user name and password of a resource owner password credentials request (<c>grant_type=password</c>).
/// </summary>
/// <remarks>
/// The password grant is deprecated by OAuth 2.1 and RFC 9700. It only runs for clients that list
/// <see cref="GrantType.ResourceOwnerPassword"/> and set <see cref="Client.AllowResourceOwnerPasswordFlow"/>.
/// Without a registered validator the grant answers <c>unsupported_grant_type</c>.
/// </remarks>
public interface IResourceOwnerPasswordValidator
{
    /// <summary>
    /// Validates the resource owner password credential and sets <see cref="ResourceOwnerPasswordValidationContext.Result"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    Task ValidateAsync(ResourceOwnerPasswordValidationContext context);
}
