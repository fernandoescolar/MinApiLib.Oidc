namespace MinApiLib.Oidc.Server.Core.TokenServices;

public enum BearerTokenUsageType
{
    AuthorizationHeader = 0,
    PostBody = 1,
    QueryString = 2,

    /// <summary>
    /// The token was sent in the Authorization header with the DPoP scheme (RFC 9449 section 7.1).
    /// </summary>
    DPoPAuthorizationHeader = 3
}
