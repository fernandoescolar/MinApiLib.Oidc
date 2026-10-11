namespace Samples.WebClient;

public static class Oidc
{
    /// <summary>The <c>acr</c> of a phishing-resistant sign-in, which the sample server meets with a passkey.</summary>
    public const string PhishingResistant = "phr";

    /// <summary>The authentication property with the <c>acr_values</c> to ask for.</summary>
    public const string AcrValuesProperty = "acr_values";

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
        // The handler drops acr from the identity token by default; the home page shows it.
        options.ClaimActions.Remove("acr");

        // acr_values, when the sign-in asks for it. AddOpenIdConnectTokens keeps this handler and runs its own after it.
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (context.Properties.Items.TryGetValue(AcrValuesProperty, out var acrValues))
            {
                context.ProtocolMessage.AcrValues = acrValues;
            }

            return Task.CompletedTask;
        };
    }
}
