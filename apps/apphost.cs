#:sdk Aspire.AppHost.Sdk@13.6.5
#:package Aspire.Hosting.MongoDB
#:project IdP/IdP.csproj
// The dashboard and the orchestrator come as NuGet packages, so it runs with dotnet run, without the Aspire CLI.
#:property NoWarn=$(NoWarn);ASPIRE010

// Runs the identity provider with Aspire: MongoDB in a container, the IdP on it, and the Aspire dashboard with its
// logs and traces. From this folder:
//
//   dotnet run apphost.cs
//
// The IdP keeps the URL of its launch profile, https://localhost:5100, and the Development environment, so the
// administrator is admin / admin123 (IdP/appsettings.Development.json).

var builder = DistributedApplication.CreateBuilder(args);

// Users, clients, grants, signing keys and events live in this database, which survives restarts in a volume.
// MongoDB creates its admin user only when the volume is empty, so the password must stay the same from one run to
// the next: a password Aspire generates, and keeps in user secrets, breaks the volume as soon as it is generated again.
// A fixed password for this local database, published only on 127.0.0.1, and a named volume avoid it.
var mongoPassword = builder.AddParameter("mongodb-password", "minapilib-oidc-idp", secret: true);
var mongo = builder.AddMongoDB("mongodb", password: mongoPassword)
    .WithDataVolume("minapilib-oidc-idp-mongodb");
var database = mongo.AddDatabase("idp-database", databaseName: "idp");

// ConnectionStrings__Mongo is the IdP's connection string; IdP__Database names the database (idp by default).
builder.AddProject<Projects.IdP>("idp", launchProfileName: "https")
    .WithReference(database, connectionName: "Mongo")
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
