// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Parses a POST body for a JWT bearer client assertion
/// </summary>
public class JwtBearerClientAssertionSecretParser : ISecretParser
{
    private readonly OidcServerOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtBearerClientAssertionSecretParser"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    public JwtBearerClientAssertionSecretParser(OidcServerOptions options, ILogger<JwtBearerClientAssertionSecretParser> logger)
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
    public string AuthenticationMethod => OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt;

    /// <summary>
    /// Tries to find a JWT client assertion token in the request body that can be used for authentication
    /// Used for "private_key_jwt" client authentication method as defined in http://openid.net/specs/openid-connect-core-1_0.html#ClientAuthentication
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>
    /// A parsed secret
    /// </returns>
    public async Task<ParsedSecret?> ParseAsync(HttpContext context)
    {
        SecretLog.StartParsingJwtClientAssertion(_logger);

        if (!context.Request.HasApplicationFormContentType())
        {
            SecretLog.ContentTypeNotForm(_logger);
            return null;
        }

        var body = await context.Request.ReadFormAsync();

        if (body != null)
        {
            var clientAssertionType = body[OidcConstants.TokenRequest.ClientAssertionType].FirstOrDefault();
            var clientAssertion = body[OidcConstants.TokenRequest.ClientAssertion].FirstOrDefault();

            if (clientAssertion.IsPresent()
                && clientAssertionType == OidcConstants.ClientAssertionTypes.JwtBearer)
            {
                if (clientAssertion.Length > _options.InputLengthRestrictions.Jwt)
                {
                    SecretLog.ClientAssertionTooLong(_logger);
                    return null;
                }

                var clientId = GetClientIdFromToken(clientAssertion);
                if (!clientId.IsPresent())
                {
                    return null;
                }

                if (clientId.Length > _options.InputLengthRestrictions.ClientId)
                {
                    SecretLog.ClientIdTooLong(_logger);
                    return null;
                }

                var parsedSecret = new ParsedSecret
                {
                    Id = clientId,
                    Credential = clientAssertion,
                    Type = OidcServerConstants.ParsedSecretTypes.JwtBearer
                };

                return parsedSecret;
            }
        }

        SecretLog.NoJwtClientAssertionFound(_logger);
        return null;
    }

    private string? GetClientIdFromToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityToken(token);
            return jwt.Subject;
        }
        catch (Exception e)
        {
            SecretLog.CouldNotParseClientAssertion(_logger, e);
            return null;
        }
    }
}
