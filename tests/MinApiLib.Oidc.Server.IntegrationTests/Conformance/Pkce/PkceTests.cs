// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Conformance.Pkce;

public class PkceTests : IDisposable
{
    private const string Category = "PKCE";

    private OidcServerPipeline _pipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    private Client client;

    private const string client_id = "code_client";
    private const string client_id_optional = "code_client_optional";
    private const string client_id_plain = "code_plain_client";
    private const string client_id_pkce = "codewithproofkey_client";
    private const string client_id_pkce_plain = "codewithproofkey_plain_client";

    private string redirect_uri = "https://code_client/callback";
    private string code_verifier = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private string client_secret = "secret";
    private string response_type = "code";

    public PkceTests()
    {
        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Claims = new Claim[]
            {
                        new Claim("name", "Bob Loblaw"),
                        new Claim("email", "bob@loblaw.com"),
                        new Claim("role", "Attorney")
            }
        });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());

        _pipeline.Clients.Add(client = new Client
        {
            Enabled = true,
            ClientId = client_id,
            ClientSecrets = new List<Secret>
                {
                    new Secret(client_secret.Sha256())
                },

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,

            AllowedScopes = { "openid" },

            RequireConsent = false,
            RedirectUris = new List<string>
                {
                    redirect_uri
                }
        });
        _pipeline.Clients.Add(client = new Client
        {
            Enabled = true,
            ClientId = client_id_optional,
            ClientSecrets = new List<Secret>
                {
                    new Secret(client_secret.Sha256())
                },

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = false,

            AllowedScopes = { "openid" },

            RequireConsent = false,
            RedirectUris = new List<string>
                {
                    redirect_uri
                }
        });
        _pipeline.Clients.Add(client = new Client
        {
            Enabled = true,
            ClientId = client_id_pkce,
            ClientSecrets = new List<Secret>
                {
                    new Secret(client_secret.Sha256())
                },

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,

            AllowedScopes = { "openid" },

            RequireConsent = false,
            RedirectUris = new List<string>
                {
                    redirect_uri
                }
        });

        _pipeline.Clients.Add(client = new Client
        {
            Enabled = true,
            ClientId = client_id_plain,
            ClientSecrets = new List<Secret>
                {
                    new Secret(client_secret.Sha256())
                },

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            AllowPlainTextPkce = true,

            AllowedScopes = { "openid" },

            RequireConsent = false,
            RedirectUris = new List<string>
                {
                    redirect_uri
                }
        });
        _pipeline.Clients.Add(client = new Client
        {
            Enabled = true,
            ClientId = client_id_pkce_plain,
            ClientSecrets = new List<Secret>
                {
                    new Secret(client_secret.Sha256())
                },

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            AllowPlainTextPkce = true,

            AllowedScopes = { "openid" },

            RequireConsent = false,
            RedirectUris = new List<string>
                {
                    redirect_uri
                }
        });

        _pipeline.Initialize();
    }

    [Theory]
    [InlineData(client_id)]
    [InlineData(client_id_pkce)]
    [Trait("Category", Category)]
    public async Task Client_cannot_use_plain_code_challenge_method(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var _ = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Plain);

        Assert.True(_pipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _pipeline.ErrorMessage.Error);
    }

    [Theory]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Client_can_use_plain_code_challenge_method(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Plain);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
            CodeVerifier = code_verifier
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResponse.IsError);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.NotNull(tokenResponse.AccessToken);
        Assert.NotNull(tokenResponse.IdentityToken);
        Assert.True(tokenResponse.ExpiresIn > 0);
    }

    [Theory]
    [InlineData(client_id)]
    [InlineData(client_id_pkce)]
    [Trait("Category", Category)]
    public async Task Client_can_use_sha256_code_challenge_method(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = Sha256OfCodeVerifier(code_verifier);
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Sha256);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
            CodeVerifier = code_verifier
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResponse.IsError);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.NotNull(tokenResponse.AccessToken);
        Assert.NotNull(tokenResponse.IdentityToken);
        Assert.True(tokenResponse.ExpiresIn > 0);
    }

    [Theory]
    [InlineData(client_id_pkce)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Authorize_request_needs_code_challenge(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce);

        Assert.Null(authorizeResponse);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_verifier_should_not_be_accepted_if_no_code_challenge_was_used()
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(client_id_optional,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = client_id_optional,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
            CodeVerifier = code_verifier
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(tokenResponse.IsError);
    }

    [Theory]
    [InlineData(client_id)]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Authorize_request_code_challenge_cannot_be_too_short(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var _ = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: "a");

        Assert.True(_pipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _pipeline.ErrorMessage.Error);
    }

    [Theory]
    [InlineData(client_id)]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Authorize_request_code_challenge_cannot_be_too_long(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var _ = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: new string('a', _pipeline.Options.InputLengthRestrictions.CodeChallengeMaxLength + 1)
        );

        Assert.True(_pipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _pipeline.ErrorMessage.Error);
    }

    [Theory]
    [InlineData(client_id)]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Authorize_request_needs_supported_code_challenge_method(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: "unknown_code_challenge_method"
        );

        Assert.Null(authorizeResponse);
    }

    [Theory]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Token_request_needs_code_verifier(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Plain);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(tokenResponse.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, tokenResponse.Error);
    }

    [Theory]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Token_request_code_verifier_cannot_be_too_short(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Plain);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
            CodeVerifier = "a"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(tokenResponse.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, tokenResponse.Error);
    }

    [Theory]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Token_request_code_verifier_cannot_be_too_long(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Plain);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
            CodeVerifier = new string('a', _pipeline.Options.InputLengthRestrictions.CodeVerifierMaxLength + 1)
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(tokenResponse.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, tokenResponse.Error);
    }

    [Theory]
    [InlineData(client_id_plain)]
    [InlineData(client_id_pkce_plain)]
    [Trait("Category", Category)]
    public async Task Token_request_code_verifier_must_match_with_code_chalenge(string clientId)
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var code_challenge = code_verifier;
        var authorizeResponse = await _pipeline.RequestAuthorizationEndpointAsync(clientId,
            response_type,
            OidcServerConstants.StandardScopes.OpenId,
            redirect_uri,
            nonce: nonce,
            codeChallenge: code_challenge,
            codeChallengeMethod: OidcConstants.CodeChallengeMethods.Plain);

        Assert.False(authorizeResponse.IsError);

        var code = authorizeResponse.Code;

        var tokenResponse = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = client_secret,

            Code = code,
            RedirectUri = redirect_uri,
            CodeVerifier = "mismatched_code_verifier"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(tokenResponse.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, tokenResponse.Error);
    }

    private static string Sha256OfCodeVerifier(string codeVerifier)
    {
        var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hashedBytes = codeVerifierBytes.Sha256();
        var transformedCodeVerifier = Base64Url.EncodeToString(hashedBytes);

        return transformedCodeVerifier;
    }
}
