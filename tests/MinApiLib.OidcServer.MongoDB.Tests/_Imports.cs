global using System.Net;
global using System.Security.Claims;
global using System.Text.Json;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using MinApiLib.OidcServer;
global using MinApiLib.OidcServer.Core.ClientAuthentication;
global using MinApiLib.OidcServer.Core.Keys;
global using MinApiLib.OidcServer.Core.Models;
global using MinApiLib.OidcServer.Core.Stores;
global using MinApiLib.OidcServer.Core.Stores.Serialization;
global using MinApiLib.OidcServer.Features.DeviceFlow;
global using MinApiLib.OidcServer.MongoDB;

global using MongoDB.Bson;
global using MongoDB.Driver;

global using Testcontainers.MongoDb;
