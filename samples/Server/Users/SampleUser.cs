namespace Samples.Server.Users;

/// <summary>
/// A user of the sample, for ASP.NET Core Identity's <see cref="UserManager{TUser}"/>.
/// </summary>
public sealed class SampleUser
{
    public required string Id { get; init; }

    public required string UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public string? PasswordHash { get; set; }

    /// <summary>The user's passkeys. Lock the list to read or change it.</summary>
    public List<UserPasskeyInfo> Passkeys { get; } = [];
}
