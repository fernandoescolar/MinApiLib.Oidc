namespace Samples.Server.Users;

/// <summary>
/// The ASP.NET Core Identity store over <see cref="SampleUserDirectory"/>: users, password hashes and passkeys.
/// </summary>
public sealed class SampleUserStore(SampleUserDirectory directory) : IUserPasswordStore<SampleUser>, IUserPasskeyStore<SampleUser>
{
    public Task<string> GetUserIdAsync(SampleUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(SampleUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.UserName);

    public Task SetUserNameAsync(SampleUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName ?? user.UserName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(SampleUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(SampleUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(SampleUser user, CancellationToken cancellationToken)
        => Task.FromResult(directory.Users.TryAdd(user.Id, user) ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = "The user exists." }));

    public Task<IdentityResult> UpdateAsync(SampleUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(SampleUser user, CancellationToken cancellationToken)
    {
        directory.Users.TryRemove(user.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<SampleUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(directory.Users.GetValueOrDefault(userId));

    public Task<SampleUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Task.FromResult(directory.Users.Values.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName));

    public Task SetPasswordHashAsync(SampleUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(SampleUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(SampleUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash != null);

    public Task AddOrUpdatePasskeyAsync(SampleUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
    {
        lock (user.Passkeys)
        {
            var index = user.Passkeys.FindIndex(p => p.CredentialId.AsSpan().SequenceEqual(passkey.CredentialId));
            if (index < 0)
            {
                user.Passkeys.Add(passkey);
            }
            else
            {
                passkey.Name ??= user.Passkeys[index].Name;
                user.Passkeys[index] = passkey;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IList<UserPasskeyInfo>> GetPasskeysAsync(SampleUser user, CancellationToken cancellationToken)
    {
        lock (user.Passkeys)
        {
            return Task.FromResult<IList<UserPasskeyInfo>>([.. user.Passkeys]);
        }
    }

    public Task<SampleUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
        => Task.FromResult(directory.Users.Values.FirstOrDefault(user => Find(user, credentialId) != null));

    public Task<UserPasskeyInfo?> FindPasskeyAsync(SampleUser user, byte[] credentialId, CancellationToken cancellationToken)
        => Task.FromResult(Find(user, credentialId));

    public Task RemovePasskeyAsync(SampleUser user, byte[] credentialId, CancellationToken cancellationToken)
    {
        lock (user.Passkeys)
        {
            user.Passkeys.RemoveAll(p => p.CredentialId.AsSpan().SequenceEqual(credentialId));
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    private static UserPasskeyInfo? Find(SampleUser user, byte[] credentialId)
    {
        lock (user.Passkeys)
        {
            return user.Passkeys.FirstOrDefault(p => p.CredentialId.AsSpan().SequenceEqual(credentialId));
        }
    }
}
