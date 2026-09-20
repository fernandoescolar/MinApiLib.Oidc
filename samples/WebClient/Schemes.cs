namespace Samples.WebClient;

public static class Schemes
{
    public const string Cookie = "cookie";

    /// <summary>Signs in with the "web" client: bearer access tokens.</summary>
    public const string Bearer = "oidc";

    /// <summary>Signs in with the "web.dpop" client: DPoP-bound access tokens.</summary>
    public const string DPoP = "oidc-dpop";

    /// <summary>The authentication property that records which of the two signed the user in.</summary>
    public const string SchemeProperty = "sample.scheme";
}
