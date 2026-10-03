namespace MinApiLib.Oidc.Server.Features.Revocation;

/// <summary>
/// Result for revocation error
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Hosting.IEndpointResult" />
public class TokenRevocationErrorResult : IEndpointResult
{
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    /// <value>
    /// The error.
    /// </value>
    public string Error { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevocationErrorResult"/> class.
    /// </summary>
    /// <param name="error">The error.</param>
    public TokenRevocationErrorResult(string error)
    {
        Error = error;
    }

    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return context.Response.WriteJsonAsync(new { error = Error });
    }
}
