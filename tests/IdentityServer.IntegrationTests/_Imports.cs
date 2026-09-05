global using IdentityServer.IdentityModel;
global using IdentityServer.IntegrationTests.ProtocolClient;

global using IdentityServer.Configuration;
global using IdentityServer.IntegrationTests.Common;
global using IdentityServer.Models;
global using IdentityServer.Services;
global using IdentityServer.Stores;
global using IdentityServer.Stores.Default;
global using IdentityServer.Test;
global using IdentityServer.Validation;


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

global using static IdentityServer.IdentityServerConstants;
global using Base64Url = System.Buffers.Text.Base64Url;
