namespace MinApiLib.OidcServer.ResponseHandling;

/// <summary>
/// Default implementation of the discovery endpoint response generator
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.ResponseHandling.IDiscoveryResponseGenerator" />
[RegisterInIServiceCollection(typeof(IDiscoveryResponseGenerator))]
public class DiscoveryResponseGenerator : IDiscoveryResponseGenerator
{
    private static readonly string[] SubjectTypesSupported = ["public"];

    /// <summary>
    /// The options
    /// </summary>
    protected readonly OidcServerOptions Options;

    /// <summary>
    /// The extension grants validator
    /// </summary>
    protected readonly ExtensionGrantValidator ExtensionGrants;

    /// <summary>
    /// The key material service
    /// </summary>
    protected readonly IKeyMaterialService Keys;

    /// <summary>
    /// The resource store
    /// </summary>
    protected readonly IResourceStore ResourceStore;

    /// <summary>
    /// The secret parsers
    /// </summary>
    protected readonly ISecretsListParser SecretParsers;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryResponseGenerator"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="resourceStore">The resource store.</param>
    /// <param name="keys">The keys.</param>
    /// <param name="extensionGrants">The extension grants.</param>
    /// <param name="secretParsers">The secret parsers.</param>
    /// <param name="logger">The logger.</param>
    public DiscoveryResponseGenerator(
        OidcServerOptions options,
        IResourceStore resourceStore,
        IKeyMaterialService keys,
        ExtensionGrantValidator extensionGrants,
        ISecretsListParser secretParsers,
        ILogger<DiscoveryResponseGenerator> logger)
    {
        Options = options;
        ResourceStore = resourceStore;
        Keys = keys;
        ExtensionGrants = extensionGrants;
        SecretParsers = secretParsers;
        Logger = logger;
    }

    /// <summary>
    /// Creates the discovery document.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="issuerUri">The issuer URI.</param>
    public virtual async Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri)
    {
        var entries = new Dictionary<string, object>
        {
            { OidcConstants.Discovery.Issuer, issuerUri }
        };

        if (Options.Discovery.ShowKeySet)
        {
            if ((await Keys.GetValidationKeysAsync()).Any())
            {
                entries.Add(OidcConstants.Discovery.JwksUri, baseUrl + Constants.ProtocolRoutePaths.DiscoveryWebKeys);
            }
        }

        if (Options.Discovery.ShowEndpoints)
        {
            if (Options.Endpoints.EnableAuthorizeEndpoint)
            {
                entries.Add(OidcConstants.Discovery.AuthorizationEndpoint, baseUrl + Constants.ProtocolRoutePaths.Authorize);
            }

            if (Options.Endpoints.EnableTokenEndpoint)
            {
                entries.Add(OidcConstants.Discovery.TokenEndpoint, baseUrl + Constants.ProtocolRoutePaths.Token);
            }

            if (Options.Endpoints.EnableUserInfoEndpoint)
            {
                entries.Add(OidcConstants.Discovery.UserInfoEndpoint, baseUrl + Constants.ProtocolRoutePaths.UserInfo);
            }

            if (Options.Endpoints.EnableEndSessionEndpoint)
            {
                entries.Add(OidcConstants.Discovery.EndSessionEndpoint, baseUrl + Constants.ProtocolRoutePaths.EndSession);
            }

            if (Options.Endpoints.EnableCheckSessionEndpoint)
            {
                entries.Add(OidcConstants.Discovery.CheckSessionIframe, baseUrl + Constants.ProtocolRoutePaths.CheckSession);
            }

            if (Options.Endpoints.EnableTokenRevocationEndpoint)
            {
                entries.Add(OidcConstants.Discovery.RevocationEndpoint, baseUrl + Constants.ProtocolRoutePaths.Revocation);
            }

            if (Options.Endpoints.EnableIntrospectionEndpoint)
            {
                entries.Add(OidcConstants.Discovery.IntrospectionEndpoint, baseUrl + Constants.ProtocolRoutePaths.Introspection);
            }

            if (Options.Endpoints.EnableDeviceAuthorizationEndpoint)
            {
                entries.Add(OidcConstants.Discovery.DeviceAuthorizationEndpoint, baseUrl + Constants.ProtocolRoutePaths.DeviceAuthorization);
            }

            if (Options.Endpoints.EnableAuthorizeEndpoint && Options.Endpoints.EnablePushedAuthorizationEndpoint)
            {
                entries.Add(OidcConstants.Discovery.PushedAuthorizationRequestEndpoint, baseUrl + Constants.ProtocolRoutePaths.PushedAuthorization);
            }

            if (Options.Endpoints.EnableNonceEndpoint)
            {
                entries.Add(Constants.Discovery.NonceEndpoint, baseUrl + Constants.ProtocolRoutePaths.Nonce);
            }

            if (Options.MutualTls.Enabled)
            {
                var mtlsEndpoints = new Dictionary<string, string>();

                if (Options.Endpoints.EnableTokenEndpoint)
                {
                    mtlsEndpoints.Add(OidcConstants.Discovery.TokenEndpoint, ConstructMtlsEndpoint(Constants.ProtocolRoutePaths.Token));
                }
                if (Options.Endpoints.EnableTokenRevocationEndpoint)
                {
                    mtlsEndpoints.Add(OidcConstants.Discovery.RevocationEndpoint, ConstructMtlsEndpoint(Constants.ProtocolRoutePaths.Revocation));
                }
                if (Options.Endpoints.EnableIntrospectionEndpoint)
                {
                    mtlsEndpoints.Add(OidcConstants.Discovery.IntrospectionEndpoint, ConstructMtlsEndpoint(Constants.ProtocolRoutePaths.Introspection));
                }
                if (Options.Endpoints.EnableAuthorizeEndpoint && Options.Endpoints.EnablePushedAuthorizationEndpoint)
                {
                    mtlsEndpoints.Add(OidcConstants.Discovery.PushedAuthorizationRequestEndpoint, ConstructMtlsEndpoint(Constants.ProtocolRoutePaths.PushedAuthorization));
                }
                if (Options.Endpoints.EnableDeviceAuthorizationEndpoint)
                {
                    mtlsEndpoints.Add(OidcConstants.Discovery.DeviceAuthorizationEndpoint, ConstructMtlsEndpoint(Constants.ProtocolRoutePaths.DeviceAuthorization));
                }

                if (mtlsEndpoints.Count > 0)
                {
                    entries.Add(OidcConstants.Discovery.MtlsEndpointAliases, mtlsEndpoints);
                }

                string ConstructMtlsEndpoint(string endpoint)
                {
                    if (Options.MutualTls.DomainName.IsMissing())
                    {
                        return baseUrl + endpoint.Replace(Constants.ProtocolRoutePaths.ConnectPathPrefix, Constants.ProtocolRoutePaths.MtlsPathPrefix);
                    }

                    if (Options.MutualTls.DomainName.Contains('.'))
                    {
                        return $"https://{Options.MutualTls.DomainName}/{endpoint}";
                    }
                    else
                    {
                        var parts = baseUrl.Split("://");
                        return $"https://{Options.MutualTls.DomainName}.{parts[1]}{endpoint}";
                    }
                }
            }
        }

        if (Options.Endpoints.EnableEndSessionEndpoint)
        {
            entries.Add(OidcConstants.Discovery.FrontChannelLogoutSupported, true);
            entries.Add(OidcConstants.Discovery.FrontChannelLogoutSessionSupported, true);
            entries.Add(OidcConstants.Discovery.BackChannelLogoutSupported, true);
            entries.Add(OidcConstants.Discovery.BackChannelLogoutSessionSupported, true);
        }

        if (Options.Discovery.ShowIdentityScopes ||
            Options.Discovery.ShowApiScopes ||
            Options.Discovery.ShowClaims)
        {
            var resources = await ResourceStore.GetAllEnabledResourcesAsync();
            var scopes = new List<string>();

            if (Options.Discovery.ShowIdentityScopes)
            {
                scopes.AddRange(resources.IdentityResources.Where(x => x.ShowInDiscoveryDocument).Select(x => x.Name));
            }

            if (Options.Discovery.ShowApiScopes)
            {
                var apiScopes = from scope in resources.ApiScopes
                                where scope.ShowInDiscoveryDocument
                                select scope.Name;

                scopes.AddRange(apiScopes);
                scopes.Add(OidcServerConstants.StandardScopes.OfflineAccess);
            }

            if (scopes.Count > 0)
            {
                entries.Add(OidcConstants.Discovery.ScopesSupported, scopes.ToArray());
            }

            if (Options.Discovery.ShowClaims)
            {
                var claims = new List<string>();

                claims.AddRange(resources.IdentityResources.Where(x => x.ShowInDiscoveryDocument).SelectMany(x => x.UserClaims));
                claims.AddRange(resources.ApiResources.Where(x => x.ShowInDiscoveryDocument).SelectMany(x => x.UserClaims));
                claims.AddRange(resources.ApiScopes.Where(x => x.ShowInDiscoveryDocument).SelectMany(x => x.UserClaims));

                entries.Add(OidcConstants.Discovery.ClaimsSupported, claims.Distinct().ToArray());
            }
        }

        if (Options.Discovery.ShowGrantTypes)
        {
            var standardGrantTypes = new List<string>
            {
                OidcConstants.GrantTypes.AuthorizationCode,
                OidcConstants.GrantTypes.ClientCredentials,
                OidcConstants.GrantTypes.RefreshToken
            };

            if (Options.Endpoints.EnableDeviceAuthorizationEndpoint)
            {
                standardGrantTypes.Add(OidcConstants.GrantTypes.DeviceCode);
            }

            var showGrantTypes = new List<string>(standardGrantTypes);

            if (Options.Discovery.ShowExtensionGrantTypes)
            {
                showGrantTypes.AddRange(ExtensionGrants.GetAvailableGrantTypes());
            }

            entries.Add(OidcConstants.Discovery.GrantTypesSupported, showGrantTypes.ToArray());
        }

        if (Options.Discovery.ShowResponseTypes)
        {
            // The deprecated response types work only for clients that opt in, so they are not published.
            entries.Add(OidcConstants.Discovery.ResponseTypesSupported, Constants.SupportedResponseTypes.Where(t => !Constants.DeprecatedResponseTypes.Contains(t)).ToArray());
        }

        if (Options.Discovery.ShowResponseModes)
        {
            entries.Add(OidcConstants.Discovery.ResponseModesSupported, Constants.SupportedResponseModes.ToArray());
        }

        if (Options.Discovery.ShowTokenEndpointAuthenticationMethods)
        {
            var types = SecretParsers.GetAvailableAuthenticationMethods().ToList();
            if (Options.MutualTls.Enabled)
            {
                types.Add(OidcConstants.EndpointAuthenticationMethods.TlsClientAuth);
                types.Add(OidcConstants.EndpointAuthenticationMethods.SelfSignedTlsClientAuth);
            }

            entries.Add(OidcConstants.Discovery.TokenEndpointAuthenticationMethodsSupported, types);

            if (types.Contains(OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt))
            {
                entries.Add("token_endpoint_auth_signing_alg_values_supported", OidcServerConstants.SupportedSigningAlgorithms.ToArray());
            }
        }

        var signingCredentials = await Keys.GetAllSigningCredentialsAsync();
        if (signingCredentials.Any())
        {
            var signingAlgorithms = signingCredentials.Select(c => c.Algorithm).Distinct();
            entries.Add(OidcConstants.Discovery.IdTokenSigningAlgorithmsSupported, signingAlgorithms);
        }

        entries.Add(OidcConstants.Discovery.SubjectTypesSupported, SubjectTypesSupported);
        // Only S256 is advertised: plain is rejected unless a client explicitly allows it (OAuth 2.1).
        entries.Add(OidcConstants.Discovery.CodeChallengeMethodsSupported, new[] { OidcConstants.CodeChallengeMethods.Sha256 });

        if (Options.Endpoints.EnableAuthorizeEndpoint)
        {
            entries.Add(OidcConstants.Discovery.RequestParameterSupported, true);
            entries.Add(OidcConstants.Discovery.PromptValuesSupported, Options.UserInteraction.PromptValuesSupported.ToArray());
            entries.Add("request_object_signing_alg_values_supported", OidcServerConstants.SupportedSigningAlgorithms.ToArray());

            if (Options.EmitIssuerIdentificationResponseParameter)
            {
                entries.Add("authorization_response_iss_parameter_supported", true);
            }

            if (Options.Endpoints.EnableJwtRequestUri)
            {
                entries.Add(OidcConstants.Discovery.RequestUriParameterSupported, true);
            }

            if (Options.Endpoints.EnablePushedAuthorizationEndpoint && Options.PushedAuthorization.Required)
            {
                entries.Add(OidcConstants.Discovery.RequirePushedAuthorizationRequests, true);
            }
        }

        if (Options.Endpoints.EnableTokenEndpoint)
        {
            entries.Add(OidcConstants.Discovery.DPoPSigningAlgorithmsSupported, Options.DPoP.SupportedSigningAlgorithms.ToArray());
        }

        if (Options.MutualTls.Enabled)
        {
            entries.Add(OidcConstants.Discovery.TlsClientCertificateBoundAccessTokens, true);
        }

        if (Options.Discovery.CustomEntries != null && Options.Discovery.CustomEntries.Count > 0)
        {
            foreach ((string key, object value) in Options.Discovery.CustomEntries)
            {
#pragma warning disable CA1864 // The value added depends on the branch below, so TryAdd does not fit.
                if (entries.ContainsKey(key))
                {
                    ResponseAndHostingLog.DiscoveryCustomEntryExists(Logger, key);
                }
#pragma warning restore CA1864
                else
                {
                    if (value is string customValueString)
                    {
                        if (customValueString.StartsWith("~/", StringComparison.Ordinal) && Options.Discovery.ExpandRelativePathsInCustomEntries)
                        {
                            entries.Add(key, string.Concat(baseUrl, customValueString.AsSpan(2)));
                            continue;
                        }
                    }

                    entries.Add(key, value);
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// Creates the JWK document.
    /// </summary>
    public virtual async Task<IEnumerable<Models.JsonWebKey>> CreateJwkDocumentAsync()
    {
        var webKeys = new List<Models.JsonWebKey>();

        foreach (var key in await Keys.GetValidationKeysAsync())
        {
            if (key.Key is X509SecurityKey x509Key)
            {
                var cert64 = Convert.ToBase64String(x509Key.Certificate.RawData);
                var thumbprint = Base64Url.EncodeToString(x509Key.Certificate.GetCertHash());

                if (x509Key.PublicKey is RSA rsa)
                {
                    var parameters = rsa.ExportParameters(false);
                    var exponent = Base64Url.EncodeToString(parameters.Exponent);
                    var modulus = Base64Url.EncodeToString(parameters.Modulus);

                    var rsaJsonWebKey = new Models.JsonWebKey
                    {
                        kty = "RSA",
                        use = "sig",
                        kid = x509Key.KeyId,
                        x5t = thumbprint,
                        e = exponent,
                        n = modulus,
                        x5c = new[] { cert64 },
                        alg = key.SigningAlgorithm
                    };
                    webKeys.Add(rsaJsonWebKey);
                }
                else if (x509Key.PublicKey is ECDsa ecdsa)
                {
                    var parameters = ecdsa.ExportParameters(false);
                    var x = Base64Url.EncodeToString(parameters.Q.X);
                    var y = Base64Url.EncodeToString(parameters.Q.Y);

                    var ecdsaJsonWebKey = new Models.JsonWebKey
                    {
                        kty = "EC",
                        use = "sig",
                        kid = x509Key.KeyId,
                        x5t = thumbprint,
                        x = x,
                        y = y,
                        crv = CryptoHelper.GetCrvValueFromCurve(parameters.Curve),
                        x5c = new[] { cert64 },
                        alg = key.SigningAlgorithm
                    };
                    webKeys.Add(ecdsaJsonWebKey);
                }
                else
                {
                    throw new InvalidOperationException($"key type: {x509Key.PublicKey.GetType().Name} not supported.");
                }
            }
            else if (key.Key is RsaSecurityKey rsaKey)
            {
                var parameters = rsaKey.Rsa?.ExportParameters(false) ?? rsaKey.Parameters;
                var exponent = Base64Url.EncodeToString(parameters.Exponent);
                var modulus = Base64Url.EncodeToString(parameters.Modulus);

                var webKey = new Models.JsonWebKey
                {
                    kty = "RSA",
                    use = "sig",
                    kid = rsaKey.KeyId,
                    e = exponent,
                    n = modulus,
                    alg = key.SigningAlgorithm
                };

                webKeys.Add(webKey);
            }
            else if (key.Key is ECDsaSecurityKey ecdsaKey)
            {
                var parameters = ecdsaKey.ECDsa.ExportParameters(false);
                var x = Base64Url.EncodeToString(parameters.Q.X);
                var y = Base64Url.EncodeToString(parameters.Q.Y);

                var ecdsaJsonWebKey = new Models.JsonWebKey
                {
                    kty = "EC",
                    use = "sig",
                    kid = ecdsaKey.KeyId,
                    x = x,
                    y = y,
                    crv = CryptoHelper.GetCrvValueFromCurve(parameters.Curve),
                    alg = key.SigningAlgorithm
                };
                webKeys.Add(ecdsaJsonWebKey);
            }
            else if (key.Key is Microsoft.IdentityModel.Tokens.JsonWebKey jsonWebKey)
            {
                var webKey = new JsonWebKey
                {
                    kty = jsonWebKey.Kty,
                    use = jsonWebKey.Use ?? "sig",
                    kid = jsonWebKey.Kid,
                    x5t = jsonWebKey.X5t,
                    e = jsonWebKey.E,
                    n = jsonWebKey.N,
                    x5c = jsonWebKey.X5c is { Count: > 0 } x5cValues ? x5cValues.ToArray() : null,
                    alg = jsonWebKey.Alg,
                    crv = jsonWebKey.Crv,
                    x = jsonWebKey.X,
                    y = jsonWebKey.Y
                };

                webKeys.Add(webKey);
            }
        }

        return webKeys;
    }
}
