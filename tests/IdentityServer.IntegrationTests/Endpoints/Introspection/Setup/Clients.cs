namespace IdentityServer.IntegrationTests.Endpoints.Introspection.Setup;

internal class Clients
{
    public static IEnumerable<Client> Get()
    {
        return new List<Client>
            {
                new Client
                {
                    ClientId = "client1",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "api1", "api2", "api3-a", "api3-b" },
                    AccessTokenType = AccessTokenType.Reference
                },
                new Client
                {
                    ClientId = "client2",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "api1", "api2", "api3-a", "api3-b" },
                    AccessTokenType = AccessTokenType.Reference
                },
                new Client
                {
                    ClientId = "client3",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "api1", "api2", "api3-a", "api3-b" },
                    AccessTokenType = AccessTokenType.Reference
                },

            };
    }
}
