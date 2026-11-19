namespace IdP.Infrastructure;

/// <summary>
/// Authentication of the JSON APIs. A request with an <c>Authorization</c> header carries an access token of this
/// server with the <c>OidcServerApi</c> scope (the local API scheme); any other request uses the session cookie and,
/// when it changes something, must carry the antiforgery token in the <see cref="AntiforgeryModel.Header"/>
/// header. The administration API also needs the <c>admin</c> role.
/// </summary>
public static class ApiAuth
{
    public const string AdminRole = "admin";

    public static RouteHandlerBuilder RequireUser(this RouteHandlerBuilder builder) => builder.AddEndpointFilter(Filter(requireAdmin: false));

    public static RouteHandlerBuilder RequireAdmin(this RouteHandlerBuilder builder) => builder.AddEndpointFilter(Filter(requireAdmin: true));

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.HasClaim(JwtClaimTypes.Role, AdminRole);

    private static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> Filter(bool requireAdmin)
        => async (context, next) =>
        {
            var http = context.HttpContext;
            var result = await AuthenticateAsync(http);
            if (result is not { Succeeded: true, Principal: var principal })
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to use this API.");
            }

            if (requireAdmin && !principal.IsAdmin())
            {
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "This API needs the admin role.");
            }

            http.User = principal;
            var response = await next(context);

            // An account operation may have renewed the security stamp: keep this session.
            if (!http.Request.Headers.ContainsKey("Authorization"))
            {
                await SecurityStampValidation.RefreshAsync(http);
            }

            return response;
        };

    private static async Task<AuthenticateResult> AuthenticateAsync(HttpContext http)
    {
        if (http.Request.Headers.ContainsKey("Authorization"))
        {
            return await http.AuthenticateAsync(OidcServerConstants.LocalApi.AuthenticationScheme);
        }

        var cookie = await http.AuthenticateAsync();
        if (!cookie.Succeeded)
        {
            return cookie;
        }

        if (!HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && !HttpMethods.IsOptions(http.Request.Method))
        {
            // Cookie authentication is ambient, so a state-changing request needs the token of the page that made it.
            var antiforgery = http.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(http);
            }
            catch (AntiforgeryValidationException)
            {
                return AuthenticateResult.Fail("The antiforgery token is missing or invalid.");
            }
        }

        return cookie;
    }
}
