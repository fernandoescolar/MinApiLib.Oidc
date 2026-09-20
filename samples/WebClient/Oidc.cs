namespace Samples.WebClient;

public static class Oidc
{
    public static void Configure(OpenIdConnectOptions options, IConfiguration configuration, string clientId, string suffix)
    {
        var authority = configuration["Authority"]!;
        var dpop = suffix.Length > 0;

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
        options.Scope.Add("offline_access");

        // Keeps access_token, refresh_token, id_token and token_type in the session cookie.
        options.SaveTokens = true;
        // The profile claims come in the identity token. The userinfo endpoint would also need a DPoP proof for a
        // DPoP-bound token.
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";

        options.Events.OnTicketReceived = context =>
        {
            context.Properties!.Items[Schemes.SchemeProperty] = context.Scheme.Name;
            return Task.CompletedTask;
        };

        if (dpop)
        {
            // Right before the code is redeemed: create the key of this session. The token request carries a proof
            // of it, and the key is kept, protected, in the session cookie to sign the proofs of the API calls.
            options.Events.OnAuthorizationCodeReceived = context =>
            {
                var key = DPoPKey.Create();
                context.Properties!.Items[DPoPKey.PropertyName] = key.ToJson();
                context.HttpContext.Items[DPoPKey.PropertyName] = key;
                return Task.CompletedTask;
            };
        }
    }
}
