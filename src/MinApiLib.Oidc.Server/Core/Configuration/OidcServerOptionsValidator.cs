using Microsoft.Extensions.Options;

namespace MinApiLib.Oidc.Server.Core.Configuration;

/// <summary>
/// Rejects inconsistent <see cref="OidcServerOptions"/> when the host starts, instead of failing on the first
/// request that happens to read the bad value.
/// </summary>
internal sealed class OidcServerOptionsValidator : IValidateOptions<OidcServerOptions>
{
    public ValidateOptionsResult Validate(string? name, OidcServerOptions options)
    {
        var failures = new List<string>();

        if (options.IssuerUri is { Length: > 0 } issuer &&
            (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
             (issuerUri.Scheme != Uri.UriSchemeHttps && issuerUri.Scheme != Uri.UriSchemeHttp) ||
             issuerUri.Query.Length > 0 || issuerUri.Fragment.Length > 0))
        {
            failures.Add($"{nameof(options.IssuerUri)} must be an absolute http(s) URL without query or fragment (RFC 8414 section 2).");
        }

        if (options.Authentication.CookieLifetime <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(options.Authentication)}.{nameof(options.Authentication.CookieLifetime)} must be positive.");
        }

        foreach (var property in typeof(InputLengthRestrictions).GetProperties().Where(p => p.PropertyType == typeof(int)))
        {
            if ((int)property.GetValue(options.InputLengthRestrictions)! <= 0)
            {
                failures.Add($"{nameof(options.InputLengthRestrictions)}.{property.Name} must be positive.");
            }
        }

        if (options.DeviceFlow.Interval <= 0 || options.DeviceFlow.UserCodeMaxFailedAttempts <= 0)
        {
            failures.Add($"{nameof(options.DeviceFlow)}.{nameof(options.DeviceFlow.Interval)} and {nameof(options.DeviceFlow.UserCodeMaxFailedAttempts)} must be positive.");
        }

        if (options.PushedAuthorization.Lifetime <= 0)
        {
            failures.Add($"{nameof(options.PushedAuthorization)}.{nameof(options.PushedAuthorization.Lifetime)} must be positive.");
        }

        ValidateDPoP(options.DPoP, failures);
        ValidateKeyManagement(options.KeyManagement, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateDPoP(DPoPOptions dpop, List<string> failures)
    {
        if (dpop.ProofTokenValidityDuration <= TimeSpan.Zero || dpop.ClockSkew < TimeSpan.Zero)
        {
            failures.Add("DPoP.ProofTokenValidityDuration must be positive and DPoP.ClockSkew must not be negative.");
        }

        if (dpop.SupportedSigningAlgorithms.Count == 0 || dpop.SupportedSigningAlgorithms.Any(a => !OidcServerConstants.SupportedSigningAlgorithms.Contains(a)))
        {
            failures.Add("DPoP.SupportedSigningAlgorithms must be a non-empty list of asymmetric JWS algorithms (RS*, PS*, ES*).");
        }
    }

    private static void ValidateKeyManagement(KeyManagementOptions keys, List<string> failures)
    {
        if (!keys.Enabled)
        {
            return;
        }

        if (keys.SigningAlgorithms.Count == 0 || keys.SigningAlgorithms.Any(a => !OidcServerConstants.SupportedSigningAlgorithms.Contains(a)))
        {
            failures.Add("KeyManagement.SigningAlgorithms must be a non-empty list of RS*, PS* or ES* algorithms.");
        }

        if (keys.PropagationTime <= TimeSpan.Zero || keys.RotationInterval <= keys.PropagationTime)
        {
            failures.Add("KeyManagement.RotationInterval must be longer than KeyManagement.PropagationTime, which must be positive.");
        }

        if (keys.RetentionDuration < TimeSpan.Zero)
        {
            failures.Add("KeyManagement.RetentionDuration must not be negative.");
        }

        if (keys.KeyCacheDuration <= TimeSpan.Zero || keys.KeyCacheDuration >= keys.PropagationTime)
        {
            failures.Add("KeyManagement.KeyCacheDuration must be positive and shorter than KeyManagement.PropagationTime, so every instance sees a new key before it signs.");
        }
    }
}
