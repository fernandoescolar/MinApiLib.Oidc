namespace MinApiLib.Oidc.JwtBearer;

/// <summary>
/// Adds DPoP to the events of a JWT bearer scheme: reads <c>Authorization: DPoP</c>, checks the proof of DPoP-bound
/// tokens and answers with the challenges of both schemes.
/// </summary>
internal static class DPoPJwtBearerEvents
{
    private const string InvalidToken = "invalid_token";

    public static void Attach(JwtBearerOptions options, DPoPJwtBearerOptions settings)
    {
        if (options.EventsType != null)
        {
            throw new InvalidOperationException("AddDPoP extends JwtBearerOptions.Events; schemes that use EventsType are not supported.");
        }

        var events = options.Events ??= new JwtBearerEvents();
        var challenge = options.Challenge;

        var onMessageReceived = events.OnMessageReceived;
        events.OnMessageReceived = async context =>
        {
            await onMessageReceived(context);
            MessageReceived(context, settings);
        };

        var onTokenValidated = events.OnTokenValidated;
        events.OnTokenValidated = async context =>
        {
            await TokenValidatedAsync(context, settings);
            if (context.Result == null)
            {
                await onTokenValidated(context);
            }
        };

        var onChallenge = events.OnChallenge;
        events.OnChallenge = async context =>
        {
            await onChallenge(context);
            if (!context.Handled)
            {
                Challenge(context, settings, challenge);
            }
        };
    }

    private static void MessageReceived(MessageReceivedContext context, DPoPJwtBearerOptions settings)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        var space = authorization.IndexOf(' ', StringComparison.Ordinal);
        if (space <= 0)
        {
            return;
        }

        var scheme = authorization[..space];
        var token = authorization[(space + 1)..].Trim();
        var items = context.HttpContext.Items;

        if (scheme.Equals(DPoPConstants.TokenType, StringComparison.OrdinalIgnoreCase))
        {
            items[DPoPHttpContextExtensions.SchemeItem] = DPoPConstants.TokenType;
            items[DPoPHttpContextExtensions.AccessTokenItem] = token;
            context.Token ??= token;
        }
        else if (scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
        {
            items[DPoPHttpContextExtensions.SchemeItem] = JwtBearerDefaults.AuthenticationScheme;
            items[DPoPHttpContextExtensions.AccessTokenItem] = token;
            if (settings.Mode == DPoPTokenMode.Required)
            {
                context.Fail(new DPoPValidationException(InvalidToken, "Bearer tokens are not accepted: send a DPoP-bound token with the DPoP scheme"));
            }
        }
    }

    private static async Task TokenValidatedAsync(TokenValidatedContext context, DPoPJwtBearerOptions settings)
    {
        var http = context.HttpContext;
        var scheme = http.GetAccessTokenScheme();
        var boundThumbprint = BoundThumbprint(context);

        if (scheme != DPoPConstants.TokenType)
        {
            // A stolen DPoP-bound token must not work as a bearer token.
            if (boundThumbprint != null)
            {
                context.Fail(new DPoPValidationException(InvalidToken, "DPoP-bound access token sent without the DPoP scheme"));
            }

            return;
        }

        if (boundThumbprint == null)
        {
            context.Fail(new DPoPValidationException(InvalidToken, "Access token sent with the DPoP scheme is not DPoP-bound"));
            return;
        }

        var proofs = http.Request.Headers[DPoPConstants.HeaderName];
        if (proofs.Count != 1 || string.IsNullOrEmpty(proofs[0]))
        {
            context.Fail(new DPoPValidationException(DPoPConstants.Errors.InvalidProof, "Missing or repeated DPoP proof"));
            return;
        }

        var services = http.RequestServices;
        var validator = new DPoPProofValidator(settings.ToValidationOptions(), services.GetRequiredService<IDPoPReplayCache>(), services.GetRequiredService<TimeProvider>());
        var request = http.Request;
        var url = new Uri($"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}");
        var accessToken = (string)http.Items[DPoPHttpContextExtensions.AccessTokenItem]!;
        var result = await validator.ValidateAsync(proofs[0]!, request.Method, url, accessToken, http.RequestAborted);

        if (!result.IsValid)
        {
            context.Fail(new DPoPValidationException(DPoPConstants.Errors.InvalidProof, result.Error!));
        }
        else if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(result.Thumbprint!), Encoding.ASCII.GetBytes(boundThumbprint)))
        {
            context.Fail(new DPoPValidationException(InvalidToken, "DPoP proof key does not match the key the access token is bound to"));
        }
        else if (settings.RequireNonce && !services.GetRequiredService<DPoPNonces>().IsValid(result.Nonce))
        {
            context.Fail(new DPoPValidationException(DPoPConstants.Errors.UseNonce, "Resource server requires nonce in DPoP proof"));
        }
        else
        {
            http.Items[DPoPHttpContextExtensions.ThumbprintItem] = result.Thumbprint;
        }
    }

    private static void Challenge(JwtBearerChallengeContext context, DPoPJwtBearerOptions settings, string bearerChallenge)
    {
        context.HandleResponse();

        var failure = context.AuthenticateFailure as DPoPValidationException;
        var (error, description) = failure != null
            ? (failure.Error, failure.Message)
            : context.AuthenticateFailure != null
                ? (InvalidToken, context.ErrorDescription is { Length: > 0 } detail ? detail : "The access token is not valid")
                : ((string?)null, (string?)null);
        var errorParameters = error == null ? null : $"error=\"{error}\", error_description=\"{description!.Replace('"', '\'')}\"";

        var dpop = Append($"DPoP algs=\"{string.Join(' ', settings.SupportedAlgorithms)}\"", errorParameters);
        var response = context.Response;
        response.StatusCode = StatusCodes.Status401Unauthorized;

        if (error == DPoPConstants.Errors.UseNonce)
        {
            response.Headers[DPoPConstants.NonceHeaderName] = context.HttpContext.RequestServices.GetRequiredService<DPoPNonces>().Create(settings.NonceLifetime);
        }

        if (settings.Mode == DPoPTokenMode.Required)
        {
            response.Headers.Append(HeaderNames.WWWAuthenticate, dpop);
            return;
        }

        // Both schemes are accepted, so both are announced; the error goes with the scheme the request used.
        var usedDPoP = context.HttpContext.GetAccessTokenScheme() == DPoPConstants.TokenType || failure != null;
        if (usedDPoP)
        {
            response.Headers.Append(HeaderNames.WWWAuthenticate, dpop);
            response.Headers.Append(HeaderNames.WWWAuthenticate, bearerChallenge);
        }
        else
        {
            response.Headers.Append(HeaderNames.WWWAuthenticate, Append(bearerChallenge, errorParameters));
            response.Headers.Append(HeaderNames.WWWAuthenticate, $"DPoP algs=\"{string.Join(' ', settings.SupportedAlgorithms)}\"");
        }
    }

    private static string Append(string challenge, string? parameters)
        => parameters == null ? challenge : challenge.Contains(' ', StringComparison.Ordinal) ? $"{challenge}, {parameters}" : $"{challenge} {parameters}";

    private static string? BoundThumbprint(TokenValidatedContext context)
    {
        if (context.SecurityToken is JsonWebToken jwt)
        {
            return jwt.TryGetPayloadValue<JsonElement>(DPoPConstants.Claims.Confirmation, out var cnf) &&
                   cnf.ValueKind == JsonValueKind.Object &&
                   cnf.TryGetProperty(DPoPConstants.Claims.JwkThumbprint, out var jkt) &&
                   jkt.ValueKind == JsonValueKind.String
                ? jkt.GetString()
                : null;
        }

        var claim = context.Principal?.FindFirst(DPoPConstants.Claims.Confirmation)?.Value;
        if (string.IsNullOrEmpty(claim))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(claim);
            return document.RootElement.TryGetProperty(DPoPConstants.Claims.JwkThumbprint, out var jkt) ? jkt.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>Why a request's DPoP did not pass, with the OAuth error of the challenge.</summary>
internal sealed class DPoPValidationException(string error, string description) : Exception(description)
{
    public string Error { get; } = error;
}
