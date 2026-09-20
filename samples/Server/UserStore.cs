using System.Security.Cryptography;
using System.Text;

namespace Samples.Server;

public sealed record SampleUser(string SubjectId, string Username, string Password, string Name, string Email);

/// <summary>
/// Two demo users. A real server checks a user database or ASP.NET Core Identity instead.
/// </summary>
public sealed class UserStore
{
    private readonly SampleUser[] _users =
    [
        new("1", "alice", "alice", "Alice Smith", "alice@example.com"),
        new("2", "bob", "bob", "Bob Jones", "bob@example.com")
    ];

    public SampleUser? Validate(string? username, string? password)
    {
        var user = _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        if (user == null || password == null)
        {
            return null;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(user.Password), Encoding.UTF8.GetBytes(password))
            ? user
            : null;
    }
}
