// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.DeviceAuthorization;

public class DeviceAuthorizationTests : IDisposable
{
    private const string Category = "Device authorization endpoint";

    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public DeviceAuthorizationTests()
    {
        _mockPipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.DeviceFlow,
            AllowedScopes = { "openid" }
        });

        _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId()
            });

        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_should_return_MethodNotAllowed()
    {
        // The endpoint is mapped for POST only (RFC 8628 section 3.1), so routing rejects other methods.
        var response = await _mockPipeline.BackChannelClient.GetAsync(OidcServerPipeline.DeviceAuthorization, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(["POST"], response.Content.Headers.Allow);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task wrong_content_type_return_InvalidRequest()
    {
        var _ = new Dictionary<string, string>
            {
                {"client_id", Guid.NewGuid().ToString()}
            };
        var response = await _mockPipeline.BackChannelClient.PostAsync(OidcServerPipeline.DeviceAuthorization, new StringContent(@"{""client_id"": ""client1""}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var resultDto = ParseJsonBody<ErrorResultDto>(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(resultDto);
        Assert.Equal(OidcConstants.TokenErrors.InvalidRequest, resultDto.error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task empty_request_should_return_InvalidClient()
    {
        var response = await _mockPipeline.BackChannelClient.PostAsync(OidcServerPipeline.DeviceAuthorization, new FormUrlEncodedContent(new Dictionary<string, string>()), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var resultDto = ParseJsonBody<ErrorResultDto>(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(resultDto);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, resultDto.error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task unknown_client_should_return_InvalidClient()
    {
        var form = new Dictionary<string, string>
            {
                {"client_id", "client1"}
            };
        var response = await _mockPipeline.BackChannelClient.PostAsync(OidcServerPipeline.DeviceAuthorization, new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var resultDto = ParseJsonBody<ErrorResultDto>(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(resultDto);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, resultDto.error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_should_return_json()
    {
        var form = new Dictionary<string, string>
            {
                {"client_id", "client1"},
                {"client_secret", "secret" }
            };
        var response = await _mockPipeline.BackChannelClient.PostAsync(OidcServerPipeline.DeviceAuthorization, new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

        var resultDto = ParseJsonBody<ResultDto>(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(resultDto);

        Assert.NotNull(resultDto);
        Assert.NotNull(resultDto.device_code);
        Assert.NotNull(resultDto.user_code);
        Assert.NotNull(resultDto.verification_uri);
        Assert.NotNull(resultDto.verification_uri_complete);
        Assert.True(resultDto.expires_in > 0);
        Assert.True(resultDto.interval > 0);
    }

    private T ParseJsonBody<T>(Stream streamBody)
    {
        streamBody.Position = 0;
        using (var reader = new StreamReader(streamBody))
        {
            var jsonString = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(jsonString);
        }
    }

    internal class ResultDto
    {
        public string device_code { get; set; }
        public string user_code { get; set; }
        public string verification_uri { get; set; }
        public string verification_uri_complete { get; set; }
        public int expires_in { get; set; }
        public int interval { get; set; }
    }

    internal class ErrorResultDto
    {
        public string error { get; set; }
        public string error_description { get; set; }
    }
}
