# syntax=docker/dockerfile:1
# Builds the identity provider. The context is the repository root, because the project references the libraries in
# src/:
#
#   docker build -f apps/IdP/Dockerfile -t minapilib-oidc-idp .
#
# The frontend is built with Node.js, the backend with the .NET SDK, and the image runs on the ASP.NET Core runtime.

FROM node:24-alpine AS frontend
WORKDIR /app/ClientApp
COPY apps/IdP/ClientApp/package.json apps/IdP/ClientApp/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY apps/IdP/ClientApp/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
# The version of the build. MinVer reads it from the git tags, which the image build has not: the workflow passes the tag.
ARG MINVERVERSIONOVERRIDE=0.0.0-alpha.0.local
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props dotnet.config .editorconfig ./
COPY samples/ClientApp.targets samples/
COPY src/MinApiLib.Oidc.DPoP/MinApiLib.Oidc.DPoP.csproj src/MinApiLib.Oidc.DPoP/
COPY src/MinApiLib.Oidc.Server/MinApiLib.Oidc.Server.csproj src/MinApiLib.Oidc.Server/
COPY src/MinApiLib.Oidc.Server.MongoDB/MinApiLib.Oidc.Server.MongoDB.csproj src/MinApiLib.Oidc.Server.MongoDB/
COPY apps/Directory.Build.props apps/
COPY apps/IdP/IdP.csproj apps/IdP/
RUN dotnet restore apps/IdP/IdP.csproj
COPY src/ src/
COPY apps/ apps/
COPY --from=frontend /app/wwwroot apps/IdP/wwwroot
# The frontend is already built (SkipClientApp). A framework-dependent, portable publish runs on any architecture.
RUN dotnet publish apps/IdP/IdP.csproj -c Release --no-restore -o /out -p:SkipClientApp=true -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
# Health: GET /health answers 200 when MongoDB is reachable.
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "IdP.dll"]
