using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MinApiLib.OidcServer.Client.ClientCredentials;

using Samples.Worker;

// A background service that calls the orders API with its own access token (client credentials, no user): once with
// a bearer token, once with a DPoP-bound token.
var builder = Host.CreateApplicationBuilder(args);
var authority = builder.Configuration["Authority"]!;
var apiUrl = new Uri(builder.Configuration["ApiUrl"]!);

foreach (var (name, clientId, useDPoP) in new[] { ("bearer", "worker", false), ("dpop", "worker.dpop", true) })
{
    // The client: MinApiLib.OidcServer.Client finds the token endpoint in the discovery document, asks for a token,
    // caches it until shortly before it expires and, with UseDPoP, creates a key and binds the token to it.
    builder.Services.AddClientCredentialsTokens(name, options =>
    {
        options.Authority = authority;
        options.ClientId = clientId;
        options.ClientSecret = "worker-secret";
        options.Scope = "orders.read";
        options.UseDPoP = useDPoP;
    });

    // The API client: each request carries the token, and for DPoP a new proof. The printer shows the requests.
    builder.Services.AddHttpClient(name, client => client.BaseAddress = apiUrl)
        .AddClientCredentialsTokenHandler(name)
        .AddHttpMessageHandler(() => new ConsolePrinter());
}

using var host = builder.Build();
var clients = host.Services.GetRequiredService<IHttpClientFactory>();
var tokens = host.Services.GetRequiredService<IClientCredentialsTokenManager>();

foreach (var name in new[] { "bearer", "dpop" })
{
    var token = await tokens.GetAccessTokenAsync(name);
    Console.WriteLine($"=== {name}: the token endpoint answered token_type {token.TokenType}" +
        (token.DPoPKey == null ? string.Empty : $", bound to the key {token.DPoPKey.Thumbprint}"));

    // Two calls: the same token, a new proof each time.
    var http = clients.CreateClient(name);
    for (var i = 0; i < 2; i++)
    {
        using var response = await http.GetAsync("/orders");
    }
}
