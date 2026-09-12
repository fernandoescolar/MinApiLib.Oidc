namespace MinApiLib.OidcServer.Features.PushedAuthorization;

/// <summary>
/// Stores pushed authorization requests (RFC 9126).
/// </summary>
public interface IPushedAuthorizationRequestStore
{
    /// <summary>
    /// Stores a pushed authorization request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The handle used as the reference value of the <c>request_uri</c>.</returns>
    Task<string> StorePushedAuthorizationRequestAsync(PushedAuthorizationRequest request);

    /// <summary>
    /// Gets a pushed authorization request.
    /// </summary>
    /// <param name="handle">The handle.</param>
    Task<PushedAuthorizationRequest?> GetPushedAuthorizationRequestAsync(string handle);

    /// <summary>
    /// Replaces a pushed authorization request, keeping its handle and expiration.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <param name="request">The request.</param>
    Task UpdatePushedAuthorizationRequestAsync(string handle, PushedAuthorizationRequest request);

    /// <summary>
    /// Removes a pushed authorization request.
    /// </summary>
    /// <param name="handle">The handle.</param>
    Task RemovePushedAuthorizationRequestAsync(string handle);
}
