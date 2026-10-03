global using MinApiLib.Oidc.Server.Core.IdentityModel;
global using MinApiLib.Oidc.Server.Core.IdentityModel.X509Certificates;
global using MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

global using MinApiLib.Oidc.Server.Core.Configuration;
global using MinApiLib.Oidc.Server.Core.Cors;
global using MinApiLib.Oidc.Server.Core.DPoP;
global using MinApiLib.Oidc.Server.Core.Events;
global using MinApiLib.Oidc.Server.Core.Hosting;
global using MinApiLib.Oidc.Server.Core.Keys;
global using MinApiLib.Oidc.Server.Core.Logging;
global using MinApiLib.Oidc.Server.Core.TokenServices;
global using MinApiLib.Oidc.Server.Features.DeviceFlow;
global using MinApiLib.Oidc.Server.Features.Discovery;
global using MinApiLib.Oidc.Server.Features.Interaction;
global using MinApiLib.Oidc.Server.Features.PushedAuthorization;
global using MinApiLib.Oidc.Server.IntegrationTests.Common;
global using MinApiLib.Oidc.Server.Core.ClientAuthentication;
global using MinApiLib.Oidc.Server.Core.Clients;
global using MinApiLib.Oidc.Server.Core.Models;
global using MinApiLib.Oidc.Server.Core.ResourceValidation;
global using MinApiLib.Oidc.Server.Core.Users;
global using MinApiLib.Oidc.Server.Features.Authorize;
global using MinApiLib.Oidc.Server.Features.Nonce;
global using MinApiLib.Oidc.Server.Features.Session;
global using MinApiLib.Oidc.Server.Features.Tokens;
global using MinApiLib.Oidc.Server.Core.Infrastructure;
global using MinApiLib.Oidc.Server.Core.Stores;
global using MinApiLib.Oidc.Server.Test;
global using MinApiLib.Oidc.Server.Core.Validation;
global using MinApiLib.Oidc.Server.Features.Introspection;
global using MinApiLib.Oidc.Server.Features.Revocation;
global using MinApiLib.Oidc.Server.Features.Tokens.Grants;
global using MinApiLib.Oidc.Server.Features.UserInfo;


global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.IdentityModel.Logging;
global using Microsoft.IdentityModel.Tokens;

global using System.IdentityModel.Tokens.Jwt;
global using System.Net;
global using System.Security.Claims;
global using System.Security.Cryptography.X509Certificates;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Net.Http.Headers;
global using System.Text.Encodings.Web;

global using ClaimValueTypes = System.Security.Claims.ClaimValueTypes;

global using static MinApiLib.Oidc.Server.OidcServerConstants;
global using Base64Url = System.Buffers.Text.Base64Url;
global using TokenResponse = MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient.TokenResponse;
global using AuthorizeResponse = MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient.AuthorizeResponse;
global using TokenRevocationResponse = MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient.TokenRevocationResponse;
