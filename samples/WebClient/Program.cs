using Samples.WebClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(Api.HttpClientName, client => client.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!));

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

// The DPoP client signs its token request with the key of the session (see DPoPTokenRequestHandler).
builder.Services.AddOptions<OpenIdConnectOptions>(Schemes.DPoP)
    .Configure<IHttpContextAccessor>((options, accessor) => options.BackchannelHttpHandler = DPoPTokenRequestHandler.Create(accessor));

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();

app.MapEndpoints();

app.Run();
