global using System.Collections.Concurrent;
global using System.Globalization;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.OpenIdConnect;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Protocols;
global using Microsoft.IdentityModel.Protocols.OpenIdConnect;
global using Microsoft.IdentityModel.Tokens;

global using MinApiLib.OidcServer.Client.DPoP;
global using MinApiLib.OidcServer.Client.Tokens;
