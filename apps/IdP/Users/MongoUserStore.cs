using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace IdP.Users;

/// <summary>
/// The ASP.NET Core Identity store over the <c>users</c> collection: passwords, e-mail, lockout, TOTP, recovery codes,
/// passkeys, external logins, roles and claims. The user document is written back as a whole by
/// <see cref="UpdateAsync"/>, with an optimistic concurrency check on <see cref="IdPUser.ConcurrencyStamp"/>.
/// </summary>
public sealed class MongoUserStore(IdPMongoContext context, IDataProtectionProvider dataProtection) :
    IUserPasswordStore<IdPUser>,
    IUserEmailStore<IdPUser>,
    IUserPhoneNumberStore<IdPUser>,
    IUserSecurityStampStore<IdPUser>,
    IUserLockoutStore<IdPUser>,
    IUserTwoFactorStore<IdPUser>,
    IUserAuthenticatorKeyStore<IdPUser>,
    IUserTwoFactorRecoveryCodeStore<IdPUser>,
    IUserPasskeyStore<IdPUser>,
    IUserLoginStore<IdPUser>,
    IUserRoleStore<IdPUser>,
    IUserClaimStore<IdPUser>
{
    private readonly IDataProtector _authenticatorKeys = dataProtection.CreateProtector("IdP.Users.AuthenticatorKey");

    // IUserStore

    public Task<string> GetUserIdAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.UserName);

    public Task SetUserNameAsync(IdPUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(IdPUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName ?? throw new ArgumentNullException(nameof(normalizedName));
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(IdPUser user, CancellationToken cancellationToken)
    {
        try
        {
            user.CreatedAt = user.UpdatedAt = DateTime.UtcNow;
            await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
            return IdentityResult.Success;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "A user with that name or e-mail already exists." });
        }
    }

    public async Task<IdentityResult> UpdateAsync(IdPUser user, CancellationToken cancellationToken)
    {
        var expectedStamp = user.ConcurrencyStamp;
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        try
        {
            var result = await context.Users.ReplaceOneAsync(u => u.Id == user.Id && u.ConcurrencyStamp == expectedStamp, user, cancellationToken: cancellationToken);
            if (result.MatchedCount == 0)
            {
                user.ConcurrencyStamp = expectedStamp;
                return IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "The user was changed by somebody else. Reload and try again." });
            }

            return IdentityResult.Success;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            user.ConcurrencyStamp = expectedStamp;
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "A user with that name or e-mail already exists." });
        }
    }

    public async Task<IdentityResult> DeleteAsync(IdPUser user, CancellationToken cancellationToken)
    {
        await context.Users.DeleteOneAsync(u => u.Id == user.Id, cancellationToken);
        return IdentityResult.Success;
    }

    public Task<IdPUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken)!;

    public Task<IdPUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => context.Users.Find(u => u.NormalizedUserName == normalizedUserName).FirstOrDefaultAsync(cancellationToken)!;

    // IUserPasswordStore

    public Task SetPasswordHashAsync(IdPUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash != null);

    // IUserEmailStore

    public Task SetEmailAsync(IdPUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(IdPUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<IdPUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => context.Users.Find(u => u.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync(cancellationToken)!;

    public Task<string?> GetNormalizedEmailAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(IdPUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // IUserPhoneNumberStore

    public Task SetPhoneNumberAsync(IdPUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumber);

    public Task<bool> GetPhoneNumberConfirmedAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumberConfirmed);

    public Task SetPhoneNumberConfirmedAsync(IdPUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    // IUserSecurityStampStore

    public Task SetSecurityStampAsync(IdPUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.SecurityStamp);

    // IUserLockoutStore

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(IdPUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(++user.AccessFailedCount);

    public Task ResetAccessFailedCountAsync(IdPUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(IdPUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    // IUserTwoFactorStore, IUserAuthenticatorKeyStore, IUserTwoFactorRecoveryCodeStore

    public Task SetTwoFactorEnabledAsync(IdPUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.TwoFactorEnabled);

    public Task SetAuthenticatorKeyAsync(IdPUser user, string key, CancellationToken cancellationToken)
    {
        user.AuthenticatorKey = _authenticatorKeys.Protect(key);
        return Task.CompletedTask;
    }

    public Task<string?> GetAuthenticatorKeyAsync(IdPUser user, CancellationToken cancellationToken)
    {
        if (user.AuthenticatorKey == null)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            return Task.FromResult<string?>(_authenticatorKeys.Unprotect(user.AuthenticatorKey));
        }
        catch (CryptographicException)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task ReplaceCodesAsync(IdPUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        user.RecoveryCodes = [.. recoveryCodes.Select(HashRecoveryCode)];
        return Task.CompletedTask;
    }

    public Task<bool> RedeemCodeAsync(IdPUser user, string code, CancellationToken cancellationToken)
    {
        var hash = HashRecoveryCode(code);
        return Task.FromResult(user.RecoveryCodes.Remove(hash));
    }

    public Task<int> CountCodesAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult(user.RecoveryCodes.Count);

    private static string HashRecoveryCode(string code)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code.Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant())));

    // IUserPasskeyStore

    public Task AddOrUpdatePasskeyAsync(IdPUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
    {
        var index = user.Passkeys.FindIndex(p => p.CredentialId.AsSpan().SequenceEqual(passkey.CredentialId));
        var document = PasskeyDocument.From(passkey);
        if (index < 0)
        {
            user.Passkeys.Add(document);
        }
        else
        {
            document.Name ??= user.Passkeys[index].Name;
            user.Passkeys[index] = document;
        }

        return Task.CompletedTask;
    }

    public Task<IList<UserPasskeyInfo>> GetPasskeysAsync(IdPUser user, CancellationToken cancellationToken)
        => Task.FromResult<IList<UserPasskeyInfo>>([.. user.Passkeys.Select(p => p.ToInfo())]);

    public Task<IdPUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
        => context.Users.Find(Builders<IdPUser>.Filter.ElemMatch(u => u.Passkeys, p => p.CredentialId == credentialId)).FirstOrDefaultAsync(cancellationToken)!;

    public Task<UserPasskeyInfo?> FindPasskeyAsync(IdPUser user, byte[] credentialId, CancellationToken cancellationToken)
        => Task.FromResult(user.Passkeys.FirstOrDefault(p => p.CredentialId.AsSpan().SequenceEqual(credentialId))?.ToInfo());

    public Task RemovePasskeyAsync(IdPUser user, byte[] credentialId, CancellationToken cancellationToken)
    {
        user.Passkeys.RemoveAll(p => p.CredentialId.AsSpan().SequenceEqual(credentialId));
        return Task.CompletedTask;
    }

    // IUserLoginStore

    public Task AddLoginAsync(IdPUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        user.Logins.RemoveAll(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey);
        user.Logins.Add(new ExternalLoginDocument { LoginProvider = login.LoginProvider, ProviderKey = login.ProviderKey, ProviderDisplayName = login.ProviderDisplayName });
        return Task.CompletedTask;
    }

    public Task RemoveLoginAsync(IdPUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        user.Logins.RemoveAll(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        return Task.CompletedTask;
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(IdPUser user, CancellationToken cancellationToken)
        => Task.FromResult<IList<UserLoginInfo>>([.. user.Logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))]);

    public Task<IdPUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        => context.Users.Find(Builders<IdPUser>.Filter.ElemMatch(u => u.Logins, l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey))
            .FirstOrDefaultAsync(cancellationToken)!;

    // IUserRoleStore: the user document keeps the role names; the normalized name is resolved through the roles collection.

    public async Task AddToRoleAsync(IdPUser user, string roleName, CancellationToken cancellationToken)
    {
        var role = await context.Roles.Find(r => r.NormalizedName == roleName).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"The role {roleName} does not exist.");
        if (!user.Roles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            user.Roles.Add(role.Name);
        }
    }

    public Task RemoveFromRoleAsync(IdPUser user, string roleName, CancellationToken cancellationToken)
    {
        user.Roles.RemoveAll(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(IdPUser user, CancellationToken cancellationToken) => Task.FromResult<IList<string>>([.. user.Roles]);

    public Task<bool> IsInRoleAsync(IdPUser user, string roleName, CancellationToken cancellationToken)
        => Task.FromResult(user.Roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase)));

    public async Task<IList<IdPUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var role = await context.Roles.Find(r => r.NormalizedName == roleName).FirstOrDefaultAsync(cancellationToken);
        return role == null ? [] : await context.Users.Find(u => u.Roles.Contains(role.Name)).ToListAsync(cancellationToken);
    }

    // IUserClaimStore

    public Task<IList<Claim>> GetClaimsAsync(IdPUser user, CancellationToken cancellationToken)
        => Task.FromResult<IList<Claim>>([.. user.Claims.Select(c => new Claim(c.Type, c.Value))]);

    public Task AddClaimsAsync(IdPUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        user.Claims.AddRange(claims.Select(c => new ClaimDocument { Type = c.Type, Value = c.Value }));
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(IdPUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        foreach (var existing in user.Claims.Where(c => c.Type == claim.Type && c.Value == claim.Value))
        {
            existing.Type = newClaim.Type;
            existing.Value = newClaim.Value;
        }

        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(IdPUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            user.Claims.RemoveAll(c => c.Type == claim.Type && c.Value == claim.Value);
        }

        return Task.CompletedTask;
    }

    public Task<IList<IdPUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        => context.Users.Find(Builders<IdPUser>.Filter.ElemMatch(u => u.Claims, c => c.Type == claim.Type && c.Value == claim.Value))
            .ToListAsync(cancellationToken).ContinueWith(t => (IList<IdPUser>)t.Result, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

    public void Dispose()
    {
    }
}
