namespace IdentityServer.IntegrationTests.Clients.Setup;

public class ExtensionGrantValidator2 : IExtensionGrantValidator
{
    public Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var credential = context.Request.Raw.Get("custom_credential");

        if (credential != null)
        {
            context.Result = new GrantValidationResult("818727", "custom");
        }
        else
        {
            context.Result = new GrantValidationResult(IdentityServer.Models.TokenRequestErrors.InvalidGrant, "invalid custom credential");
        }

        return Task.CompletedTask;
    }

    public string GrantType => "custom2";
}
