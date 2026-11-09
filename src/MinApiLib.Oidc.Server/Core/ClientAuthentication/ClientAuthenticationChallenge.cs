namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// The status code of an <c>invalid_client</c> error (RFC 6749 section 5.2): 401 with a <c>WWW-Authenticate</c>
/// challenge when the client tried to authenticate with the <c>Authorization</c> header, which the RFC requires, or when
/// the endpoint answers 401 anyway; the endpoint's status code otherwise.
/// </summary>
internal static class ClientAuthenticationChallenge
{
    public const string BasicChallenge = "Basic realm=\"OidcServer\"";

    public static void SetStatusCode(HttpContext context, string error, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        if (error == OidcConstants.TokenErrors.InvalidClient
            && (statusCode == HttpStatusCode.Unauthorized || !StringValues.IsNullOrEmpty(context.Request.Headers.Authorization)))
        {
            statusCode = HttpStatusCode.Unauthorized;
            context.Response.Headers.WWWAuthenticate = BasicChallenge;
        }

        context.Response.StatusCode = (int)statusCode;
    }
}
