namespace IdentityServer.Hosting;

/// <summary>
/// Runs an endpoint handler and its result for an endpoint mapped by <c>MapIdentityServer()</c>.
/// </summary>
internal static class EndpointHandlerExecutor
{
    /// <summary>
    /// Runs the handler and executes its result.
    /// </summary>
    public static async Task ExecuteAsync(HttpContext context, IEndpointHandler handler, ILogger logger)
    {
        try
        {
            ResponseAndHostingLog.InvokingEndpoint(logger, handler.GetType().FullName!, context.Request.Path.ToString());

            var result = await handler.ProcessAsync(context);
            if (result != null)
            {
                ResponseAndHostingLog.InvokingResult(logger, result.GetType().FullName!);
                await result.ExecuteAsync(context);
            }
        }
        catch (Exception ex)
        {
            await context.RequestServices.GetRequiredService<IEventService>().RaiseAsync(new UnhandledExceptionEvent(ex));
            ResponseAndHostingLog.UnhandledException(logger, ex, ex.Message);
            throw;
        }
    }
}
