global using System.Net;
global using System.Security.Claims;
global using System.Text.Json;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using MinApiLib.Oidc.Server;
global using MinApiLib.Oidc.Server.Core.ClientAuthentication;
global using MinApiLib.Oidc.Server.Core.Keys;
global using MinApiLib.Oidc.Server.Core.Models;
global using MinApiLib.Oidc.Server.Core.Stores;
global using MinApiLib.Oidc.Server.Core.Stores.Serialization;
global using MinApiLib.Oidc.Server.Features.DeviceFlow;
global using MinApiLib.Oidc.Server.MongoDB;

global using MongoDB.Bson;
global using MongoDB.Driver;

global using Testcontainers.MongoDb;
