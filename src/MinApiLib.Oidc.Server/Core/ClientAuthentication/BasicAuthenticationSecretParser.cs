// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Parses a Basic Authentication header
/// </summary>
[RegisterInIServiceCollection(typeof(ISecretParser))]
public class BasicAuthenticationSecretParser : ISecretParser
{
    private readonly ILogger _logger;
    private readonly OidcServerOptions _options;

    /// <summary>
    /// Creates the parser with a reference to identity server options
    /// </summary>
    /// <param name="options">OidcServer options</param>
    /// <param name="logger">The logger</param>
    public BasicAuthenticationSecretParser(OidcServerOptions options, ILogger<BasicAuthenticationSecretParser> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Returns the authentication method name that this parser implements
    /// </summary>
    /// <value>
    /// The authentication method.
    /// </value>
    public string AuthenticationMethod => OidcConstants.EndpointAuthenticationMethods.BasicAuthentication;

    /// <summary>
    /// Tries to find a secret that can be used for authentication
    /// </summary>
    /// <returns>
    /// A parsed secret
    /// </returns>
    public Task<ParsedSecret?> ParseAsync(HttpContext context)
    {
        SecretLog.StartParsingBasicAuthentication(_logger);

        var notfound = Task.FromResult<ParsedSecret?>(null);
        var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (authorizationHeader.IsMissing())
        {
            return notfound;
        }

        if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return notfound;
        }

        var parameter = authorizationHeader.Substring("Basic ".Length);

        string pair;
        try
        {
            pair = Encoding.UTF8.GetString(
                Convert.FromBase64String(parameter));
        }
        catch (FormatException)
        {
            SecretLog.MalformedBasicAuthenticationCredential(_logger);
            return notfound;
        }
        catch (ArgumentException)
        {
            SecretLog.MalformedBasicAuthenticationCredential(_logger);
            return notfound;
        }

        var ix = pair.IndexOf(':');
        if (ix == -1)
        {
            SecretLog.MalformedBasicAuthenticationCredential(_logger);
            return notfound;
        }

        var clientId = pair.Substring(0, ix);
        var secret = pair.Substring(ix + 1);

        if (clientId.IsPresent())
        {
            if (clientId.Length > _options.InputLengthRestrictions.ClientId)
            {
                SecretLog.ClientIdTooLong(_logger);
                return notfound;
            }

            if (secret.IsPresent())
            {
                if (secret.Length > _options.InputLengthRestrictions.ClientSecret)
                {
                    SecretLog.ClientSecretTooLong(_logger);
                    return notfound;
                }

                var parsedSecret = new ParsedSecret
                {
                    Id = Decode(clientId),
                    Credential = Decode(secret),
                    Type = OidcServerConstants.ParsedSecretTypes.SharedSecret
                };

                return Task.FromResult<ParsedSecret?>(parsedSecret);
            }
            else
            {
                SecretLog.ClientIdWithoutSecretFound(_logger);

                var parsedSecret = new ParsedSecret
                {
                    Id = Decode(clientId),
                    Type = OidcServerConstants.ParsedSecretTypes.NoSecret
                };

                return Task.FromResult<ParsedSecret?>(parsedSecret);
            }
        }

        SecretLog.NoBasicAuthenticationSecretFound(_logger);
        return notfound;
    }

    private string Decode(string value)
    {
        if (value.IsMissing()) return string.Empty;

        return Uri.UnescapeDataString(value.Replace("+", "%20"));
    }
}
