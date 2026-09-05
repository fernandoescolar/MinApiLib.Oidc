namespace IdentityServer.UnitTests.Models;

public class ModelDefaultsTests
{
    [Fact]
    public void Client_protocol_type_defaults_to_openid_connect()
    {
        Assert.Equal(IdentityServerConstants.ProtocolTypes.OpenIdConnect, new Client().ProtocolType);
    }

    [Fact]
    public void Secret_type_defaults_to_shared_secret()
    {
        Assert.Equal(IdentityServerConstants.SecretTypes.SharedSecret, new Secret().Type);
    }

    [Fact]
    public void IdentityServerUser_principal_uses_identityserver_authentication_type()
    {
        Assert.Equal(Constants.IdentityServerAuthenticationType, new IdentityServerUser("123").CreatePrincipal().Identity.AuthenticationType);
    }
}
