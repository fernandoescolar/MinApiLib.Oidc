namespace IdentityServer.AspNetIdentity;

/// <summary>
/// Validates the deprecated password grant against ASP.NET Core Identity. It only runs for clients with
/// <see cref="Client.AllowResourceOwnerPasswordFlow"/>.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public class ResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidator
    where TUser : class
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly ILogger<ResourceOwnerPasswordValidator<TUser>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceOwnerPasswordValidator{TUser}"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="signInManager">The sign in manager.</param>
    /// <param name="logger">The logger.</param>
    public ResourceOwnerPasswordValidator(
        UserManager<TUser> userManager,
        SignInManager<TUser> signInManager,
        ILogger<ResourceOwnerPasswordValidator<TUser>> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Validates the resource owner password credential. Failed attempts count towards lockout.
    /// </summary>
    /// <param name="context">The context.</param>
    public virtual async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = await _userManager.FindByNameAsync(context.UserName);
        if (user != null)
        {
            var result = await _signInManager.CheckPasswordSignInAsync(user, context.Password, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var sub = await _userManager.GetUserIdAsync(user);

                _logger.CredentialsValidated(context.UserName);

                context.Result = new GrantValidationResult(sub, OidcConstants.AuthenticationMethods.Password);
                return;
            }

            var reason = result.IsLockedOut ? "locked out" : result.IsNotAllowed ? "not allowed" : "invalid credentials";
            _logger.AuthenticationFailed(context.UserName, reason);
        }
        else
        {
            _logger.NoUserFoundForUserName(context.UserName);
        }

        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
    }
}
