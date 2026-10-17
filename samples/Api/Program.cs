var builder = WebApplication.CreateBuilder(args);
var authority = builder.Configuration["Authority"]!;

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
        options.Challenge = "Bearer realm=\"orders\"";
    })
    // MinApiLib.Oidc.JwtBearer: accepts "Authorization: DPoP <token>", checks the proof of DPoP-bound tokens against
    // cnf.jkt, rejects them as bearer tokens and announces both schemes. options.Mode = DPoPTokenMode.Required would
    // accept DPoP-bound tokens only, and options.RequireNonce = true would make clients put a nonce of the API in
    // their proofs.
    .AddDPoP();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders.read", policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "orders.read"));

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
