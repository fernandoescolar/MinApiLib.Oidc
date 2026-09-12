namespace MinApiLib.OidcServer.Core.DPoP;

/// <summary>
/// Reads DPoP (RFC 9449) proofs from HTTP requests.
/// </summary>
internal static class DPoPRequest
{
    /// <summary>
    /// Reads the <c>DPoP</c> header. Returns <c>false</c> when it is sent more than once (RFC 9449 section 4.3).
    /// </summary>
    public static bool TryGetProof(HttpContext context, out string? proof)
    {
        var values = context.Request.Headers[OidcConstants.HttpHeaders.DPoP];
        proof = values.Count == 1 ? values[0] : null;
        return values.Count <= 1;
    }

    /// <summary>
    /// The URL of the request as the client addressed it, without query and fragment.
    /// </summary>
    public static string GetRequestUrl(HttpContext context)
        => context.GetOidcServerHost().RemoveTrailingSlash() + context.Request.PathBase.Value + context.Request.Path.Value;

    /// <summary>
    /// Validates the proof of a request to a protected resource of OidcServer (userinfo, local APIs) and checks
    /// that the access token is bound to the proof key, or that it is not DPoP-bound when sent as a bearer token.
    /// </summary>
    /// <returns><c>null</c> when the request is acceptable, otherwise a description of the problem.</returns>
    public static async Task<string?> ValidateProtectedResourceRequestAsync(HttpContext context, string token, bool isDPoPScheme, IEnumerable<Claim>? tokenClaims)
    {
        var boundThumbprint = GetBoundThumbprint(tokenClaims);

        if (!isDPoPScheme)
        {
            // CVE-2024-49755: a DPoP-bound token presented as a bearer token must be rejected.
            return boundThumbprint == null ? null : "DPoP-bound access token sent without the DPoP scheme";
        }

        if (boundThumbprint == null)
        {
            return "Access token sent with the DPoP scheme is not DPoP-bound";
        }

        if (!TryGetProof(context, out var proof) || proof.IsMissing())
        {
            return "Missing or repeated DPoP proof";
        }

        var validator = context.RequestServices.GetRequiredService<IDPoPProofValidator>();
        var result = await validator.ValidateAsync(new DPoPProofValidationContext
        {
            ProofToken = proof,
            Method = context.Request.Method,
            Url = GetRequestUrl(context),
            AccessToken = token
        });

        if (result.IsError)
        {
            return result.ErrorDescription;
        }

        return FixedTimeComparer.IsEqual(result.JsonWebKeyThumbprint, boundThumbprint)
            ? null
            : "DPoP proof key does not match the key the access token is bound to";
    }

    /// <summary>
    /// The <c>jkt</c> member of the <c>cnf</c> claim, or <c>null</c> when the token is not DPoP-bound.
    /// </summary>
    public static string? GetBoundThumbprint(IEnumerable<Claim>? claims)
    {
        var cnf = claims?.FirstOrDefault(c => c.Type == JwtClaimTypes.Confirmation)?.Value;
        return GetBoundThumbprint(cnf);
    }

    /// <summary>
    /// The <c>jkt</c> member of a confirmation value, or <c>null</c> when it has none.
    /// </summary>
    public static string? GetBoundThumbprint(string? confirmation)
    {
        if (confirmation.IsMissing())
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(confirmation);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty(JwtClaimTypes.ConfirmationMethods.JwkThumbprint, out var jkt) &&
                   jkt.ValueKind == JsonValueKind.String
                ? jkt.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
