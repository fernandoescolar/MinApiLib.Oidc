namespace MinApiLib.OidcServer;

/// <summary>
/// Extensions for OidcServerTools
/// </summary>
public static class OidcServerToolsExtensions
{
    /// <summary>
    /// Issues the client JWT.
    /// </summary>
    /// <param name="tools">The tools.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="scopes">The scopes.</param>
    /// <param name="audiences">The audiences.</param>
    /// <param name="additionalClaims">Additional claims</param>
    /// <returns></returns>
    public static async Task<string> IssueClientJwtAsync(this OidcServerTools tools,
        string clientId,
        int lifetime,
        IEnumerable<string>? scopes = null,
        IEnumerable<string>? audiences = null,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new HashSet<Claim>(new ClaimComparer());
        var options = tools.Options;

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(claim);
            }
        }

        claims.Add(new Claim(JwtClaimTypes.ClientId, clientId));

        if (scopes != null && scopes.Any())
        {
            foreach (var scope in scopes)
            {
                claims.Add(new Claim(JwtClaimTypes.Scope, scope));
            }
        }

        if (options.EmitStaticAudienceClaim)
        {
            var issuer = await tools.IssuerNameService.GetCurrentAsync();
            claims.Add(new Claim(JwtClaimTypes.Audience, string.Format(CultureInfo.InvariantCulture, Constants.AccessTokenAudienceFormat, issuer.EnsureTrailingSlash())));
        }

        if (audiences != null && audiences.Any())
        {
            foreach (var audience in audiences)
            {
                claims.Add(new Claim(JwtClaimTypes.Audience, audience));
            }
        }

        return await tools.IssueJwtAsync(lifetime, claims);
    }
}
