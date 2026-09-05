namespace IdentityServer.Endpoints.Results;

/// <summary>
/// Builds the return URL of a pushed authorization request (RFC 9126) that needs user interaction.
/// </summary>
internal static class PushedAuthorizationReturnUrl
{
    /// <summary>
    /// For a pushed request, saves the current parameters (for example with <c>prompt</c> already processed) back to
    /// the store and returns <paramref name="returnUrl"/> with only <c>client_id</c> and <c>request_uri</c>, so the
    /// pushed parameters never travel through the browser. Returns <c>null</c> for a request that was not pushed.
    /// </summary>
    public static async Task<string?> TryCreateAsync(HttpContext context, ValidatedAuthorizeRequest request, string returnUrl)
    {
        if (request.PushedAuthorizationReferenceValue == null || request.PushedAuthorizationRequest == null)
        {
            return null;
        }

        var store = context.RequestServices.GetRequiredService<IPushedAuthorizationRequestStore>();
        request.PushedAuthorizationRequest.Parameters = request.Raw.ToFullDictionary();
        await store.UpdatePushedAuthorizationRequestAsync(request.PushedAuthorizationReferenceValue, request.PushedAuthorizationRequest);

        return returnUrl
            .AddQueryString(OidcConstants.AuthorizeRequest.ClientId, request.PushedAuthorizationRequest.ClientId)
            .AddQueryString(OidcConstants.AuthorizeRequest.RequestUri, Constants.PushedAuthorization.RequestUriPrefix + request.PushedAuthorizationReferenceValue);
    }
}
