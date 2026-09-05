namespace MinApiLib.OidcServer.IntegrationTests.Clients.Setup;

internal class Clients
{
    public static IEnumerable<Client> Get()
    {
        return new List<Client>
            {
                new Client
                {
                    ClientId = "client",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowOfflineAccess = true,

                    AllowedScopes =
                    {
                        "api1", "api2", "other_api"
                    }
                },
                new Client
                {
                    ClientId = "client.cnf",
                    ClientSecrets =
                    {
                        new Secret
                        {
                            Type = "confirmation.test",
                            Description = "Test for cnf claim",
                            Value = "foo"
                        }
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowOfflineAccess = true,

                    AllowedScopes =
                    {
                        "api1", "api2"
                    }
                },
                new Client
                {
                    ClientId = "client.and.ro",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes =
                    {
                        "openid",
                        "api1", "api2"
                    }
                },
                new Client
                {
                    ClientId = "client.identityscopes",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes =
                    {
                        "openid", "profile",
                        "api1", "api2"
                    }
                },
                new Client
                {
                    ClientId = "client.no_default_scopes",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials
                },
                new Client
                {
                    ClientId = "client.no_secret",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    RequireClientSecret = false,
                    AllowedScopes = { "api1" }
                },
                new Client
                {
                    ClientId = "client.custom",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = { "custom", "custom.nosubject" },

                    AllowedScopes =
                    {
                        "api1", "api2"
                    },

                    AllowOfflineAccess = true
                },
                new Client
                {
                    ClientId = "client.dynamic",
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = { "dynamic" },

                    AllowedScopes =
                    {
                        "api1", "api2"
                    },

                    AlwaysSendClientClaims = true
                },

                new Client
                {
                    ClientName = "Client with Base64 encoded X509 Certificate",
                    ClientId = "certificate_base64_valid",
                    Enabled = true,

                    ClientSecrets =
                    {
                        new Secret
                        {
                            Type = OidcServerConstants.SecretTypes.X509CertificateBase64,
                            Value = Convert.ToBase64String(TestCert.Load().Export(X509ContentType.Cert))
                        }
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes = new List<string>
                    {
                        "api1", "api2"
                    }
                },

                new Client
                {
                    ClientId = "code_only",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    AllowedScopes = {"api1"},
                    RedirectUris = { "http://code_only" }
                },

                // Clients used by RefreshTokenClient integration tests.
                // Bootstrap token obtained via the 'custom' extension grant.
                new Client
                {
                    ClientId = "client.refreshtoken",
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = { "custom" },
                    AllowedScopes = { "openid", "api1" },
                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Absolute
                },
                new Client
                {
                    ClientId = "client.refreshtoken.reuse",
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = { "custom" },
                    AllowedScopes = { "openid", "api1" },
                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Absolute
                },

            };
    }
}
