namespace IdP.Users;

/// <summary>
/// Issues the user's claims to the tokens and the userinfo endpoint from the user document: profile, e-mail, phone,
/// roles (the user's and those of its groups), groups, the free attributes and the extra claims. A disabled or locked
/// user is not active, so it gets no new tokens.
/// </summary>
public sealed class IdPProfileService(IdPMongoContext db, ILogger<IdPProfileService> logger) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var subject = context.Subject.GetSubjectId();
        var user = await db.Users.Find(u => u.Id == subject).FirstOrDefaultAsync();
        if (user == null)
        {
            logger.LogWarning("Profile requested for unknown user {Subject}", subject);
            return;
        }

        var claims = await GetClaimsAsync(user);
        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var subject = context.Subject.GetSubjectId();
        var user = await db.Users.Find(u => u.Id == subject).FirstOrDefaultAsync();
        context.IsActive = user != null && IsActive(user);
    }

    public static bool IsActive(IdPUser user) => user.Enabled && (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow);

    /// <summary>Every claim the user can be issued; the resources decide which ones a client gets.</summary>
    public async Task<List<Claim>> GetClaimsAsync(IdPUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, user.Id),
            new(JwtClaimTypes.Name, user.DisplayName),
            new(JwtClaimTypes.PreferredUserName, user.UserName),
            new(JwtClaimTypes.UpdatedAt, new DateTimeOffset(user.UpdatedAt).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            claims.Add(new Claim(JwtClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            claims.Add(new Claim(JwtClaimTypes.FamilyName, user.LastName));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtClaimTypes.Email, user.Email));
            claims.Add(new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber));
            claims.Add(new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }

        var groups = user.Groups.Count == 0
            ? []
            : await db.Groups.Find(g => user.Groups.Contains(g.Id)).ToListAsync();

        foreach (var role in user.Roles.Concat(groups.SelectMany(g => g.Roles)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(JwtClaimTypes.Role, role));
        }

        foreach (var group in groups)
        {
            claims.Add(new Claim("groups", group.Name));
        }

        foreach (var (type, value) in user.Attributes.Where(a => !string.IsNullOrWhiteSpace(a.Value)))
        {
            claims.Add(new Claim(type, value));
        }

        foreach (var claim in user.Claims)
        {
            claims.Add(new Claim(claim.Type, claim.Value));
        }

        return claims;
    }
}
