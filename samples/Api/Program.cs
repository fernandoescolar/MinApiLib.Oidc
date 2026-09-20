var builder = WebApplication.CreateBuilder(args);
var authority = builder.Configuration["Authority"]!;

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DPoPProofValidator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The access token is a JWT signed by the server: validate it against its discovery document and keys.
        options.Authority = authority;
        options.RequireHttpsMetadata = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        options.Audience = "orders";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidTypes = ["at+jwt"];

        // Accepts "Authorization: DPoP <token>" and checks the DPoP proof of DPoP-bound tokens.
        options.Events = new DPoPJwtBearerEvents();
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders.read", policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "orders.read"));

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
