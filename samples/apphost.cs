#:sdk Aspire.AppHost.Sdk@13.6.5
#:package Aspire.Hosting.MongoDB
#:project Server/Server.csproj
#:project Api/Api.csproj
#:project WebClient/WebClient.csproj
#:project Worker/Worker.csproj
// The dashboard and the orchestrator come as NuGet packages, so it runs with dotnet run, without the Aspire CLI.
#:property NoWarn=$(NoWarn);ASPIRE010

// Runs every sample with Aspire: MongoDB in a container, the OpenID Connect server on it, the API, the web client and
// the worker, and the Aspire dashboard with their logs and traces. From this folder:
//
//   dotnet run apphost.cs
//
// The applications keep the URLs of their launch profiles (server https://localhost:5001, web client :5002, API
// :5003), which the clients' configuration and the server's redirect URIs expect.

var builder = DistributedApplication.CreateBuilder(args);

// The server keeps its configuration, grants and signing keys in this database, which survives restarts in a volume.
var mongo = builder.AddMongoDB("mongodb")
    .WithDataVolume();
var database = mongo.AddDatabase("oidcserver-sample");

// ConnectionStrings__Mongo turns on the server's MongoDB stores; it writes the sample clients and resources at startup.
var server = builder.AddProject<Projects.Server>("server", launchProfileName: "https")
    .WithReference(database, connectionName: "Mongo")
    .WaitFor(database)
    .WithHttpHealthCheck("/.well-known/openid-configuration");

var api = builder.AddProject<Projects.Api>("api")
    .WaitFor(server)
    .WithHttpHealthCheck("/");

builder.AddProject<Projects.WebClient>("webclient")
    .WaitFor(server)
    .WaitFor(api);

// The worker calls the API with its own token and exits; the dashboard keeps its output.
builder.AddProject<Projects.Worker>("worker")
    .WaitFor(server)
    .WaitFor(api);

builder.Build().Run();
