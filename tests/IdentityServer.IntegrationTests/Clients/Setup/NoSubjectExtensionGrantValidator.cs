namespace IdentityServer.IntegrationTests.Clients.Setup;

public class NoSubjectExtensionGrantValidator : IExtensionGrantValidator
{
    public Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var credential = context.Request.Raw.Get("custom_credential");

        if (credential != null)
        {
            context.Result = new GrantValidationResult();
        }
        else
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid custom credential");
        }

        return Task.CompletedTask;
    }

    public string GrantType => "custom.nosubject";
}
