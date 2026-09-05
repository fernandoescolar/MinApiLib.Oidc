namespace MinApiLib.OidcServer.IntegrationTests.Clients.Setup;

public class TestCustomTokenRequestValidator : ICustomTokenRequestValidator
{
    public Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
        context.Result.CustomResponse = new Dictionary<string, object>
            {
                {"custom", "custom" }
            };

        return Task.CompletedTask;
    }
}
