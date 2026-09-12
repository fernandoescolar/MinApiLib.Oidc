namespace MinApiLib.OidcServer.Core.Models;

/// <summary>
/// Protocol values the model types need for their defaults. They mirror the public constants in
/// MinApiLib.OidcServer.OidcServerConstants, which live in the OidcServer assembly; a unit test keeps them in sync.
/// </summary>
internal static class ModelConstants
{
    public const string OidcServerAuthenticationType = "OidcServer";
    public const string OpenIdConnectProtocolType = "oidc";
    public const string SharedSecretType = "SharedSecret";
}
