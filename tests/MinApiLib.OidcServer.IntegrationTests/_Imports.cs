global using MinApiLib.OidcServer.IdentityModel;
global using MinApiLib.OidcServer.IntegrationTests.ProtocolClient;

global using MinApiLib.OidcServer.Configuration;
global using MinApiLib.OidcServer.IntegrationTests.Common;
global using MinApiLib.OidcServer.Models;
global using MinApiLib.OidcServer.Services;
global using MinApiLib.OidcServer.Stores;
global using MinApiLib.OidcServer.Stores.Default;
global using MinApiLib.OidcServer.Test;
global using MinApiLib.OidcServer.Validation;


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
