using IDPoPReplayCache = MinApiLib.Oidc.DPoP.IDPoPReplayCache;
using SharedDPoPProofValidator = MinApiLib.Oidc.DPoP.DPoPProofValidator;
using SharedDPoPProofValidationOptions = MinApiLib.Oidc.DPoP.DPoPProofValidationOptions;

namespace MinApiLib.Oidc.Server.Core.DPoP;

/// <summary>
/// Validates DPoP proofs as described in RFC 9449 section 4.3, with the validator of MinApiLib.Oidc.DPoP that APIs
/// use too. It takes its settings from <see cref="OidcServerOptions.DPoP"/> and remembers the accepted proofs in the
/// <see cref="IReplayCache"/>.
/// </summary>
[RegisterInIServiceCollection(typeof(IDPoPProofValidator))]
public class DefaultDPoPProofValidator : IDPoPProofValidator
{
    private const string ReplayCachePurpose = "DPoPProofReplay-";

    private readonly OidcServerOptions _options;
    private readonly IDPoPReplayCache _replayCache;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDPoPProofValidator"/> class.
    /// </summary>
    public DefaultDPoPProofValidator(OidcServerOptions options, IReplayCache replayCache, TimeProvider clock, ILogger<DefaultDPoPProofValidator> logger)
    {
        _options = options;
        _replayCache = new ReplayCache(replayCache);
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc/>
    public virtual async Task<DPoPProofValidationResult> ValidateAsync(DPoPProofValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ProofToken.IsMissing() || context.ProofToken.Length > _options.InputLengthRestrictions.Jwt)
        {
            return Invalid("Missing or too long DPoP proof");
        }

        if (!Uri.TryCreate(context.Url, UriKind.Absolute, out var url))
        {
            return Invalid("DPoP proof htu does not match the request URL");
        }

        var validator = new SharedDPoPProofValidator(
            new SharedDPoPProofValidationOptions
            {
                SupportedAlgorithms = _options.DPoP.SupportedSigningAlgorithms,
                ProofLifetime = _options.DPoP.ProofTokenValidityDuration,
                ClockSkew = _options.DPoP.ClockSkew
            },
            _replayCache,
            _clock);

        var result = await validator.ValidateAsync(context.ProofToken, context.Method, url, context.AccessToken);
        if (!result.IsValid)
        {
            if (result.Exception != null)
            {
                RequestValidationLog.DPoPProofException(_logger, result.Error!, result.Exception);
            }

            return Invalid(result.Error!);
        }

        return new DPoPProofValidationResult
        {
            IsError = false,
            JsonWebKeyThumbprint = result.Thumbprint
        };
    }

    private DPoPProofValidationResult Invalid(string description)
    {
        RequestValidationLog.DPoPValidationFailed(_logger, description);
        return new DPoPProofValidationResult
        {
            IsError = true,
            Error = OidcConstants.TokenErrors.InvalidDPoPProof,
            ErrorDescription = description
        };
    }

    /// <summary>
    /// The server's <see cref="IReplayCache"/>, which the host can replace (with a distributed cache, for example), as
    /// the replay cache of the shared validator.
    /// </summary>
    private sealed class ReplayCache(IReplayCache cache) : IDPoPReplayCache
    {
        public async ValueTask<bool> TryAddAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            if (await cache.ExistsAsync(ReplayCachePurpose, key))
            {
                return false;
            }

            await cache.AddAsync(ReplayCachePurpose, key, expiresAt);
            return true;
        }
    }
}
