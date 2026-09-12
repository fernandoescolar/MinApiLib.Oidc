namespace MinApiLib.OidcServer.Core.Hosting;

/// <summary>
/// Endpoint result. It is an ASP.NET Core <see cref="IResult"/>, so endpoint handlers' results can be returned
/// from minimal API and endpoint routing handlers as well.
/// </summary>
public interface IEndpointResult : IResult
{
    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    new Task ExecuteAsync(HttpContext context);
}
