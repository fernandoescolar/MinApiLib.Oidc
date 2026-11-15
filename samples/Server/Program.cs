using Microsoft.Extensions.DependencyInjection.Extensions;
using Samples.Server;

var builder = WebApplication.CreateBuilder(args);
var webClientUrl = builder.Configuration["WebClientUrl"] ?? "https://localhost:5002";

var oidc = builder.Services.AddOidcServer(options =>
{
    // Signing keys are created and rotated automatically: in MongoDB with the Mongo stores, in ./keys otherwise.
    options.KeyManagement.Enabled = true;

    // A client that asks for acr_values=phr gets a passkey sign-in, even when the user already has a session with a
    // password: the server sends the user back to the login page, and fails the request if the sign-in still is not
    // phishing-resistant.
    options.Authentication.EnforceAcrValues = true;
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

// The users, with ASP.NET Core Identity: password hashes and passkeys. The passkey handler makes the WebAuthn options
// and checks the browser's answers; the login page signs the user in to the server's cookie (Login.cs).
builder.Services.AddIdentityCore<SampleUser>()
    .AddUserStore<SampleUserStore>();
builder.Services.TryAddScoped<IPasskeyHandler<SampleUser>, PasskeyHandler<SampleUser>>();
builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    // Discoverable passkeys, so the user signs in without typing a user name.
    options.ResidentKeyRequirement = "required";
    options.UserVerificationRequirement = "preferred";

    // The browser must be on this server's origin, port included: https://localhost:5001 in the sample. The default
    // check rejects that origin here, so the sample states the rule.
    options.ValidateOrigin = context => ValueTask.FromResult(
        !context.CrossOrigin &&
        string.Equals(context.Origin, $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}", StringComparison.OrdinalIgnoreCase));
});
builder.Services.AddSingleton<SampleUserDirectory>();
builder.Services.AddSingleton<PasskeyState>();
builder.Services.AddAntiforgery();

// The pages are a React frontend (ClientApp/, built into wwwroot/): each endpoint renders its page and model.
builder.Services.AddSingleton(sp => new SpaPages(sp.GetRequiredService<IWebHostEnvironment>(), "Sample OIDC server"));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseOidcServer();
app.UseAntiforgery();

app.MapOidcServer();  // the protocol endpoints
app.MapEndpoints();   // the login, passkeys, logout, error and home pages of this sample (MinApiLib records in Pages/)

app.Run();
