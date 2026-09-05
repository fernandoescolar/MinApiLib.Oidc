namespace IdentityServer.UnitTests.Common;

public class StubHandleGenerationService : DefaultHandleGenerationService, IHandleGenerationService
{
    public string Handle { get; set; }

    public new Task<string> GenerateAsync(int length)
    {
        if (Handle != null) return Task.FromResult(Handle);
        return base.GenerateAsync(length);
    }
}
