// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Session;

internal static partial class LoggerExtensions
{
    [LoggerMessage(0x1F01, LogLevel.Debug, "Rendering check session result")]
    public static partial void RenderingCheckSessionResult(this ILogger logger);

    [LoggerMessage(0x2F19, LogLevel.Debug, "Processing signout callback request")]
    public static partial void ProcessingSignoutCallbackRequest(this ILogger logger);

    [LoggerMessage(0x2F20, LogLevel.Information, "Successful signout callback.")]
    public static partial void SuccessfulSignoutCallback(this ILogger logger);

    [LoggerMessage(0x2F21, LogLevel.Error, "Error validating signout callback: {error}")]
    public static partial void ErrorValidatingSignoutCallback(this ILogger logger, string error);

    [LoggerMessage(0x2F23, LogLevel.Debug, "Processing signout request for {subjectId}")]
    public static partial void ProcessingSignoutRequest(this ILogger logger, string subjectId);

    [LoggerMessage(0x2F24, LogLevel.Error, "Error processing end session request {error}")]
    public static partial void ErrorProcessingEndSessionRequest(this ILogger logger, string error);

    [LoggerMessage(0x2F25, LogLevel.Debug, "Success validating end session request from {clientId}")]
    public static partial void SuccessValidatingEndSessionRequest(this ILogger logger, string? clientId);
}
