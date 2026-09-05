namespace IdentityServer;

/// <summary>
/// Protocol values the model types need for their defaults. They mirror the public constants in
/// IdentityServer.IdentityServerConstants, which live in the IdentityServer assembly; a unit test keeps them in sync.
/// </summary>
internal static class ModelConstants
{
    public const string IdentityServerAuthenticationType = "IdentityServer";
    public const string OpenIdConnectProtocolType = "oidc";
    public const string SharedSecretType = "SharedSecret";
}
