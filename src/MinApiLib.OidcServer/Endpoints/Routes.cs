namespace MinApiLib.OidcServer.Endpoints;

// The routes of the protocol endpoints. MinApiLib maps every non-abstract endpoint record of the assembly, so each
// route is a record: the endpoint's own path and, for the connect/ endpoints, its connect/mtls/ alias used for mutual
// TLS client authentication (it answers 404 unless mutual TLS uses the path mode). The HTTP methods are those of the
// endpoint record: routing answers 405 to any other.

internal sealed record AuthorizeGetRoute() : AuthorizeGetEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Authorize));
internal sealed record AuthorizeGetMutualTlsRoute() : AuthorizeGetEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Authorize));
internal sealed record AuthorizePostRoute() : AuthorizePostEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Authorize));
internal sealed record AuthorizePostMutualTlsRoute() : AuthorizePostEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Authorize));

internal sealed record AuthorizeCallbackRoute() : AuthorizeCallbackEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.AuthorizeCallback));
internal sealed record AuthorizeCallbackMutualTlsRoute() : AuthorizeCallbackEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.AuthorizeCallback));

internal sealed record CheckSessionRoute() : CheckSessionEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.CheckSession));
internal sealed record CheckSessionMutualTlsRoute() : CheckSessionEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.CheckSession));

internal sealed record DeviceAuthorizationRoute() : DeviceAuthorizationEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.DeviceAuthorization));
internal sealed record DeviceAuthorizationMutualTlsRoute() : DeviceAuthorizationEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.DeviceAuthorization));

internal sealed record DiscoveryRoute() : DiscoveryEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.DiscoveryConfiguration));

internal sealed record DiscoveryKeyRoute() : DiscoveryKeyEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.DiscoveryWebKeys));

internal sealed record EndSessionGetRoute() : EndSessionGetEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.EndSession));
internal sealed record EndSessionGetMutualTlsRoute() : EndSessionGetEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.EndSession));
internal sealed record EndSessionPostRoute() : EndSessionPostEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.EndSession));
internal sealed record EndSessionPostMutualTlsRoute() : EndSessionPostEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.EndSession));

internal sealed record EndSessionCallbackRoute() : EndSessionCallbackEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.EndSessionCallback));
internal sealed record EndSessionCallbackMutualTlsRoute() : EndSessionCallbackEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.EndSessionCallback));

internal sealed record IntrospectionRoute() : IntrospectionEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Introspection));
internal sealed record IntrospectionMutualTlsRoute() : IntrospectionEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Introspection));

internal sealed record NonceRoute() : NonceEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Nonce));
internal sealed record NonceMutualTlsRoute() : NonceEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Nonce));

internal sealed record PushedAuthorizationRoute() : PushedAuthorizationEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.PushedAuthorization));
internal sealed record PushedAuthorizationMutualTlsRoute() : PushedAuthorizationEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.PushedAuthorization));

internal sealed record TokenRoute() : TokenEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Token));
internal sealed record TokenMutualTlsRoute() : TokenEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Token));

internal sealed record TokenRevocationRoute() : TokenRevocationEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Revocation));
internal sealed record TokenRevocationMutualTlsRoute() : TokenRevocationEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Revocation));

internal sealed record UserInfoRoute() : UserInfoEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.UserInfo));
internal sealed record UserInfoMutualTlsRoute() : UserInfoEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.UserInfo));
