using IdP;
using IdP.Features.Flow;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

var settings = builder.Configuration.GetSection(IdPOptions.Section).Get<IdPOptions>() ?? new IdPOptions();
builder.Services.Configure<IdPOptions>(builder.Configuration.GetSection(IdPOptions.Section));

var mongoConnection = builder.Configuration.GetConnectionString("Mongo");
if (string.IsNullOrWhiteSpace(mongoConnection))
{
    throw new InvalidOperationException("Set the MongoDB connection string in ConnectionStrings:Mongo (ConnectionStrings__Mongo).");
}

// MongoDB: one client, one database for the server's stores and the identity provider's collections.
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));
builder.Services.AddSingleton(sp => new IdPMongoContext(sp.GetRequiredService<IMongoClient>().GetDatabase(settings.Database)));

// Data protection keys in MongoDB: cookies, antiforgery and e-mail tokens survive restarts and are shared by every instance.
builder.Services.AddDataProtection().SetApplicationName("MinApiLib.Oidc.IdP");
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
    new ConfigureOptions<KeyManagementOptions>(options => options.XmlRepository = new MongoXmlRepository(sp.GetRequiredService<IdPMongoContext>().DataProtectionKeys)));

// The OpenID Connect server.
builder.Services.AddOidcServer(options =>
    {
        options.IssuerUri = string.IsNullOrWhiteSpace(settings.Issuer) ? null : settings.Issuer;
        options.KeyManagement.Enabled = true;
        options.Authentication.EnforceAcrValues = true;
        options.Authentication.CookieSameSiteMode = settings.CookieSameSite;
        options.Authentication.CheckSessionCookieSameSiteMode = settings.CookieSameSite;

        options.Events.RaiseSuccessEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;

        options.UserInteraction.LoginUrl = "/login";
        options.UserInteraction.LogoutUrl = "/logout";
        options.UserInteraction.ConsentUrl = "/consent";
        options.UserInteraction.ErrorUrl = "/error";
        options.UserInteraction.DeviceVerificationUrl = "/device";
    })
    .AddMongoStores(options => options.DatabaseName = settings.Database)
    .AddProfileService<IdPProfileService>();
builder.Services.AddTransient<IEventSink, MongoEventSink>();

// Access tokens of this server with the OidcServerApi scope can call the administration API too.
builder.Services.AddLocalApiAuthentication();

// Sessions end when the user is disabled or the security stamp changes.
builder.Services.PostConfigure<CookieAuthenticationOptions>(OidcServerConstants.DefaultCookieAuthenticationScheme,
    options => options.Events.OnValidatePrincipal = SecurityStampValidation.ValidateAsync);

// The users, with ASP.NET Core Identity over MongoDB: passwords, lockout, e-mail tokens, TOTP, recovery codes, passkeys.
builder.Services.AddIdentityCore<IdPUser>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.Password.RequiredLength = settings.Password.RequiredLength;
        options.Password.RequireDigit = settings.Password.RequireDigit;
        options.Password.RequireUppercase = settings.Password.RequireUppercase;
        options.Password.RequireLowercase = settings.Password.RequireLowercase;
        options.Password.RequireNonAlphanumeric = settings.Password.RequireNonAlphanumeric;
        options.Lockout.MaxFailedAccessAttempts = settings.Password.MaxFailedAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(settings.Password.LockoutMinutes);
        options.Lockout.AllowedForNewUsers = true;
        options.Tokens.AuthenticatorIssuer = settings.Name;
    })
    .AddUserStore<MongoUserStore>()
    .AddDefaultTokenProviders();
builder.Services.TryAddScoped<IPasskeyHandler<IdPUser>, PasskeyHandler<IdPUser>>();
builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    // Discoverable passkeys, so the user signs in without typing a user name.
    options.ResidentKeyRequirement = "required";
    options.UserVerificationRequirement = "preferred";
    options.ValidateOrigin = context => ValueTask.FromResult(
        !context.CrossOrigin &&
        string.Equals(context.Origin, $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}", StringComparison.OrdinalIgnoreCase));
});

// External OpenID Connect providers, registered from the database at run time.
builder.Services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectPostConfigureOptions>();
builder.Services.AddSingleton<DynamicSchemeManager>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DynamicSchemeManager>());

// E-mail.
if (string.IsNullOrWhiteSpace(settings.Email.Smtp.Host))
{
    builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}

builder.Services.AddSingleton<EmailTemplates>();

// The pages (React, ClientApp/) and the JSON APIs behind them.
builder.Services.AddAntiforgery(options => options.HeaderName = AntiforgeryModel.Header);
builder.Services.AddSingleton<SpaPages>();
builder.Services.AddSingleton<PasskeyState>();
builder.Services.AddSingleton<MfaState>();
builder.Services.AddScoped<LoginRenderer>();
builder.Services.AddScoped<AccountEmails>();
builder.Services.AddHostedService<Bootstrap>();
builder.Services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");

if (settings.ForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (settings.ForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseStaticFiles();
app.UseRouting();
app.UseOidcServer();
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapOidcServer();  // the protocol endpoints
app.MapEndpoints();   // the pages and the APIs (MinApiLib records in Features/)

app.Run();

/// <summary>Lets the tests host the application with <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
