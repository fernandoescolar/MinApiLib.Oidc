namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Default implementation of redirect URI validator. Validates the URIs against
/// the client's configured URIs.
/// </summary>
public class StrictRedirectUriValidator : IRedirectUriValidator
{
    /// <summary>
    /// Checks if a given URI string is in a collection of strings. Following RFC 6749 section 3.1.2.3 and
    /// RFC 3986 section 6.2.2.1, only the scheme and host are compared case-insensitively; the port, path
    /// and query must match exactly.
    /// </summary>
    /// <param name="uris">The uris.</param>
    /// <param name="requestedUri">The requested URI.</param>
    /// <returns></returns>
    protected static bool StringCollectionContainsString(IEnumerable<string> uris, string requestedUri)
    {
        if (uris == null || requestedUri == null) return false;

        return uris.Any(uri => UrisAreEqual(uri, requestedUri));
    }

    private static bool UrisAreEqual(string registered, string requested)
    {
        if (string.Equals(registered, requested, StringComparison.Ordinal)) return true;
        if (registered == null || registered.Length != requested.Length) return false;

        var caseInsensitiveLength = SchemeAndHostLength(requested);
        if (caseInsensitiveLength != SchemeAndHostLength(registered)) return false;

        return string.Compare(registered, 0, requested, 0, caseInsensitiveLength, StringComparison.OrdinalIgnoreCase) == 0 &&
               string.CompareOrdinal(registered, caseInsensitiveLength, requested, caseInsensitiveLength, requested.Length - caseInsensitiveLength) == 0;
    }

    // Length of the case-insensitive prefix: "scheme://host[:port]" for hierarchical URIs, "scheme:" otherwise.
    private static int SchemeAndHostLength(string uri)
    {
        var schemeSeparator = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator < 0)
        {
            return Math.Max(uri.IndexOf(':', StringComparison.Ordinal), 0);
        }

        var pathStart = uri.IndexOfAny(['/', '?', '#'], schemeSeparator + 3);
        return pathStart < 0 ? uri.Length : pathStart;
    }

    /// <summary>
    /// Determines whether a redirect URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns>
    ///   <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public virtual Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
    {
        return Task.FromResult(StringCollectionContainsString(client.RedirectUris, requestedUri));
    }

    /// <summary>
    /// Determines whether a post logout URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns>
    ///   <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public virtual Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
    {
        return Task.FromResult(StringCollectionContainsString(client.PostLogoutRedirectUris, requestedUri));
    }
}
