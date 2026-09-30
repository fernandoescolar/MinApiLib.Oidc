global using System.Net;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Cookies;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Tokens;

global using MinApiLib.OidcServer;
global using MinApiLib.OidcServer.Client;
global using MinApiLib.OidcServer.Client.ClientCredentials;
global using MinApiLib.OidcServer.Client.DPoP;
global using MinApiLib.OidcServer.Client.OpenIdConnect;
global using MinApiLib.OidcServer.Client.Tests.Infrastructure;
global using MinApiLib.OidcServer.Client.Tokens;
global using MinApiLib.OidcServer.Core.Models;
global using MinApiLib.OidcServer.Core.ClientAuthentication;
