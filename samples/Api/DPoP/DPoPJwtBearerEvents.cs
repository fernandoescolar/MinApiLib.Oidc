namespace Samples.Api.DPoP;

/// <summary>
/// Extends JWT bearer authentication with DPoP: reads the token of <c>Authorization: DPoP</c>, checks the proof of a
/// DPoP-bound token against <c>cnf.jkt</c> and answers with a <c>DPoP</c> challenge.
/// </summary>
public sealed class DPoPJwtBearerEvents : JwtBearerEvents
{
    /// <summary>The scheme the request used, <c>Bearer</c> or <c>DPoP</c>, for the endpoints and the challenge.</summary>
    public const string SchemeItem = "sample.token_scheme";

    public const string ThumbprintItem = "sample.jkt";

    public override Task MessageReceived(MessageReceivedContext context)
    {
        string authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("DPoP ", StringComparison.OrdinalIgnoreCase))
        {
            context.Token = authorization["DPoP ".Length..].Trim();
            context.HttpContext.Items[SchemeItem] = "DPoP";
        }
        else if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.HttpContext.Items[SchemeItem] = "Bearer";
        }

        return Task.CompletedTask;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var http = context.HttpContext;
        var scheme = http.Items[SchemeItem] as string;
        var boundThumbprint = GetBoundThumbprint(context.Principal?.FindFirst("cnf")?.Value);

        if (scheme != "DPoP")
        {
            // A stolen DPoP-bound token must not work as a bearer token.
            if (boundThumbprint != null)
            {
                context.Fail(new DPoPValidationException("DPoP-bound access token sent without the DPoP scheme"));
            }

            return;
        }

        if (boundThumbprint == null)
        {
            context.Fail(new DPoPValidationException("Access token sent with the DPoP scheme is not DPoP-bound"));
            return;
        }

        var proofs = http.Request.Headers["DPoP"];
        if (proofs.Count != 1 || string.IsNullOrEmpty(proofs[0]))
        {
            context.Fail(new DPoPValidationException("Missing or repeated DPoP proof"));
            return;
        }

        var validator = http.RequestServices.GetRequiredService<DPoPProofValidator>();
        var url = new Uri($"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}{http.Request.Path}");
        var accessToken = ((JsonWebToken)context.SecurityToken).EncodedToken;
        var (thumbprint, error) = await validator.ValidateAsync(proofs[0]!, http.Request.Method, url, accessToken);

        if (error != null)
        {
            context.Fail(new DPoPValidationException(error));
        }
        else if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(thumbprint!), Encoding.ASCII.GetBytes(boundThumbprint)))
        {
            context.Fail(new DPoPValidationException("DPoP proof key does not match the key the access token is bound to"));
        }
        else
        {
            http.Items[ThumbprintItem] = thumbprint;
        }
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        // Both schemes are accepted, so both are announced; the error goes with the scheme the request used.
        context.HandleResponse();

        var description = context.AuthenticateFailure switch
        {
            DPoPValidationException dpop => dpop.Message,
            null => null,
            _ => "The access token is not valid"
        };
        var error = description == null ? string.Empty : $", error=\"invalid_token\", error_description=\"{description}\"";
        var usedDPoP = context.HttpContext.Items[SchemeItem] as string == "DPoP" ||
                       context.AuthenticateFailure is DPoPValidationException;
        var algs = string.Join(' ', DPoPProofValidator.SupportedAlgorithms);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.Append("WWW-Authenticate", usedDPoP
            ? $"DPoP algs=\"{algs}\"{error}"
            : $"Bearer realm=\"orders\"{error}");
        context.Response.Headers.Append("WWW-Authenticate", usedDPoP
            ? "Bearer realm=\"orders\""
            : $"DPoP algs=\"{algs}\"");

        return Task.CompletedTask;
    }

    private static string? GetBoundThumbprint(string? cnf)
    {
        if (string.IsNullOrEmpty(cnf))
        {
            return null;
        }

        using var document = JsonDocument.Parse(cnf);
        return document.RootElement.TryGetProperty("jkt", out var jkt) ? jkt.GetString() : null;
    }
}

public sealed class DPoPValidationException(string message) : Exception(message);
