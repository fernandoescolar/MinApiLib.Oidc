// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients;

public class ClientAssertionClient : IDisposable
{
    private const string TokenEndpoint = "https://idsvr4/connect/token";
    private const string ClientId = "certificate_base64_valid";

    private readonly HttpClient _client;
    private readonly IHost _host;

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    public ClientAssertionClient()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseStartup<Startup>())
            .Start();
        var server = _host.GetTestServer();

        _client = server.CreateClient();
    }

    [Fact]
    public async Task Valid_client_with_manual_payload_should_succeed()
    {
        var token = CreateToken(ClientId);
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ClientId },
                    { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                    { "client_assertion", token },
                    { "grant_type", "client_credentials" },
                    { "scope", "api1" }
                });

        var response = await GetToken(requestBody);

        AssertValidToken(response);
    }

    [Fact]
    public async Task Valid_client_should_succeed()
    {
        var token = CreateToken(ClientId);

        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = token
                },

            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        AssertValidToken(response);
    }

    [Fact]
    public async Task Valid_client_with_no_secret_should_succeed()
    {
        var token = CreateToken(ClientId);

        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = token
                },

            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        AssertValidToken(response);
    }

    [Fact]
    public async Task Valid_client_with_token_replay_should_fail()
    {
        var token = CreateToken(ClientId);

        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,

            ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = token
                },
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        AssertValidToken(response);

        response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,

            ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = token
                },

            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal("invalid_client", response.Error);
    }

    [Fact]
    public async Task Client_with_invalid_secret_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = "invalid"
                },

            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, response.Error);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
    }

    [Fact]
    public async Task Invalid_client_should_fail()
    {
        const string clientId = "certificate_base64_invalid";
        var token = CreateToken(clientId);

        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientAssertion =
                {
                    Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                    Value = token
                },

            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, response.Error);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
    }

    private async Task<TokenResponse> GetToken(FormUrlEncodedContent body)
    {
        var response = await _client.PostAsync(TokenEndpoint, body);
        return await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(response);
    }

    private void AssertValidToken(TokenResponse response)
    {
        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(8, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal(ClientId, payload["client_id"].ToString());
        Assert.True(payload.ContainsKey("iat"));

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        Assert.Equal("api", payload["aud"].ToString());
    }

    private JsonObject GetPayload(TokenResponse response)
    {
        var token = response.AccessToken.Split('.').Skip(1).Take(1).First();
        var json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        return JsonNode.Parse(json).AsObject();
    }

    private string CreateToken(string clientId, DateTime? nowOverride = null)
    {
        var certificate = TestCert.Load();
        var now = nowOverride ?? DateTime.UtcNow;

        var token = new JwtSecurityToken(
                clientId,
                TokenEndpoint,
                new List<Claim>()
                {
                        new Claim("jti", Guid.NewGuid().ToString()),
                        new Claim(JwtClaimTypes.Subject, clientId),
                        new Claim(JwtClaimTypes.IssuedAt, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                },
                now,
                now.AddMinutes(1),
                new SigningCredentials(
                    new X509SecurityKey(certificate),
                    SecurityAlgorithms.RsaSha256
                )
            );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
