// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens.Grants;

/// <summary>
/// State of one token request validation, passed explicitly to the grant validators so that no validator keeps
/// per-request state in fields.
/// </summary>
internal sealed class TokenRequestValidationContext
{
    private readonly ILogger _logger;

    public TokenRequestValidationContext(ValidatedTokenRequest request, NameValueCollection parameters, OidcServerOptions options, ILogger logger)
    {
        Request = request;
        Parameters = parameters;
        Options = options;
        _logger = logger;
    }

    public ValidatedTokenRequest Request { get; }

    public NameValueCollection Parameters { get; }

    public OidcServerOptions Options { get; }

    public ILogger Logger => _logger;

    public TokenRequestValidationResult Valid(Dictionary<string, object>? customResponse = null)
    {
        return new TokenRequestValidationResult(Request, customResponse);
    }

    public TokenRequestValidationResult Invalid(string error, string? errorDescription = null, Dictionary<string, object>? customResponse = null)
    {
        return new TokenRequestValidationResult(Request, error, errorDescription, customResponse);
    }

    public void LogError(string? message = null, object? values = null)
    {
        LogWithRequestDetails(LogLevel.Error, message, values);
    }

    public void LogWarning(string? message = null, object? values = null)
    {
        LogWithRequestDetails(LogLevel.Warning, message, values);
    }

    public void LogSuccess()
    {
        LogWithRequestDetails(LogLevel.Information, "Token request validation success");
    }

    private void LogWithRequestDetails(LogLevel logLevel, string? message = null, object? values = null)
    {
        // Building the details serializes the scrubbed request; skip it when the level is disabled.
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }

        var details = new TokenRequestValidationLog(Request, Options.Logging.TokenRequestSensitiveValuesFilter);

        if (message.IsPresent())
        {
            try
            {
                if (values == null)
                {
                    RequestValidationLog.TokenRequestValidationMessage(_logger, logLevel, message, details);
                }
                else
                {
                    RequestValidationLog.TokenRequestValidationMessageWithValues(_logger, logLevel, message, values, details);
                }

            }
            catch (Exception ex)
            {
                RequestValidationLog.TokenRequestLoggingFailed(_logger, ex.Message, details);
            }
        }
        else
        {
            RequestValidationLog.TokenRequestValidationDetails(_logger, logLevel, details);
        }
    }
}
