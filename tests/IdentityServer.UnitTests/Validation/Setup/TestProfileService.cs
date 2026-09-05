namespace IdentityServer.UnitTests.Validation.Setup;

internal class TestProfileService : IProfileService
{
    private bool _shouldBeActive;

    public TestProfileService(bool shouldBeActive = true)
    {
        _shouldBeActive = shouldBeActive;
    }

    public Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        return Task.CompletedTask;
    }

    public Task IsActiveAsync(IsActiveContext context)
    {
        context.IsActive = _shouldBeActive;
        return Task.CompletedTask;
    }
}
