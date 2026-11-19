using System.Text.RegularExpressions;

namespace IdP.Features.Api.Admin;

// The users (/api/admin/users): accounts, credentials, roles, groups, sessions and activity.

public sealed record UserSummary(string Id, string UserName, string? Email, bool EmailConfirmed, string DisplayName, bool Enabled, bool LockedOut, bool TwoFactorEnabled,
    IReadOnlyList<string> Roles, DateTime CreatedAt, DateTime? LastSignInAt);

public sealed record UserDetail(string Id, string UserName, string? Email, bool EmailConfirmed, string? FirstName, string? LastName, string? PhoneNumber, bool PhoneNumberConfirmed,
    bool Enabled, DateTimeOffset? LockoutEnd, int AccessFailedCount, bool TwoFactorEnabled, int RecoveryCodesLeft, bool HasPassword,
    IReadOnlyList<string> Roles, IReadOnlyList<string> EffectiveRoles, IReadOnlyList<GroupRef> Groups, IReadOnlyList<ClaimDocument> Claims, IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<Account.PasskeyModel> Passkeys, IReadOnlyList<Account.LinkedAccountModel> Logins, DateTime CreatedAt, DateTime UpdatedAt, DateTime? LastSignInAt);

public sealed record GroupRef(string Id, string Name);

public sealed record UserInput(string? UserName, string? Email, bool? EmailConfirmed, string? FirstName, string? LastName, string? PhoneNumber, bool? Enabled,
    List<string>? Roles, List<string>? Groups, List<ClaimDocument>? Claims, Dictionary<string, string>? Attributes, string? Password, bool? SendInvitation);

public static class UserModels
{
    public static UserSummary Summary(IdPUser u)
        => new(u.Id, u.UserName, u.Email, u.EmailConfirmed, u.DisplayName, u.Enabled, u.LockoutEnd > DateTimeOffset.UtcNow, u.TwoFactorEnabled, u.Roles, u.CreatedAt, u.LastSignInAt);

    public static async Task<UserDetail> DetailAsync(IdPUser u, IdPMongoContext db, CancellationToken cancellationToken)
    {
        var groups = u.Groups.Count == 0 ? [] : await db.Groups.Find(g => u.Groups.Contains(g.Id)).ToListAsync(cancellationToken);
        return new UserDetail(u.Id, u.UserName, u.Email, u.EmailConfirmed, u.FirstName, u.LastName, u.PhoneNumber, u.PhoneNumberConfirmed,
            u.Enabled, u.LockoutEnd, u.AccessFailedCount, u.TwoFactorEnabled, u.RecoveryCodes.Count, u.PasswordHash != null,
            u.Roles, u.Roles.Concat(groups.SelectMany(g => g.Roles)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            groups.Select(g => new GroupRef(g.Id, g.Name)).ToList(), u.Claims, u.Attributes,
            u.Passkeys.Select(Account.AccountModels.Passkey).ToList(),
            u.Logins.Select(l => new Account.LinkedAccountModel(l.LoginProvider, l.ProviderDisplayName, l.ProviderKey, l.LinkedAt)).ToList(),
            u.CreatedAt, u.UpdatedAt, u.LastSignInAt);
    }

    /// <summary>Checks that every role exists and every group exists; returns the error, if any.</summary>
    public static async Task<string?> CheckRolesAndGroupsAsync(IdPMongoContext db, List<string>? roles, List<string>? groups, CancellationToken cancellationToken)
    {
        if (roles is { Count: > 0 })
        {
            var known = await db.Roles.Find(FilterDefinition<IdPRole>.Empty).Project(r => r.Name).ToListAsync(cancellationToken);
            var unknown = roles.Except(known, StringComparer.OrdinalIgnoreCase).ToList();
            if (unknown.Count > 0)
            {
                return $"Unknown roles: {string.Join(", ", unknown)}.";
            }
        }

        if (groups is { Count: > 0 })
        {
            var known = await db.Groups.Find(g => groups.Contains(g.Id)).Project(g => g.Id).ToListAsync(cancellationToken);
            var unknown = groups.Except(known, StringComparer.Ordinal).ToList();
            if (unknown.Count > 0)
            {
                return $"Unknown groups: {string.Join(", ", unknown)}.";
            }
        }

        return null;
    }
}

public sealed record ListUsers() : GetHandlerAsync<ListUsers.Request>("/api/admin/users")
{
    public readonly record struct Request([FromQuery] string? Q, [FromQuery] string? Role, [FromQuery] string? Group, [FromQuery] bool? Enabled, [FromQuery] int? Page, [FromQuery] int? PageSize, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filters = new List<FilterDefinition<IdPUser>>();
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var regex = new BsonRegularExpression(Regex.Escape(request.Q.Trim()), "i");
            filters.Add(Builders<IdPUser>.Filter.Or(
                Builders<IdPUser>.Filter.Regex(u => u.UserName, regex),
                Builders<IdPUser>.Filter.Regex(u => u.Email, regex),
                Builders<IdPUser>.Filter.Regex(u => u.FirstName, regex),
                Builders<IdPUser>.Filter.Regex(u => u.LastName, regex)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            filters.Add(Builders<IdPUser>.Filter.AnyEq(u => u.Roles, request.Role));
        }

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            filters.Add(Builders<IdPUser>.Filter.AnyEq(u => u.Groups, request.Group));
        }

        if (request.Enabled is { } enabled)
        {
            filters.Add(Builders<IdPUser>.Filter.Eq(u => u.Enabled, enabled));
        }

        var filter = filters.Count == 0 ? FilterDefinition<IdPUser>.Empty : Builders<IdPUser>.Filter.And(filters);
        var total = await request.Db.Users.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var users = await request.Db.Users.Find(filter).SortBy(u => u.NormalizedUserName).Skip(Paging.Skip(request.Page, request.PageSize)).Limit(Paging.Size(request.PageSize)).ToListAsync(cancellationToken);
        return Results.Ok(new Page<UserSummary>(users.Select(UserModels.Summary).ToList(), total, Paging.Number(request.Page), Paging.Size(request.PageSize)));
    }
}

public sealed record GetUser() : GetHandlerAsync<GetUser.Request>("/api/admin/users/{id}")
{
    public readonly record struct Request([FromRoute] string Id, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Db.Users.Find(u => u.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        return user == null ? Results.NotFound() : Results.Ok(await UserModels.DetailAsync(user, request.Db, cancellationToken));
    }
}

public sealed record CreateUser() : PostHandlerAsync<CreateUser.Request>("/api/admin/users")
{
    public readonly record struct Request(HttpContext HttpContext, [FromBody] UserInput Body, [FromServices] UserManager<IdPUser> Users, [FromServices] IdPMongoContext Db,
        [FromServices] AccountEmails Emails, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.UserName))
        {
            return ApiErrors.Validation("userName is required.", "userName");
        }

        var error = await UserModels.CheckRolesAndGroupsAsync(request.Db, body.Roles, body.Groups, cancellationToken);
        if (error != null)
        {
            return ApiErrors.Validation(error);
        }

        var user = new IdPUser
        {
            UserName = body.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim(),
            EmailConfirmed = body.EmailConfirmed ?? false,
            FirstName = body.FirstName?.Trim(),
            LastName = body.LastName?.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(body.PhoneNumber) ? null : body.PhoneNumber.Trim(),
            Enabled = body.Enabled ?? true,
            Roles = body.Roles ?? [],
            Groups = body.Groups ?? [],
            Claims = body.Claims ?? [],
            Attributes = body.Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };

        var result = string.IsNullOrEmpty(body.Password) ? await request.Users.CreateAsync(user) : await request.Users.CreateAsync(user, body.Password);
        if (!result.Succeeded)
        {
            return ApiErrors.Identity(result);
        }

        if (body.SendInvitation == true && user.Email != null)
        {
            await request.Emails.SendPasswordResetAsync(request.HttpContext, user, invitation: true);
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "User created", "By an administrator", user.Id, user.UserName);
        return Results.Json(await UserModels.DetailAsync(user, request.Db, cancellationToken), Json.Options, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record UpdateUser() : PutHandlerAsync<UpdateUser.Request>("/api/admin/users/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromBody] UserInput Body, [FromServices] UserManager<IdPUser> Users, [FromServices] IdPMongoContext Db, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Users.FindByIdAsync(request.Id);
        if (user == null)
        {
            return Results.NotFound();
        }

        var body = request.Body;
        var error = await UserModels.CheckRolesAndGroupsAsync(request.Db, body.Roles, body.Groups, cancellationToken);
        if (error != null)
        {
            return ApiErrors.Validation(error);
        }

        if (body.Roles != null && user.Id == request.HttpContext.User.GetSubjectId() && user.Roles.Contains(ApiAuth.AdminRole, StringComparer.OrdinalIgnoreCase) && !body.Roles.Contains(ApiAuth.AdminRole, StringComparer.OrdinalIgnoreCase))
        {
            return ApiErrors.Validation("You cannot remove your own admin role.", "roles");
        }

        if (!string.IsNullOrWhiteSpace(body.UserName) && body.UserName.Trim() != user.UserName)
        {
            var rename = await request.Users.SetUserNameAsync(user, body.UserName.Trim());
            if (!rename.Succeeded)
            {
                return ApiErrors.Identity(rename);
            }
        }

        if (body.Email != null)
        {
            var email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim();
            if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var change = await request.Users.SetEmailAsync(user, email);
                if (!change.Succeeded)
                {
                    return ApiErrors.Identity(change);
                }

                user.EmailConfirmed = false;
            }
        }

        if (body.EmailConfirmed is { } confirmed)
        {
            user.EmailConfirmed = confirmed;
        }

        if (body.FirstName != null)
        {
            user.FirstName = body.FirstName.Trim();
        }

        if (body.LastName != null)
        {
            user.LastName = body.LastName.Trim();
        }

        if (body.PhoneNumber != null)
        {
            user.PhoneNumber = string.IsNullOrWhiteSpace(body.PhoneNumber) ? null : body.PhoneNumber.Trim();
        }

        if (body.Enabled is { } enabled)
        {
            user.Enabled = enabled;
        }

        if (body.Roles != null)
        {
            user.Roles = body.Roles;
        }

        if (body.Groups != null)
        {
            user.Groups = body.Groups;
        }

        if (body.Claims != null)
        {
            user.Claims = body.Claims;
        }

        if (body.Attributes != null)
        {
            user.Attributes = body.Attributes;
        }

        var result = await request.Users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ApiErrors.Identity(result);
        }

        if (body.Enabled == false)
        {
            // Sessions end at once; tokens stop at the next refresh (IsActive).
            await request.Users.UpdateSecurityStampAsync(user);
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "User updated", "By an administrator", user.Id, user.UserName);
        return Results.Ok(await UserModels.DetailAsync(user, request.Db, cancellationToken));
    }
}

public sealed record DeleteUser() : DeleteHandlerAsync<DeleteUser.Request>("/api/admin/users/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromServices] UserManager<IdPUser> Users, [FromServices] IPersistedGrantStore Grants, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Users.FindByIdAsync(request.Id);
        if (user == null)
        {
            return Results.NotFound();
        }

        if (user.Id == request.HttpContext.User.GetSubjectId())
        {
            return ApiErrors.Validation("You cannot delete your own account from here.");
        }

        await request.Grants.RemoveAllAsync(new PersistedGrantFilter { SubjectId = user.Id });
        await request.Users.DeleteAsync(user);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "User deleted", "By an administrator", user.Id, user.UserName);
        return Results.NoContent();
    }
}

public sealed record SetUserPassword() : PutHandlerAsync<SetUserPassword.Request>("/api/admin/users/{id}/password")
{
    public sealed record Body(string Password);

    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromBody] Body Body, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Users.FindByIdAsync(request.Id);
        if (user == null)
        {
            return Results.NotFound();
        }

        if (user.PasswordHash != null)
        {
            var removed = await request.Users.RemovePasswordAsync(user);
            if (!removed.Succeeded)
            {
                return ApiErrors.Identity(removed);
            }
        }

        var result = await request.Users.AddPasswordAsync(user, request.Body.Password);
        if (!result.Succeeded)
        {
            return ApiErrors.Identity(result);
        }

        await request.Users.UpdateSecurityStampAsync(user);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Password set", "By an administrator", user.Id, user.UserName);
        return Results.NoContent();
    }
}

/// <summary>Actions on a user: send a reset link, confirm the e-mail, unlock, reset the authenticator, end the sessions.</summary>
public sealed record UserAction() : PostHandlerAsync<UserAction.Request>("/api/admin/users/{id}/actions/{action}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromRoute] string Action, [FromServices] UserManager<IdPUser> Users,
        [FromServices] IPersistedGrantStore Grants, [FromServices] AccountEmails Emails, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Users.FindByIdAsync(request.Id);
        if (user == null)
        {
            return Results.NotFound();
        }

        switch (request.Action)
        {
            case "send-password-reset":
                if (user.Email == null)
                {
                    return ApiErrors.Validation("The user has no e-mail.");
                }

                await request.Emails.SendPasswordResetAsync(request.HttpContext, user);
                break;
            case "send-verification":
                if (user.Email == null)
                {
                    return ApiErrors.Validation("The user has no e-mail.");
                }

                await request.Emails.SendVerificationAsync(request.HttpContext, user);
                break;
            case "confirm-email":
                user.EmailConfirmed = true;
                await request.Users.UpdateAsync(user);
                break;
            case "unlock":
                await request.Users.SetLockoutEndDateAsync(user, null);
                await request.Users.ResetAccessFailedCountAsync(user);
                break;
            case "reset-authenticator":
                await request.Users.SetTwoFactorEnabledAsync(user, false);
                user.AuthenticatorKey = null;
                user.RecoveryCodes.Clear();
                await request.Users.UpdateAsync(user);
                break;
            case "sign-out":
                await Account.Sessions.EndAllAsync(user, request.Users, request.Grants);
                break;
            default:
                return Results.NotFound();
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, $"User action: {request.Action}", "By an administrator", user.Id, user.UserName);
        return Results.NoContent();
    }
}

public sealed record RemoveUserPasskey() : DeleteHandlerAsync<RemoveUserPasskey.Request>("/api/admin/users/{id}/passkeys/{credentialId}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromRoute] string CredentialId, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Users.FindByIdAsync(request.Id);
        if (user == null)
        {
            return Results.NotFound();
        }

        await request.Users.RemovePasskeyAsync(user, Base64Url.DecodeFromChars(request.CredentialId));
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Passkey removed", "By an administrator", user.Id, user.UserName);
        return Results.NoContent();
    }
}

public sealed record RemoveUserLogin() : DeleteHandlerAsync<RemoveUserLogin.Request>("/api/admin/users/{id}/logins/{provider}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromRoute] string Provider, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await request.Users.FindByIdAsync(request.Id);
        if (user == null)
        {
            return Results.NotFound();
        }

        foreach (var login in user.Logins.Where(l => l.LoginProvider == request.Provider).ToList())
        {
            await request.Users.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }

        await Audit.RaiseAsync(request.Events, request.HttpContext, "External account unlinked", $"By an administrator: {request.Provider}", user.Id, user.UserName);
        return Results.NoContent();
    }
}

public sealed record GetUserEvents() : GetHandlerAsync<GetUserEvents.Request>("/api/admin/users/{id}/events")
{
    public readonly record struct Request([FromRoute] string Id, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var events = await request.Db.Events.Find(e => e.SubjectId == request.Id).SortByDescending(e => e.TimeStamp).Limit(100).ToListAsync(cancellationToken);
        return Results.Ok(events.Select(EventsApi.Model));
    }
}
