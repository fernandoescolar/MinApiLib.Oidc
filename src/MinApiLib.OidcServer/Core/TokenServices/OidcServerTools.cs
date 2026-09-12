namespace MinApiLib.OidcServer.Core.TokenServices;

/// <summary>
/// Class for useful helpers for interacting with OidcServer
/// </summary>
[RegisterInIServiceCollection]
public class OidcServerTools
{
    private readonly ITokenCreationService _tokenCreation;
    private readonly TimeProvider _clock;

    internal IIssuerNameService IssuerNameService { get; }

    internal OidcServerOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcServerTools" /> class.
    /// </summary>
    /// <param name="issuerNameService">The issuer name service.</param>
    /// <param name="tokenCreation">The token creation service.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="options">The options.</param>
    public OidcServerTools(IIssuerNameService issuerNameService, ITokenCreationService tokenCreation, TimeProvider clock, OidcServerOptions options)
    {
        IssuerNameService = issuerNameService;
        _tokenCreation = tokenCreation;
        _clock = clock;
        Options = options;
    }

    /// <summary>
    /// Issues a JWT access token using the issuer of the current request.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">claims</exception>
    public virtual async Task<string> IssueJwtAsync(int lifetime, IEnumerable<Claim> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var issuer = await IssuerNameService.GetCurrentAsync();
        return await IssueJwtAsync(lifetime, issuer, TokenTypes.AccessToken, claims);
    }

    /// <summary>
    /// Issues a JWT access token.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="issuer">The issuer.</param>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">claims</exception>
    public virtual Task<string> IssueJwtAsync(int lifetime, string issuer, IEnumerable<Claim> claims)
    {
        return IssueJwtAsync(lifetime, issuer, TokenTypes.AccessToken, claims);
    }

    /// <summary>
    /// Issues a JWT of the given token type. The type drives the JWT typ header,
    /// for example <c>at+jwt</c> for access tokens and <c>logout+jwt</c> for logout tokens.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="issuer">The issuer.</param>
    /// <param name="tokenType">The token type, see <see cref="OidcServerConstants.TokenTypes"/>.</param>
    /// <param name="claims">The claims.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">issuer, tokenType or claims</exception>
    public virtual Task<string> IssueJwtAsync(int lifetime, string issuer, string tokenType, IEnumerable<Claim> claims)
    {
        if (String.IsNullOrWhiteSpace(issuer)) throw new ArgumentNullException(nameof(issuer));
        if (String.IsNullOrWhiteSpace(tokenType)) throw new ArgumentNullException(nameof(tokenType));
        ArgumentNullException.ThrowIfNull(claims);

        var token = new Token
        {
            CreationTime = _clock.GetUtcNow().UtcDateTime,
            Issuer = issuer,
            Lifetime = lifetime,
            Type = tokenType,

            Claims = new HashSet<Claim>(claims, new ClaimComparer())
        };

        return _tokenCreation.CreateTokenAsync(token);
    }
}
