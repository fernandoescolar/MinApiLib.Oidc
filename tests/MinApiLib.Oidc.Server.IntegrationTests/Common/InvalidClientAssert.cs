namespace MinApiLib.Oidc.Server.IntegrationTests.Common;

internal static class InvalidClientAssert
{
    /// <summary>
    /// A client that authenticated with the Authorization header gets 401 with a Basic challenge (RFC 6749 section
    /// 5.2). An IdentityModel-style client reports it as an HTTP error; the JSON still says invalid_client.
    /// </summary>
    public static void Unauthorized(ProtocolResponse response)
    {
        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Http, response.ErrorType);
        Assert.Equal(HttpStatusCode.Unauthorized, response.HttpStatusCode);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, response.TryGet("error"));
        Assert.Equal("Basic realm=\"OidcServer\"", Assert.Single(response.HttpResponse!.Headers.WwwAuthenticate).ToString());
    }
}
