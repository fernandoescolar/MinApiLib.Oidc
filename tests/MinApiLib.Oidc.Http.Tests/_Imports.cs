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

global using MinApiLib.Oidc.DPoP;
global using MinApiLib.Oidc.Http;
global using MinApiLib.Oidc.Http.ClientCredentials;
global using MinApiLib.Oidc.Server;
global using MinApiLib.Oidc.Server.Core.ClientAuthentication;
global using MinApiLib.Oidc.Server.Core.Models;
global using MinApiLib.Oidc.Testing;
