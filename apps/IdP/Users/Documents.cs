namespace IdP.Users;

/// <summary>A user of the identity provider, as stored in MongoDB. ASP.NET Core Identity manages it through <see cref="MongoUserStore"/>.</summary>
public sealed class IdPUser
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string UserName { get; set; } = default!;

    public string NormalizedUserName { get; set; } = default!;

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>Free profile attributes, issued as claims when a resource asks for them (for example <c>picture</c>, <c>locale</c>, <c>department</c>).</summary>
    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);

    public string? PasswordHash { get; set; }

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Whether the user can sign in. A disabled user keeps the data but gets no session or token.</summary>
    public bool Enabled { get; set; } = true;

    public bool LockoutEnabled { get; set; } = true;

    public DateTimeOffset? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }

    public bool TwoFactorEnabled { get; set; }

    /// <summary>The TOTP secret, protected with data protection.</summary>
    public string? AuthenticatorKey { get; set; }

    public List<string> RecoveryCodes { get; set; } = [];

    public List<PasskeyDocument> Passkeys { get; set; } = [];

    public List<ExternalLoginDocument> Logins { get; set; } = [];

    /// <summary>Role names assigned directly. The groups add theirs.</summary>
    public List<string> Roles { get; set; } = [];

    public List<string> Groups { get; set; } = [];

    public List<ClaimDocument> Claims { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSignInAt { get; set; }

    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) is { Length: > 0 } name ? name : UserName;
}

public sealed class PasskeyDocument
{
    public byte[] CredentialId { get; set; } = default!;

    public byte[] PublicKey { get; set; } = default!;

    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public long SignCount { get; set; }

    public string[]? Transports { get; set; }

    public bool IsUserVerified { get; set; }

    public bool IsBackupEligible { get; set; }

    public bool IsBackedUp { get; set; }

    public byte[] AttestationObject { get; set; } = [];

    public byte[] ClientDataJson { get; set; } = [];

    public static PasskeyDocument From(UserPasskeyInfo passkey) => new()
    {
        CredentialId = passkey.CredentialId,
        PublicKey = passkey.PublicKey,
        Name = passkey.Name,
        CreatedAt = passkey.CreatedAt,
        SignCount = passkey.SignCount,
        Transports = passkey.Transports,
        IsUserVerified = passkey.IsUserVerified,
        IsBackupEligible = passkey.IsBackupEligible,
        IsBackedUp = passkey.IsBackedUp,
        AttestationObject = passkey.AttestationObject,
        ClientDataJson = passkey.ClientDataJson
    };

    public UserPasskeyInfo ToInfo() => new(CredentialId, PublicKey, CreatedAt, (uint)SignCount, Transports, IsUserVerified, IsBackupEligible, IsBackedUp, AttestationObject, ClientDataJson)
    {
        Name = Name
    };
}

public sealed class ExternalLoginDocument
{
    public string LoginProvider { get; set; } = default!;

    public string ProviderKey { get; set; } = default!;

    public string? ProviderDisplayName { get; set; }

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ClaimDocument
{
    public string Type { get; set; } = default!;

    public string Value { get; set; } = default!;
}

/// <summary>A role. Users and groups refer to it by name.</summary>
public sealed class IdPRole
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = default!;

    public string NormalizedName { get; set; } = default!;

    public string? Description { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Built-in roles cannot be deleted or renamed.</summary>
    public bool BuiltIn { get; set; }
}

/// <summary>A group of users: its roles are added to those of its members, and its name is issued as the <c>groups</c> claim.</summary>
public sealed class IdPGroup
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public List<string> Roles { get; set; } = [];

    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
