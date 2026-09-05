namespace IdentityServer.Validation;

/// <summary>
/// Parses a POST body for secrets
/// </summary>
public class PostBodySecretParser : ISecretParser
{
    private readonly ILogger _logger;
    private readonly IdentityServerOptions _options;

    /// <summary>
    /// Creates the parser with options
    /// </summary>
    /// <param name="options">IdentityServer options</param>
    /// <param name="logger">Logger</param>
    public PostBodySecretParser(IdentityServerOptions options, ILogger<PostBodySecretParser> logger)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Returns the authentication method name that this parser implements
    /// </summary>
    /// <value>
    /// The authentication method.
    /// </value>
    public string AuthenticationMethod => OidcConstants.EndpointAuthenticationMethods.PostBody;

    /// <summary>
    /// Tries to find a secret on the context that can be used for authentication
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>
    /// A parsed secret
    /// </returns>
    public async Task<ParsedSecret?> ParseAsync(HttpContext context)
    {
        SecretLog.StartParsingSecretInPostBody(_logger);

        if (!context.Request.HasApplicationFormContentType())
        {
            SecretLog.ContentTypeNotForm(_logger);
            return null;
        }

        var body = await context.Request.ReadFormAsync();

        if (body != null)
        {
            var id = body["client_id"].FirstOrDefault();
            var secret = body["client_secret"].FirstOrDefault();

            if (id.IsPresent())
            {
                if (id.Length > _options.InputLengthRestrictions.ClientId)
                {
                    SecretLog.ClientIdTooLong(_logger);
                    return null;
                }

                if (secret.IsPresent())
                {
                    if (secret.Length > _options.InputLengthRestrictions.ClientSecret)
                    {
                        SecretLog.ClientSecretTooLong(_logger);
                        return null;
                    }

                    return new ParsedSecret
                    {
                        Id = id,
                        Credential = secret,
                        Type = IdentityServerConstants.ParsedSecretTypes.SharedSecret
                    };
                }
                else
                {
                    SecretLog.ClientIdWithoutSecretFound(_logger);

                    return new ParsedSecret
                    {
                        Id = id,
                        Type = IdentityServerConstants.ParsedSecretTypes.NoSecret
                    };
                }
            }
        }

        SecretLog.NoSecretInPostBody(_logger);
        return null;
    }
}
