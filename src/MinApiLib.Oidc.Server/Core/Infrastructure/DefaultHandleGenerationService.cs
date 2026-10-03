namespace MinApiLib.Oidc.Server.Core.Infrastructure;

/// <summary>
/// Default handle generation service
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Infrastructure.IHandleGenerationService" />
[RegisterInIServiceCollection(typeof(IHandleGenerationService))]
public class DefaultHandleGenerationService : IHandleGenerationService
{
    /// <summary>
    /// Generates a handle.
    /// </summary>
    /// <param name="length">The length.</param>
    /// <returns></returns>
    public Task<string> GenerateAsync(int length)
    {
        return Task.FromResult(CryptoRandom.CreateUniqueId(length, CryptoRandom.OutputFormat.Hex));
    }
}
