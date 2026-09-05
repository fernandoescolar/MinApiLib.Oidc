namespace IdentityServer.UnitTests.Validation.AuthorizeRequest_Validation;

/// <summary>
/// The endpoints check the deprecated-flow flags on every request, also for clients that bypass configuration
/// validation (for example a custom client store).
/// </summary>
public class Authorize_DeprecatedFlows
{
    private const string Category = "AuthorizeRequest Validation - Deprecated flows";

    private static Client ImplicitClient(bool allowImplicitFlow, bool allowAccessTokensViaBrowser = false) => new()
    {
        ClientId = "implicit",
        AllowedGrantTypes = GrantTypes.Implicit,
        AllowImplicitFlow = allowImplicitFlow,
        AllowAccessTokensViaBrowser = allowAccessTokensViaBrowser,
        AllowedScopes = { "openid", "resource" },
        RedirectUris = { "https://server/cb" }
    };

    private static Task<AuthorizeRequestValidationResult> ValidateAsync(Client client, string responseType, string scope = "openid")
    {
        var parameters = new NameValueCollection
        {
            { OidcConstants.AuthorizeRequest.ClientId, client.ClientId },
            { OidcConstants.AuthorizeRequest.Scope, scope },
            { OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb" },
            { OidcConstants.AuthorizeRequest.ResponseType, responseType },
            { OidcConstants.AuthorizeRequest.Nonce, "nonce" }
        };

        var validator = Factory.CreateAuthorizeRequestValidator(clients: new InMemoryClientStore([client]));
        return validator.ValidateAsync(parameters);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_request_without_AllowImplicitFlow_is_unauthorized()
    {
        var result = await ValidateAsync(ImplicitClient(allowImplicitFlow: false), OidcConstants.ResponseTypes.IdToken);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_request_with_AllowImplicitFlow_is_valid()
    {
        var result = await ValidateAsync(ImplicitClient(allowImplicitFlow: true), OidcConstants.ResponseTypes.IdToken);

        Assert.False(result.IsError, result.ErrorDescription);
        Assert.Equal(GrantType.Implicit, result.ValidatedRequest.GrantType);
        Assert.Equal(OidcConstants.ResponseModes.Fragment, result.ValidatedRequest.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Token_response_type_without_AllowAccessTokensViaBrowser_is_unauthorized()
    {
        var result = await ValidateAsync(ImplicitClient(allowImplicitFlow: true), OidcConstants.ResponseTypes.IdTokenToken, "openid resource");

        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Token_response_type_with_AllowAccessTokensViaBrowser_is_valid()
    {
        var result = await ValidateAsync(ImplicitClient(allowImplicitFlow: true, allowAccessTokensViaBrowser: true), OidcConstants.ResponseTypes.IdTokenToken, "openid resource");

        Assert.False(result.IsError, result.ErrorDescription);
        Assert.True(result.ValidatedRequest.AccessTokenRequested);
    }
}
