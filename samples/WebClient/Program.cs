using Samples.WebClient;

var builder = WebApplication.CreateBuilder(args);
var apiUrl = new Uri(builder.Configuration["ApiUrl"]!);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = Schemes.Cookie;
        options.DefaultChallengeScheme = Schemes.Bearer;
    })
    .AddCookie(Schemes.Cookie, options => options.Cookie.Name = "sample-web")
    // Two OpenID Connect registrations of the same server, one per client: "web" gets bearer tokens and
    // "web.dpop" gets DPoP-bound tokens.
    .AddOpenIdConnect(Schemes.Bearer, options => Oidc.Configure(options, builder.Configuration, "web", suffix: string.Empty))
    .AddOpenIdConnect(Schemes.DPoP, options => Oidc.Configure(options, builder.Configuration, "web.dpop", suffix: "-dpop"));

// MinApiLib.Oidc.Authentication keeps the user's tokens in the session and refreshes them. For the DPoP scheme, it
// creates a key for each sign-in, binds the authorization code to it (dpop_jkt) and adds a proof of it to the code
// redemption, the refreshes and every API call.
builder.Services.AddOpenIdConnectTokens(Schemes.Bearer);
builder.Services.AddOpenIdConnectTokens(Schemes.DPoP, options => options.UseDPoP = true);

// The API client: sends the user's token, as a bearer token or with a DPoP proof. The recorder, after it, sees the
// request as it goes out, so the page can show it.
builder.Services.AddHttpClient(Api.HttpClientName, client => client.BaseAddress = apiUrl)
    .AddUserAccessTokenHandler(Schemes.Cookie)
    .AddHttpMessageHandler(services => new ExchangeRecorder(services.GetRequiredService<IHttpContextAccessor>()));

// The same API without the token handler, for the buttons that play the attacker.
builder.Services.AddHttpClient(Api.RawHttpClientName, client => client.BaseAddress = apiUrl)
    .AddHttpMessageHandler(services => new ExchangeRecorder(services.GetRequiredService<IHttpContextAccessor>()));

// The pages are a React frontend (ClientApp/, built into wwwroot/): each endpoint renders its page and model.
builder.Services.AddSingleton(sp => new SpaPages(sp.GetRequiredService<IWebHostEnvironment>(), "Sample web client"));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

app.MapEndpoints();

app.Run();
