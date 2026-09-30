namespace Samples.Server;

/// <summary>
/// The clients and resources of the sample.
/// </summary>
public static class Config
{
    public static IReadOnlyList<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email()
    ];

    public static IReadOnlyList<ApiScope> ApiScopes =>
    [
        new ApiScope("orders.read", "Read your orders")
    ];

    // The API resource gives the access token the audience "orders", which the API checks, and the user's name.
    public static IReadOnlyList<ApiResource> ApiResources =>
    [
        new ApiResource("orders", "Orders API") { Scopes = { "orders.read" }, UserClaims = { "name" } }
    ];

    public static IReadOnlyList<Client> Clients(string webClientUrl) =>
    [
        // The web client signing in with plain bearer tokens.
        WebClient("web", webClientUrl, suffix: string.Empty, requireDPoP: false),

        // The same web application with proof-of-possession: the token endpoint refuses requests without a DPoP
        // proof, and the access token is bound to the key of the proof (cnf.jkt).
        WebClient("web.dpop", webClientUrl, suffix: "-dpop", requireDPoP: true),

        // A background service with its own tokens (client credentials, no user), bearer and DPoP-bound.
        Worker("worker", requireDPoP: false),
        Worker("worker.dpop", requireDPoP: true)
    ];

    private static Client Worker(string clientId, bool requireDPoP) => new()
    {
        ClientId = clientId,
        ClientName = requireDPoP ? "Sample worker (DPoP)" : "Sample worker (Bearer)",
        ClientSecrets = { new Secret("worker-secret".Sha256()) },
        AllowedGrantTypes = GrantTypes.ClientCredentials,
        RequireDPoP = requireDPoP,
        AllowedScopes = { "orders.read" }
    };

    private static Client WebClient(string clientId, string url, string suffix, bool requireDPoP) => new()
    {
        ClientId = clientId,
        ClientName = requireDPoP ? "Sample web client (DPoP)" : "Sample web client (Bearer)",
        ClientSecrets = { new Secret("web-secret".Sha256()) },
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireConsent = false,
        RequireDPoP = requireDPoP,
        AllowOfflineAccess = true,
        AlwaysIncludeUserClaimsInIdToken = true,
        RedirectUris = { $"{url}/signin-oidc{suffix}" },
        PostLogoutRedirectUris = { $"{url}/signout-callback-oidc{suffix}" },
        FrontChannelLogoutUri = $"{url}/signout-oidc{suffix}",
        AllowedScopes = { "openid", "profile", "email", "orders.read" }
    };
}
