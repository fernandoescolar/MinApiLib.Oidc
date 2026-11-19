using System.Reflection;

namespace IdP.Features.Flow;

/// <summary>What every flow page's model carries, besides its own fields.</summary>
public static class FlowPages
{
    public static readonly string Version = typeof(FlowPages).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "dev";

    public static object Product(IdPOptions options) => new { name = options.Name, version = Version };

    /// <summary>A page with a title, a text and a link: "check your e-mail", "password changed", "device authorized"...</summary>
    public static IResult Message(SpaPages spa, IdPOptions options, string title, string text, string? linkHref = null, string? linkText = null, bool error = false)
        => spa.Render("message", title, new { title, text, linkHref, linkText, error, product = Product(options) });

    public static string LoginUrl(string? returnUrl) => returnUrl == null ? "/login" : "/login?returnUrl=" + Uri.EscapeDataString(returnUrl);

    public static IResult RedirectToLogin(string? returnUrl) => Results.Redirect(LoginUrl(returnUrl));
}

/// <summary>The root: the account console, or the login page.</summary>
public sealed record HomePage() : GetHandler<HomePage.Request>("/")
{
    public readonly record struct Request(HttpContext HttpContext);

    protected override IResult Handle(Request request) => Results.Redirect("/account");
}

/// <summary>Shows protocol errors that cannot be sent back to the client, such as an unknown redirect URI.</summary>
public sealed record ErrorPage() : GetHandlerAsync<ErrorPage.Request>("/error")
{
    public readonly record struct Request(
        [FromQuery] string? ErrorId,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var error = request.ErrorId == null ? null : await request.Interaction.GetErrorContextAsync(request.ErrorId);

        return request.Spa.Render("error", "Error", new
        {
            error = error?.Error,
            errorDescription = error?.ErrorDescription,
            requestId = error?.RequestId,
            product = FlowPages.Product(request.Options.Value)
        });
    }
}

/// <summary>
/// The account console (<c>/account/...</c>): a single-page application for the signed-in user. Its routes are
/// client-side, so every path renders the same page.
/// </summary>
public sealed record AccountConsole() : GetHandlerAsync<AccountConsole.Request>("/account/{**path}")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] IIssuerNameService Issuer,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return FlowPages.RedirectToLogin(http.Request.Path + http.Request.QueryString);
        }

        return request.Spa.Render("account", "My account", await ConsoleModel.ForAsync(http, request.Antiforgery, request.Options.Value, request.Issuer));
    }
}

/// <summary>The administration console (<c>/admin/...</c>), for users with the <c>admin</c> role.</summary>
public sealed record AdminConsole() : GetHandlerAsync<AdminConsole.Request>("/admin/{**path}")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] IIssuerNameService Issuer,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return FlowPages.RedirectToLogin(http.Request.Path + http.Request.QueryString);
        }

        if (!http.User.IsAdmin())
        {
            return FlowPages.Message(request.Spa, request.Options.Value, "Administrators only",
                "Your account does not have the admin role, which the administration console needs.", "/account", "Go to my account", error: true);
        }

        return request.Spa.Render("admin", "Administration", await ConsoleModel.ForAsync(http, request.Antiforgery, request.Options.Value, request.Issuer));
    }
}

public static class ConsoleModel
{
    public static async Task<object> ForAsync(HttpContext http, IAntiforgery antiforgery, IdPOptions options, IIssuerNameService issuer)
    {
        var user = http.User;
        return new
        {
            user = new
            {
                id = user.GetSubjectId(),
                userName = user.FindFirst(JwtClaimTypes.PreferredUserName)?.Value,
                name = user.FindFirst(JwtClaimTypes.Name)?.Value,
                email = user.FindFirst(JwtClaimTypes.Email)?.Value,
                isAdmin = user.IsAdmin(),
                idp = user.FindFirst(JwtClaimTypes.IdentityProvider)?.Value,
                amr = user.FindAll(JwtClaimTypes.AuthenticationMethod).Select(c => c.Value),
                acr = user.FindFirst(JwtClaimTypes.AuthenticationContextClassReference)?.Value
            },
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = new { name = options.Name, version = FlowPages.Version, issuer = await issuer.GetCurrentAsync(), registrationEnabled = options.Registration.Enabled }
        };
    }
}
