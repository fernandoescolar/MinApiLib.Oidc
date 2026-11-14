// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens;

internal static partial class LoggerExtensions
{
    [LoggerMessage(0x2F40, LogLevel.Trace, "Processing token request.")]
    public static partial void ProcessingTokenRequest(this ILogger logger);

    [LoggerMessage(0x2F41, LogLevel.Warning, "Invalid content type for token endpoint")]
    public static partial void InvalidHttpRequestForTokenEndpoint(this ILogger logger);

    [LoggerMessage(0x2F42, LogLevel.Debug, "Start token request.")]
    public static partial void StartTokenRequest(this ILogger logger);

    [LoggerMessage(0x2F43, LogLevel.Trace, "Calling into token request validator: {type}")]
    public static partial void CallingIntoTokenRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F44, LogLevel.Trace, "Calling into token request response generator: {type}")]
    public static partial void CallingIntoTokenRequestResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F45, LogLevel.Debug, "Token request success.")]
    public static partial void TokenRequestSuccess(this ILogger logger);

    [LoggerMessage(0x2F46, LogLevel.Trace, "Refresh token issued for {clientId} / {subjectId}: {token}")]
    public static partial void RefreshTokenIssued(this ILogger logger, string clientId, string subjectId, string token);
}
