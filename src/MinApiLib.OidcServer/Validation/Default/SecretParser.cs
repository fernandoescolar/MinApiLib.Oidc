namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Uses the registered secret parsers to parse a secret on the current request
/// </summary>
[RegisterInIServiceCollection(typeof(ISecretsListParser))]
public class SecretParser : ISecretsListParser
{
    private readonly ILogger _logger;
    private readonly IEnumerable<ISecretParser> _parsers;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretParser"/> class.
    /// </summary>
    /// <param name="parsers">The parsers.</param>
    /// <param name="logger">The logger.</param>
    public SecretParser(IEnumerable<ISecretParser> parsers, ILogger<ISecretsListParser> logger)
    {
        _parsers = parsers;
        _logger = logger;
    }

    /// <summary>
    /// Checks the context to find a secret.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task<ParsedSecret?> ParseAsync(HttpContext context)
    {
        ParsedSecret? bestSecret = null;
        ParsedSecret? credential = null;
        foreach (var parser in _parsers)
        {
            var parsedSecret = await parser.ParseAsync(context);
            if (parsedSecret == null)
            {
                continue;
            }

            SecretLog.ParserFoundSecret(_logger, parser.GetType().Name);

            if (parsedSecret.Type == OidcServerConstants.ParsedSecretTypes.NoSecret)
            {
                bestSecret ??= parsedSecret;
                continue;
            }

            // RFC 6749 section 2.3: a client MUST NOT use more than one authentication method per request.
            // The mTLS certificate is transport-level and may accompany another method (RFC 8705 section 3).
            if (credential != null &&
                credential.Type != OidcServerConstants.ParsedSecretTypes.X509Certificate &&
                parsedSecret.Type != OidcServerConstants.ParsedSecretTypes.X509Certificate)
            {
                SecretLog.MultipleAuthenticationMethods(_logger);
                return null;
            }

            if (credential == null || credential.Type == OidcServerConstants.ParsedSecretTypes.X509Certificate)
            {
                credential = parsedSecret;
            }
        }

        bestSecret = credential ?? bestSecret;

        if (bestSecret != null)
        {
            SecretLog.SecretIdFound(_logger, bestSecret.Id);
            return bestSecret;
        }

        SecretLog.ParserFoundNoSecret(_logger);
        return null;
    }

    /// <summary>
    /// Gets all available authentication methods.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetAvailableAuthenticationMethods()
    {
        return _parsers.Select(p => p.AuthenticationMethod).Where(p => !String.IsNullOrWhiteSpace(p));
    }
}
