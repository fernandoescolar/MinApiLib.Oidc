global using System.Net;
global using System.Security.Cryptography;
global using System.Text;
global using System.Globalization;
global using System.Text.Json;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.OpenIdConnect;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Tokens;

global using MinApiLib.Endpoints;
global using MinApiLib.OidcServer.Client;
global using MinApiLib.OidcServer.Client.DPoP;
global using MinApiLib.OidcServer.Client.OpenIdConnect;
global using MinApiLib.OidcServer.Client.Tokens;
