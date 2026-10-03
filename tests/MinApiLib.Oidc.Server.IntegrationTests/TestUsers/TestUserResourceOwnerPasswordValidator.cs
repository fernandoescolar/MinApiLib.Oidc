namespace MinApiLib.Oidc.Server.Test;

/// <summary>
/// Resource owner password validator for test users (deprecated password grant).
/// </summary>
public class TestUserResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly TestUserStore _users;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
    /// </summary>
    /// <param name="users">The users.</param>
    /// <param name="clock">The clock.</param>
    public TestUserResourceOwnerPasswordValidator(TestUserStore users, TimeProvider clock)
    {
        _users = users;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        if (_users.ValidateCredentials(context.UserName, context.Password))
        {
            var user = _users.FindByUsername(context.UserName);
            context.Result = new GrantValidationResult(
                user.SubjectId ?? throw new ArgumentException("Subject ID not set", nameof(context)),
                OidcConstants.AuthenticationMethods.Password,
                _clock.GetUtcNow().UtcDateTime,
                user.Claims);
        }

        return Task.CompletedTask;
    }
}
