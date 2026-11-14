// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

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
