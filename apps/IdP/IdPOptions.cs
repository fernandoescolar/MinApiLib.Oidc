namespace IdP;

/// <summary>
/// The configuration of the identity provider, bound from the <c>IdP</c> section (<c>IdP__Name</c>, ... as environment
/// variables).
/// </summary>
public sealed class IdPOptions
{
    public const string Section = "IdP";

    /// <summary>The name shown on the pages and in the e-mails.</summary>
    public string Name { get; set; } = "MinApiLib IdP";

    /// <summary>
    /// The issuer (<c>iss</c>) and the base of the protocol endpoints. Leave it empty to use the URL the request came
    /// in on, as seen through the forwarded headers.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Whether to honour <c>X-Forwarded-For</c>, <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c> from a reverse
    /// proxy. On by default: the container runs behind one.
    /// </summary>
    public bool ForwardedHeaders { get; set; } = true;

    /// <summary>The MongoDB database. The connection string is <c>ConnectionStrings:Mongo</c>.</summary>
    public string Database { get; set; } = "idp";

    /// <summary>
    /// The <c>SameSite</c> mode of the session cookies: <c>Lax</c> (the default) works everywhere, over HTTP too;
    /// <c>None</c> also lets the check-session iframe see the session from other sites, and needs HTTPS.
    /// </summary>
    public Microsoft.AspNetCore.Http.SameSiteMode CookieSameSite { get; set; } = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

    public RegistrationOptions Registration { get; set; } = new();

    public AdminOptions Admin { get; set; } = new();

    public EmailOptions Email { get; set; } = new();

    public EventsRetentionOptions Events { get; set; } = new();

    public PasswordOptions Password { get; set; } = new();

    public SeedOptions Seed { get; set; } = new();

    public sealed class RegistrationOptions
    {
        /// <summary>Whether users can create their own account on the login page.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Whether a new account must verify its e-mail before it can sign in.</summary>
        public bool RequireEmailVerification { get; set; }
    }

    public sealed class AdminOptions
    {
        /// <summary>
        /// The first administrator, created when no user has the <c>admin</c> role yet. Without a password, one is
        /// generated and written to the log once.
        /// </summary>
        public string UserName { get; set; } = "admin";

        public string? Password { get; set; }

        public string? Email { get; set; }
    }

    public sealed class EmailOptions
    {
        /// <summary>The sender address. Without an SMTP host, e-mails are written to the log.</summary>
        public string From { get; set; } = "no-reply@localhost";

        public string? FromName { get; set; }

        public SmtpOptions Smtp { get; set; } = new();
    }

    public sealed class SmtpOptions
    {
        public string? Host { get; set; }

        public int Port { get; set; } = 587;

        public string? UserName { get; set; }

        public string? Password { get; set; }

        /// <summary>Use an implicit TLS connection (SMTPS, usually port 465). Otherwise STARTTLS is used when the server offers it.</summary>
        public bool UseSsl { get; set; }
    }

    public sealed class EventsRetentionOptions
    {
        /// <summary>How many days the audit events are kept. MongoDB deletes them afterwards.</summary>
        public int RetentionDays { get; set; } = 30;
    }

    public sealed class PasswordOptions
    {
        public int RequiredLength { get; set; } = 8;

        public bool RequireDigit { get; set; }

        public bool RequireUppercase { get; set; }

        public bool RequireLowercase { get; set; }

        public bool RequireNonAlphanumeric { get; set; }

        /// <summary>Failed sign-ins before the account is locked for <see cref="LockoutMinutes"/>.</summary>
        public int MaxFailedAttempts { get; set; } = 5;

        public int LockoutMinutes { get; set; } = 5;
    }

    /// <summary>
    /// Configuration written to the database at startup when it does not exist yet. Existing documents are not
    /// changed, so the administration console stays the source of truth once the server runs.
    /// </summary>
    public sealed class SeedOptions
    {
        public List<Client> Clients { get; set; } = [];

        public List<IdentityResource> IdentityResources { get; set; } = [];

        public List<ApiScope> ApiScopes { get; set; } = [];

        public List<ApiResource> ApiResources { get; set; } = [];
    }
}
