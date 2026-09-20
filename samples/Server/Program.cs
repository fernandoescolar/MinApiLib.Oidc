using Samples.Server;

var builder = WebApplication.CreateBuilder(args);
var webClientUrl = builder.Configuration["WebClientUrl"] ?? "https://localhost:5002";

var oidc = builder.Services.AddOidcServer(options =>
{
    // Signing keys are created and rotated automatically: in MongoDB with the Mongo stores, in ./keys otherwise.
    options.KeyManagement.Enabled = true;
});

var mongo = builder.Configuration.GetConnectionString("Mongo");
if (string.IsNullOrWhiteSpace(mongo))
{
    oidc.AddInMemoryClients(Config.Clients(webClientUrl))
        .AddInMemoryIdentityResources(Config.IdentityResources)
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryApiResources(Config.ApiResources);
}
else
{
    oidc.AddMongoStores(options =>
    {
        options.ConnectionString = mongo;
        options.DatabaseName = "oidcserver-sample";
    });
    builder.Services.AddHostedService(sp => new MongoSeeder(sp.GetRequiredService<OidcServerMongoContext>(), webClientUrl));
}

builder.Services.AddSingleton<UserStore>();
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseRouting();
app.UseOidcServer();
app.UseAntiforgery();

app.MapOidcServer();  // the protocol endpoints
app.MapEndpoints();   // the login, logout, error and home pages of this sample (MinApiLib records in Pages/)

app.Run();
