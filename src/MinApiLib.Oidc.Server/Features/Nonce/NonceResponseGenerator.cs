namespace MinApiLib.Oidc.Server.Features.Nonce;

[RegisterInIServiceCollection(typeof(INonceResponseGenerator))]
public class NonceResponseGenerator : INonceResponseGenerator
{
    private readonly TimeProvider _clock;
    private readonly IAuthorizationNonceStore _nonceStore;
    private readonly ILogger<NonceResponseGenerator> _logger;

    public NonceResponseGenerator(TimeProvider clock, IAuthorizationNonceStore nonceStore, ILogger<NonceResponseGenerator> logger)
    {
        _clock = clock;
        _nonceStore = nonceStore;
        _logger = logger;
    }

    public async Task<NonceResponse> ProcessAsync(NonceRequestValidationResult validationResult)
    {
        _logger.CreatingNonceEndpointResponse();
        var nonce = new AuthorizationNonce
        {
            SessionId = validationResult.SessionId,
            ClientId = validationResult.TokenValidationResult.Client?.ClientId ?? throw new InvalidOperationException("The access token has no client."),
            CreationTime = _clock.GetUtcNow().UtcDateTime,
            Lifetime = validationResult.TokenValidationResult.Client.AuthorizationNonceLifetime,
            Subject = validationResult.Subject,
            Nonce = Guid.NewGuid().ToString()
        };

        var id = await _nonceStore.StoreAuthorizationNonceAsync(nonce);
        return new NonceResponse
        {
            Nonce = id,
            Lifetime = nonce.Lifetime
        };
    }
}

internal static partial class NonceResponseGeneratorLoggerExtensions
{
    [LoggerMessage(0x1001F, LogLevel.Debug, "Creating Nonce endpoint response.")]
    public static partial void CreatingNonceEndpointResponse(this ILogger<NonceResponseGenerator> logger);
}
