global using MinApiLib.OidcServer.Core.IdentityModel;
global using MinApiLib.OidcServer.Core.IdentityModel.X509Certificates;
global using MinApiLib.OidcServer.IntegrationTests.ProtocolClient;

global using MinApiLib.OidcServer.Core.Configuration;
global using MinApiLib.OidcServer.Core.Cors;
global using MinApiLib.OidcServer.Core.DPoP;
global using MinApiLib.OidcServer.Core.Events;
global using MinApiLib.OidcServer.Core.Hosting;
global using MinApiLib.OidcServer.Core.Keys;
global using MinApiLib.OidcServer.Core.Logging;
global using MinApiLib.OidcServer.Core.TokenServices;
global using MinApiLib.OidcServer.Features.DeviceFlow;
global using MinApiLib.OidcServer.Features.Discovery;
global using MinApiLib.OidcServer.Features.Interaction;
global using MinApiLib.OidcServer.Features.PushedAuthorization;
global using MinApiLib.OidcServer.IntegrationTests.Common;
global using MinApiLib.OidcServer.Core.ClientAuthentication;
global using MinApiLib.OidcServer.Core.Clients;
global using MinApiLib.OidcServer.Core.Models;
global using MinApiLib.OidcServer.Core.ResourceValidation;
global using MinApiLib.OidcServer.Core.Users;
global using MinApiLib.OidcServer.Features.Authorize;
global using MinApiLib.OidcServer.Features.Nonce;
global using MinApiLib.OidcServer.Features.Session;
global using MinApiLib.OidcServer.Features.Tokens;
global using MinApiLib.OidcServer.Core.Infrastructure;
global using MinApiLib.OidcServer.Core.Stores;
global using MinApiLib.OidcServer.Test;
global using MinApiLib.OidcServer.Core.Validation;
global using MinApiLib.OidcServer.Features.Introspection;
global using MinApiLib.OidcServer.Features.Revocation;
global using MinApiLib.OidcServer.Features.Tokens.Grants;
global using MinApiLib.OidcServer.Features.UserInfo;


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

global using static MinApiLib.OidcServer.OidcServerConstants;
global using Base64Url = System.Buffers.Text.Base64Url;
global using TokenResponse = MinApiLib.OidcServer.IntegrationTests.ProtocolClient.TokenResponse;
global using AuthorizeResponse = MinApiLib.OidcServer.IntegrationTests.ProtocolClient.AuthorizeResponse;
global using TokenRevocationResponse = MinApiLib.OidcServer.IntegrationTests.ProtocolClient.TokenRevocationResponse;
