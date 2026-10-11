using System.Collections.Concurrent;

namespace Samples.Server.Users;

/// <summary>
/// The sample's users, in memory: two demo users with hashed passwords, and the passkeys they register. A real
/// server keeps them in a database, with the Entity Framework Core stores of ASP.NET Core Identity or its own.
/// </summary>
public sealed class SampleUserDirectory
{
    public SampleUserDirectory()
    {
        // The hasher UserManager uses by default (PBKDF2), so CheckPasswordAsync accepts these hashes.
        var passwordHasher = new PasswordHasher<SampleUser>();
        foreach (var (id, userName, name) in new[] { ("1", "alice", "Alice Smith"), ("2", "bob", "Bob Jones") })
        {
            var user = new SampleUser
            {
                Id = id,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Name = name,
                Email = $"{userName}@example.com"
            };
            user.PasswordHash = passwordHasher.HashPassword(user, userName);
            Users[id] = user;
        }
    }

    public ConcurrentDictionary<string, SampleUser> Users { get; } = new(StringComparer.Ordinal);
}
