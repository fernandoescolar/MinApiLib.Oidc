// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.CheckSession;

public class CheckSessionTests : IDisposable
{
    private const string Category = "Check session endpoint";

    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public CheckSessionTests()
    {
        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_request_should_not_return_404()
    {
        var response = await _mockPipeline.BackChannelClient.GetAsync(OidcServerPipeline.CheckSessionEndpoint, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
