namespace MinApiLib.OidcServer.Services;

/// <summary>
/// Default token creation service
/// </summary>
[RegisterInIServiceCollection(typeof(ITokenCreationService))]
public class DefaultTokenCreationService : ITokenCreationService
{
    /// <summary>
    /// The key service
    /// </summary>
    protected readonly IKeyMaterialService Keys;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    ///  The clock
    /// </summary>
    protected readonly TimeProvider Clock;

    /// <summary>
    /// The options
    /// </summary>
    protected readonly OidcServerOptions Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTokenCreationService"/> class.
    /// </summary>
    /// <param name="clock">The options.</param>
    /// <param name="keys">The keys.</param>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    public DefaultTokenCreationService(
        TimeProvider clock,
        IKeyMaterialService keys,
        OidcServerOptions options,
        ILogger<DefaultTokenCreationService> logger)
    {
        Clock = clock;
        Keys = keys;
        Options = options;
        Logger = logger;
    }

    /// <summary>
    /// Creates the token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>
    /// A protected and serialized security token
    /// </returns>
    public virtual async Task<string> CreateTokenAsync(Token token)
    {
        var header = await CreateHeaderAsync(token);
        var payload = await CreatePayloadAsync(token);

        return await CreateJwtAsync(new JwtSecurityToken(header, payload));
    }

    /// <summary>
    /// Creates the JWT header
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The JWT header</returns>
    protected virtual async Task<JwtHeader> CreateHeaderAsync(Token token)
    {
        var credential = await Keys.GetSigningCredentialsAsync(token.AllowedSigningAlgorithms);

        if (credential == null)
        {
            throw new InvalidOperationException("No signing credential is configured. Can't create JWT token");
        }

        var header = new JwtHeader(credential);

        if (credential.Key is X509SecurityKey x509Key)
        {
            var cert = x509Key.Certificate;
            if (Clock.GetUtcNow().UtcDateTime > cert.NotAfter)
            {
                ServicesLog.CertificateExpired(Logger, cert.Subject, cert.NotAfter.ToString(CultureInfo.InvariantCulture));
            }

            header["x5t"] = Base64Url.EncodeToString(cert.GetCertHash());
        }

        if (token.Type == TokenTypes.AccessToken)
        {
            if (Options.AccessTokenJwtType.IsPresent())
            {
                header["typ"] = Options.AccessTokenJwtType;
            }
        }
        else if (token.Type == TokenTypes.LogoutToken)
        {
            if (Options.LogoutTokenJwtType.IsPresent())
            {
                header["typ"] = Options.LogoutTokenJwtType;
            }
        }

        return header;
    }

    /// <summary>
    /// Creates the JWT payload
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The JWT payload</returns>
    protected virtual Task<JwtPayload> CreatePayloadAsync(Token token)
    {
        var payload = token.CreateJwtPayload(Clock, Options, Logger);
        return Task.FromResult(payload);
    }

    /// <summary>
    /// Applies the signature to the JWT
    /// </summary>
    /// <param name="jwt">The JWT object.</param>
    /// <returns>The signed JWT</returns>
    protected virtual Task<string> CreateJwtAsync(JwtSecurityToken jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        return Task.FromResult(handler.WriteToken(jwt));
    }
}
