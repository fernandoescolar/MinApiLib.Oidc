namespace Samples.WebClient;

public static class Oidc
{
    public static void Configure(OpenIdConnectOptions options, IConfiguration configuration, string clientId, string suffix)
    {
        var authority = configuration["Authority"]!;

        options.Authority = authority;
        options.RequireHttpsMetadata = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        options.ClientId = clientId;
        options.ClientSecret = "web-secret";
        options.ResponseType = "code";
        options.UsePkce = true;
        options.CallbackPath = "/signin-oidc" + suffix;
        options.SignedOutCallbackPath = "/signout-callback-oidc" + suffix;
        options.RemoteSignOutPath = "/signout-oidc" + suffix;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("orders.read");
        // A refresh token, which AddOpenIdConnectTokens uses shortly before the access token expires.
        options.Scope.Add("offline_access");

        // The profile claims come in the identity token. The userinfo endpoint would also need a DPoP proof for a
        // DPoP-bound token.
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    }
}
