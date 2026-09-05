namespace MinApiLib.OidcServer;

internal static class Constants
{
    public const string OidcServerName = "OidcServer";
    public const string OidcServerAuthenticationType = OidcServerName;
    public const string ExternalAuthenticationMethod = "external";
    public const string DefaultHashAlgorithm = "SHA256";

    public static readonly TimeSpan DefaultCookieTimeSpan = TimeSpan.FromHours(10);
    public static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);

    public static readonly CompositeFormat AccessTokenAudienceFormat = CompositeFormat.Parse(OidcServerConstants.AccessTokenAudience);

    public static readonly string[] SupportedResponseTypes =
    {
        OidcConstants.ResponseTypes.Code,
        OidcConstants.ResponseTypes.Token,
        OidcConstants.ResponseTypes.IdToken,
        OidcConstants.ResponseTypes.IdTokenToken,
        OidcConstants.ResponseTypes.CodeIdToken,
        OidcConstants.ResponseTypes.CodeToken,
        OidcConstants.ResponseTypes.CodeIdTokenToken
    };

    /// <summary>
    /// Response types of the deprecated implicit flow and of the flows that return access tokens from the
    /// authorize endpoint. They need a client flag and are not published in discovery.
    /// </summary>
    public static readonly FrozenSet<string> DeprecatedResponseTypes = new[]
    {
        OidcConstants.ResponseTypes.Token,
        OidcConstants.ResponseTypes.IdToken,
        OidcConstants.ResponseTypes.IdTokenToken,
        OidcConstants.ResponseTypes.CodeToken,
        OidcConstants.ResponseTypes.CodeIdTokenToken
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenDictionary<string, string> ResponseTypeToGrantTypeMapping = new Dictionary<string, string>
    {
        { OidcConstants.ResponseTypes.Code, GrantType.AuthorizationCode },
        { OidcConstants.ResponseTypes.Token, GrantType.Implicit },
        { OidcConstants.ResponseTypes.IdToken, GrantType.Implicit },
        { OidcConstants.ResponseTypes.IdTokenToken, GrantType.Implicit },
        { OidcConstants.ResponseTypes.CodeIdToken, GrantType.Hybrid },
        { OidcConstants.ResponseTypes.CodeToken, GrantType.Hybrid },
        { OidcConstants.ResponseTypes.CodeIdTokenToken, GrantType.Hybrid }
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static readonly FrozenSet<string> AllowedGrantTypesForAuthorizeEndpoint = new[]
    {
        GrantType.AuthorizationCode,
        GrantType.Implicit,
        GrantType.Hybrid
    }.ToFrozenSet(StringComparer.Ordinal);

    public static readonly FrozenSet<string> SupportedCodeChallengeMethods = new[]
    {
        OidcConstants.CodeChallengeMethods.Plain,
        OidcConstants.CodeChallengeMethods.Sha256
    }.ToFrozenSet(StringComparer.Ordinal);

    public enum ScopeRequirement
    {
        None,
        ResourceOnly,
        IdentityOnly,
        Identity
    }

    public static readonly FrozenDictionary<string, ScopeRequirement> ResponseTypeToScopeRequirement = new Dictionary<string, ScopeRequirement>
    {
        { OidcConstants.ResponseTypes.Code, ScopeRequirement.None },
        { OidcConstants.ResponseTypes.Token, ScopeRequirement.ResourceOnly },
        { OidcConstants.ResponseTypes.IdToken, ScopeRequirement.IdentityOnly },
        { OidcConstants.ResponseTypes.IdTokenToken, ScopeRequirement.Identity },
        { OidcConstants.ResponseTypes.CodeIdToken, ScopeRequirement.Identity },
        { OidcConstants.ResponseTypes.CodeToken, ScopeRequirement.Identity },
        { OidcConstants.ResponseTypes.CodeIdTokenToken, ScopeRequirement.Identity }
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static readonly FrozenDictionary<string, IEnumerable<string>> AllowedResponseModesForGrantType = new Dictionary<string, IEnumerable<string>>
    {
        { GrantType.AuthorizationCode, new[] { OidcConstants.ResponseModes.Query, OidcConstants.ResponseModes.FormPost, OidcConstants.ResponseModes.Fragment } },
        { GrantType.Hybrid, new[] { OidcConstants.ResponseModes.Fragment, OidcConstants.ResponseModes.FormPost }},
        { GrantType.Implicit, new[] { OidcConstants.ResponseModes.Fragment, OidcConstants.ResponseModes.FormPost }}
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static readonly string[] SupportedResponseModes =
    {
        OidcConstants.ResponseModes.FormPost,
        OidcConstants.ResponseModes.Query,
        OidcConstants.ResponseModes.Fragment
    };

    public static class SigningAlgorithms
    {
        public const string RSA_SHA_256 = "RS256";
    }

    public static readonly FrozenSet<string> SupportedDisplayModes = new[]
    {
        OidcConstants.DisplayModes.Page,
        OidcConstants.DisplayModes.Popup,
        OidcConstants.DisplayModes.Touch,
        OidcConstants.DisplayModes.Wap
    }.ToFrozenSet(StringComparer.Ordinal);

    public static class KnownAcrValues
    {
        public const string HomeRealm = "idp:";
        public const string Tenant = "tenant:";

        public static readonly string[] All = { HomeRealm, Tenant };
    }

    public static readonly FrozenDictionary<string, int> ProtectedResourceErrorStatusCodes = new Dictionary<string, int>
    {
        { OidcConstants.ProtectedResourceErrors.InvalidToken,      401 },
        { OidcConstants.ProtectedResourceErrors.ExpiredToken,      401 },
        { OidcConstants.ProtectedResourceErrors.InvalidRequest,    400 },
        { OidcConstants.ProtectedResourceErrors.InsufficientScope, 403 }
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static readonly FrozenDictionary<string, IEnumerable<string>> ScopeToClaimsMapping = new Dictionary<string, IEnumerable<string>>
    {
        { OidcServerConstants.StandardScopes.Profile, new[]
                            {
                                JwtClaimTypes.Name,
                                JwtClaimTypes.FamilyName,
                                JwtClaimTypes.GivenName,
                                JwtClaimTypes.MiddleName,
                                JwtClaimTypes.NickName,
                                JwtClaimTypes.PreferredUserName,
                                JwtClaimTypes.Profile,
                                JwtClaimTypes.Picture,
                                JwtClaimTypes.WebSite,
                                JwtClaimTypes.Gender,
                                JwtClaimTypes.BirthDate,
                                JwtClaimTypes.ZoneInfo,
                                JwtClaimTypes.Locale,
                                JwtClaimTypes.UpdatedAt
                            }},
        { OidcServerConstants.StandardScopes.Email, new[]
                            {
                                JwtClaimTypes.Email,
                                JwtClaimTypes.EmailVerified
                            }},
        { OidcServerConstants.StandardScopes.Address, new[]
                            {
                                JwtClaimTypes.Address
                            }},
        { OidcServerConstants.StandardScopes.Phone, new[]
                            {
                                JwtClaimTypes.PhoneNumber,
                                JwtClaimTypes.PhoneNumberVerified
                            }},
        { OidcServerConstants.StandardScopes.OpenId, new[]
                            {
                                JwtClaimTypes.Subject
                            }}
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static class UIConstants
    {
        public const int CookieMessageThreshold = 2;

        public static class DefaultRoutePathParams
        {
            public const string Error = "errorId";
            public const string Login = "returnUrl";
            public const string Consent = "returnUrl";
            public const string Logout = "logoutId";
            public const string EndSessionCallback = "endSessionId";
            public const string Custom = "returnUrl";
            public const string UserCode = "userCode";
        }

        public static class DefaultRoutePaths
        {
            public const string Login = "/account/login";
            public const string Logout = "/account/logout";
            public const string Consent = "/consent";
            public const string Error = "/home/error";
            public const string DeviceVerification = "/device";
        }
    }


    public static class ProtocolRoutePaths
    {
        public const string ConnectPathPrefix = "connect";

        public const string Authorize = ConnectPathPrefix + "/authorize";
        public const string AuthorizeCallback = Authorize + "/callback";
        public const string DiscoveryConfiguration = ".well-known/openid-configuration";
        public const string DiscoveryWebKeys = DiscoveryConfiguration + "/jwks";
        public const string Token = ConnectPathPrefix + "/token";
        public const string Revocation = ConnectPathPrefix + "/revocation";
        public const string UserInfo = ConnectPathPrefix + "/userinfo";
        public const string Introspection = ConnectPathPrefix + "/introspect";
        public const string EndSession = ConnectPathPrefix + "/endsession";
        public const string Nonce = ConnectPathPrefix + "/nonce";
        public const string EndSessionCallback = EndSession + "/callback";
        public const string CheckSession = ConnectPathPrefix + "/checksession";
        public const string DeviceAuthorization = ConnectPathPrefix + "/deviceauthorization";
        public const string PushedAuthorization = ConnectPathPrefix + "/par";

        public const string MtlsPathPrefix = ConnectPathPrefix + "/mtls";
        public const string MtlsToken = MtlsPathPrefix + "/token";
        public const string MtlsRevocation = MtlsPathPrefix + "/revocation";
        public const string MtlsIntrospection = MtlsPathPrefix + "/introspect";
        public const string MtlsDeviceAuthorization = MtlsPathPrefix + "/deviceauthorization";

        public static readonly string[] CorsPaths =
            {
                DiscoveryConfiguration,
                DiscoveryWebKeys,
                Token,
                UserInfo,
                Revocation,
                PushedAuthorization
            };
    }

    public static class PushedAuthorization
    {
        /// <summary>
        /// Prefix of the request_uri returned by the pushed authorization request endpoint (RFC 9126 section 2.2).
        /// </summary>
        public const string RequestUriPrefix = "urn:ietf:params:oauth:request_uri:";
    }

    public static class Discovery
    {
        public const string NonceEndpoint = "nonce_endpoint";
    }

    public static class EnvironmentKeys
    {
        public const string OidcServerBasePath = "idsvr:OidcServerBasePath";

        public const string SignOutCalled = "idsvr:OidcServerSignOutCalled";

        // Set by MutualTlsEndpointMiddleware once the client certificate authentication scheme succeeded.
        public const string MutualTlsClientCertificateAuthenticated = "idsvr:MutualTlsClientCertificateAuthenticated";
    }

    public static class TokenTypeHints
    {
        public const string RefreshToken = "refresh_token";
        public const string AccessToken = "access_token";
    }

    public static readonly FrozenSet<string> SupportedTokenTypeHints = new[]
    {
        TokenTypeHints.RefreshToken,
        TokenTypeHints.AccessToken
    }.ToFrozenSet(StringComparer.Ordinal);

    public static class RevocationErrors
    {
        public const string UnsupportedTokenType = "unsupported_token_type";
    }

    public class Filters
    {
        public static readonly FrozenSet<string> ProtocolClaimsFilter = new[]
    {
                JwtClaimTypes.AccessTokenHash,
                JwtClaimTypes.Audience,
                JwtClaimTypes.AuthorizedParty,
                JwtClaimTypes.AuthorizationCodeHash,
                JwtClaimTypes.ClientId,
                JwtClaimTypes.Expiration,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Issuer,
                JwtClaimTypes.JwtId,
                JwtClaimTypes.Nonce,
                JwtClaimTypes.NotBefore,
                JwtClaimTypes.ReferenceTokenId,
                JwtClaimTypes.SessionId,
                JwtClaimTypes.Scope
            }.ToFrozenSet(StringComparer.Ordinal);

        public static readonly FrozenSet<string> ClaimsServiceFilterClaimTypes = new[]
    {
                JwtClaimTypes.AccessTokenHash,
                JwtClaimTypes.Audience,
                JwtClaimTypes.AuthenticationMethod,
                JwtClaimTypes.AuthenticationTime,
                JwtClaimTypes.AuthorizedParty,
                JwtClaimTypes.AuthorizationCodeHash,
                JwtClaimTypes.ClientId,
                JwtClaimTypes.Expiration,
                JwtClaimTypes.IdentityProvider,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Issuer,
                JwtClaimTypes.JwtId,
                JwtClaimTypes.Nonce,
                JwtClaimTypes.NotBefore,
                JwtClaimTypes.ReferenceTokenId,
                JwtClaimTypes.SessionId,
                JwtClaimTypes.Subject,
                JwtClaimTypes.Scope,
                JwtClaimTypes.Confirmation
            }.ToFrozenSet(StringComparer.Ordinal);

        public static readonly FrozenSet<string> JwtRequestClaimTypesFilter = new[]
    {
                JwtClaimTypes.Audience,
                JwtClaimTypes.Expiration,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Issuer,
                JwtClaimTypes.NotBefore,
                JwtClaimTypes.JwtId
            }.ToFrozenSet(StringComparer.Ordinal);
    }

    public static class WsFedSignOut
    {
        public const string LogoutUriParameterName = "wa";
        public const string LogoutUriParameterValue = "wsignoutcleanup1.0";
    }

    public static class AuthorizationParamsStore
    {
        public const string MessageStoreIdParameterName = "authzId";
    }

    public static class CurveOids
    {
        public const string P256 = "1.2.840.10045.3.1.7";
        public const string P384 = "1.3.132.0.34";
        public const string P521 = "1.3.132.0.35";
    }
}
