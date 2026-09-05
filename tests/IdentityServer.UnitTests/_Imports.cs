
global using IdentityServer.IdentityModel;

global using IdentityServer.Configuration;
global using IdentityServer.Configuration.DependencyInjection;
global using IdentityServer.Endpoints;
global using IdentityServer.Endpoints.Results;
global using IdentityServer.Events;
global using IdentityServer.Extensions;
global using IdentityServer.Hosting;
global using IdentityServer.Models;
global using IdentityServer.ResponseHandling;
global using IdentityServer.Services;
global using IdentityServer.Stores;
global using IdentityServer.Stores.Serialization;
global using IdentityServer.Validation;
global using IdentityServer.Services.Default;
global using IdentityServer.UnitTests.Common;
global using IdentityServer.UnitTests.Services.Default;
global using IdentityServer.UnitTests.Validation.Setup;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.DataProtection;
global using Microsoft.AspNetCore.Cors.Infrastructure;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Primitives;
global using Microsoft.IdentityModel.Tokens;

global using System.Collections.Specialized;
global using System.IdentityModel.Tokens.Jwt;
global using System.Text.Json;
global using System.Net;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Security.Cryptography.X509Certificates;
global using System.Text;

global using Endpoint = IdentityServer.Hosting.Endpoint;
global using TokenValidationResult = IdentityServer.Validation.TokenValidationResult;
global using Secret = IdentityServer.Models.Secret;
global using AuthorizeResponse = IdentityServer.ResponseHandling.AuthorizeResponse;
global using ClaimValueTypes = System.Security.Claims.ClaimValueTypes;
global using GrantTypes = IdentityServer.Models.GrantTypes;

global using static IdentityServer.IdentityModel.OidcConstants;
global using static IdentityServer.Constants;
global using static IdentityServer.IdentityServerConstants;
global using Base64Url = System.Buffers.Text.Base64Url;
