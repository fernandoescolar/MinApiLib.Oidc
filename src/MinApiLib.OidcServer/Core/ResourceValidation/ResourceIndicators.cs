namespace MinApiLib.OidcServer.Core.ResourceValidation;

/// <summary>
/// Parsing and audience filtering for resource indicators (RFC 8707).
/// </summary>
internal static class ResourceIndicators
{
    /// <summary>
    /// Reads the <c>resource</c> parameter. Each value must be an absolute URI without a fragment (RFC 8707 section 2).
    /// </summary>
    /// <returns><c>null</c> on success, otherwise a description of the problem.</returns>
    public static string? TryRead(NameValueCollection parameters, InputLengthRestrictions limits, bool allowMultiple, out IReadOnlyList<string> resourceIndicators)
    {
        var values = parameters.GetValues(OidcConstants.AuthorizeRequest.Resource) ?? [];
        var result = new List<string>();
        resourceIndicators = result;

        foreach (var value in values)
        {
            if (value.IsMissing())
            {
                return "Empty resource indicator";
            }

            if (value.Length > limits.ResourceIndicator)
            {
                return "Resource indicator too long";
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Fragment.Length > 0 || value.Contains('#'))
            {
                return "Resource indicator must be an absolute URI without a fragment";
            }

            if (!result.Contains(value, StringComparer.Ordinal))
            {
                result.Add(value);
            }
        }

        if (!allowMultiple && result.Count > 1)
        {
            return "Only one resource indicator is supported at the token endpoint";
        }

        return null;
    }

    /// <summary>
    /// Returns a copy of <paramref name="resources"/> whose API resources are limited to the requested indicators.
    /// Without indicators the APIs that require one are left out. Indicators that do not name one of the APIs are
    /// added to <paramref name="invalid"/>.
    /// </summary>
    public static Resources Apply(Resources resources, IEnumerable<string>? requested, ICollection<string>? invalid = null)
    {
        var indicators = requested?.ToArray() ?? [];

        if (indicators.Length > 0 && invalid != null)
        {
            foreach (var indicator in indicators)
            {
                if (!resources.ApiResources.Any(x => string.Equals(x.Name, indicator, StringComparison.Ordinal)))
                {
                    invalid.Add(indicator);
                }
            }
        }

        var apis = indicators.Length == 0
            ? resources.ApiResources.Where(x => !x.RequireResourceIndicator)
            : resources.ApiResources.Where(x => indicators.Contains(x.Name, StringComparer.Ordinal));

        return new Resources(resources.IdentityResources, apis, resources.ApiScopes)
        {
            OfflineAccess = resources.OfflineAccess
        };
    }

    /// <summary>
    /// Returns a copy of <paramref name="result"/> whose API resources are limited to the requested indicators.
    /// </summary>
    public static ResourceValidationResult Apply(ResourceValidationResult result, IEnumerable<string>? requested)
        => new(Apply(result.Resources, requested), result.ParsedScopes);
}
