global using System.Buffers.Text;
global using System.Net;
global using System.Security.Claims;
global using System.Text.Json;
global using System.Text.Json.Serialization;

global using Microsoft.AspNetCore.Antiforgery;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;

global using MongoDB.Bson;
global using MongoDB.Bson.Serialization.Attributes;
global using MongoDB.Driver;

global using MinApiLib.Endpoints;
global using MinApiLib.Oidc.Server;
global using MinApiLib.Oidc.Server.Core.ClientAuthentication;
global using MinApiLib.Oidc.Server.Core.Events;
global using MinApiLib.Oidc.Server.Core.IdentityModel;
global using MinApiLib.Oidc.Server.Core.Models;
global using MinApiLib.Oidc.Server.Core.Stores;
global using MinApiLib.Oidc.Server.Core.Users;
global using MinApiLib.Oidc.Server.Features.Interaction;
global using MinApiLib.Oidc.Server.MongoDB;

global using IdP.Features.Events;
global using IdP.Features.IdentityProviders;
global using IdP.Infrastructure;
global using IdP.Infrastructure.Email;
global using IdP.Users;
global using MinApiLib.Oidc.Server.Core.Infrastructure;
global using MinApiLib.Oidc.Server.Features.DeviceFlow;
global using MinApiLib.Oidc.Server.Features.Session;
global using IdP.Features.Flow;
