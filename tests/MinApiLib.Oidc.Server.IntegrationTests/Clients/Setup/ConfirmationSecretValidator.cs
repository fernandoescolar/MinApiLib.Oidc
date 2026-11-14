// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

public class ConfirmationSecretValidator : ISecretValidator
{
    public Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
    {
        if (secrets.Any())
        {
            if (secrets.First().Type == "confirmation.test")
            {
                var cnf = new Dictionary<string, string>
                    {
                        { "x5t#S256", "foo" }
                    };

                var result = new SecretValidationResult
                {
                    Success = true,
                    Confirmation = JsonSerializer.Serialize(cnf)
                };

                return Task.FromResult(result);
            }
        }

        return Task.FromResult(new SecretValidationResult { Success = false });
    }
}
