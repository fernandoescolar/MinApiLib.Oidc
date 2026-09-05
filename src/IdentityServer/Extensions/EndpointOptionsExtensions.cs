namespace IdentityServer.Extensions;

internal static class EndpointOptionsExtensions
{
    public static bool IsEndpointEnabled(this EndpointsOptions options, Endpoint endpoint)
    {
        return endpoint?.Name switch
        {
            EndpointNames.Authorize => options.EnableAuthorizeEndpoint,
            EndpointNames.CheckSession => options.EnableCheckSessionEndpoint,
            EndpointNames.DeviceAuthorization => options.EnableDeviceAuthorizationEndpoint,
            EndpointNames.Discovery => options.EnableDiscoveryEndpoint,
            EndpointNames.EndSession => options.EnableEndSessionEndpoint,
            EndpointNames.Introspection => options.EnableIntrospectionEndpoint,
            EndpointNames.Revocation => options.EnableTokenRevocationEndpoint,
            EndpointNames.Token => options.EnableTokenEndpoint,
            EndpointNames.UserInfo => options.EnableUserInfoEndpoint,
            EndpointNames.Nonce => options.EnableNonceEndpoint,
            EndpointNames.PushedAuthorization => options.EnablePushedAuthorizationEndpoint,
            _ => true
        };
    }
}
