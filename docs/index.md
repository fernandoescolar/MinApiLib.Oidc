# Welcome to IdentityServer

IdentityServer is an OpenID Connect and OAuth 2.0 framework for ASP.NET Core.

This project is a fork of the original IdentityServer4 project. The main goal of this fork is to provide an updated and mantained version of the original project. The original project is not being updated anymore, and the last version is 4.1.1. This fork will be updated to the latest version of .NET Core and will be maintained by @fernandoescolar.

- [Welcome to IdentityServer](#welcome-to-identityserver)
  - [Documentation Structure](#documentation-structure)
  - [About IdentityServer4](#about-identityserver4)
  - [Features](#features)
    - [Authentication as a Service](#authentication-as-a-service)
    - [Single Sign-on / Sign-out](#single-sign-on--sign-out)
    - [Access Control for APIs](#access-control-for-apis)
    - [Federation Gateway](#federation-gateway)
    - [Focus on Customization](#focus-on-customization)


## Documentation Structure

- INTRODUCTION
  - [The Big Picture](intro/big_picture.md)
  - [Terminology](intro/terminology.md)
  - [Supported Specifications](intro/specs.md)
  - [Packaging and Builds](intro/packaging.md)
  - [Support and Consulting Options](intro/support.md)
  - [Contributing](intro/contributing.md)
- QUICKSTARTS
  - [Overview](quickstarts/0_overview.md)
  - [Protecting an API using Client Credentials](quickstarts/1_client_credentials.md)
  - [Interactive Applications with ASP.NET Core](quickstarts/2_interactive_aspnetcore.md)
  - [ASP.NET Core and API access](quickstarts/3_aspnetcore_and_apis.md)
  - [Adding a JavaScript client](quickstarts/4_javascript_client.md)
  - [Using EntityFramework Core for configuration and operational data](quickstarts/5_entityframework.md)
  - [Using ASP.NET Core Identity](quickstarts/6_aspnet_identity.md)
- TOPICS
  - [Startup](topics/startup.md)
  - [Defining Resources](topics/resources.md)
  - [Defining Clients](topics/clients.md)
  - [Sign-in](topics/signin.md)
  - [Custom prompt values](topics/prompt.md)
  - [Sign-in with External Identity Providers](topics/signin_external_providers.md)
  - [Windows Authentication](topics/windows.md)
  - [Sign-out](topics/signout.md)
  - [Sign-out of External Identity Providers](topics/signout_external_providers.md)
  - [Federated Sign-out](topics/signout_federated.md)
  - [Federation Gateway](topics/federation_gateway.md)
  - [Consent](topics/consent.md)
  - [Protecting APIs](topics/apis.md)
  - [Deployment](topics/deployment.md)
  - [Logging](topics/logging.md)
  - [Events](topics/events.md)
  - [Cryptography, Keys and HTTPS](topics/crypto.md)
  - [Grant Types](topics/grant_types.md)
  - [Client Authentication](topics/client_authentication.md)
  - [Extension Grants](topics/extension_grants.md)
  - [Resource Owner Password Validation](topics/resource_owner.md)
  - [Refresh Tokens](topics/refresh_tokens.md)
  - [Reference Tokens](topics/reference_tokens.md)
  - [Persisted Grants](topics/persisted_grants.md)
  - [Proof-of-Possession Access Tokens](topics/pop.md)
  - [Pushed Authorization Requests](topics/par.md)
  - [Mutual TLS](topics/mtls.md)
  - [Authorize Request Objects](topics/request_object.md)
  - [Custom Token Request Validation and Issuance](topics/custom_token_request_validation.md)
  - [CORS](topics/cors.md)
  - [Discovery](topics/discovery.md)
  - [Adding more API Endpoints](topics/add_apis.md)
  - [Adding new Protocols](topics/add_protocols.md)
  - [Tools](topics/tools.md)
- ENDPOINTS
  - [Discovery Endpoint](endpoints/discovery.md)
  - [Authorize Endpoint](endpoints/authorize.md)
  - [Token Endpoint](endpoints/token.md)
  - [UserInfo Endpoint](endpoints/userinfo.md)
  - [Device Authorization Endpoint](endpoints/device_authorization.md)
  - [Introspection Endpoint](endpoints/introspection.md)
  - [Revocation Endpoint](endpoints/revocation.md)
  - [End Session Endpoint](endpoints/endsession.md)
  - [Nonce Endpoint](endpoints/nonce.md)
- REFERENCE
  - [IdentityServer Options](reference/options.md)
  - [Identity Resource](reference/identity_resource.md)
  - [API Scope](reference/api_scope.md)
  - [API Resource](reference/api_resource.md)
  - [Client](reference/client.md)
  - [GrantValidationResult](reference/grant_validation_result.md)
  - [Profile Service](reference/profileservice.md)
  - [IdentityServer Interaction Service](reference/interactionservice.md)
  - [Device Flow Interaction Service](reference/deviceflow_interactionservice.md)
  - [ASP.NET Identity Support](reference/aspnet_identity.md)
  - [Nonce Authorization Pattern](reference/nonce_pattern.md)
MISC
  - [Training](misc/training.md)
  - [Blog posts](misc/blogs.md)
  - [Videos](misc/videos.md)

## About IdentityServer4

![image](images/logo.png)


IdentityServer is a free, open source [OpenID Connect](http://openid.net/connect/) and [OAuth 2.0](https://tools.ietf.org/html/rfc6749) framework for ASP.NET Core.
Founded and maintained by [Dominick Baier](https://twitter.com/leastprivilege) and [Brock Allen](https://twitter.com/brocklallen), IdentityServer4 incorporates all the protocol implementations and extensibility points needed to integrate token-based authentication, single-sign-on and API access control in your applications.
IdentityServer4 is officially [certified](https://openid.net/certification/) by the [OpenID Foundation](https://openid.net) and thus spec-compliant and interoperable.
It is part of the [.NET Foundation](https://www.dotnetfoundation.org/), and operates under their [code of conduct](https://www.dotnetfoundation.org/code-of-conduct). It is licensed under [Apache 2](https://opensource.org/licenses/Apache-2.0) (an OSI approved license).

For project documentation, please visit [readthedocs](https://identityserver4.readthedocs.io).

## Features

### Authentication as a Service

Centralized login logic and workflow for all of your applications (web, native, mobile, services). IdentityServer is an officially [certified](https://openid.net/certification/) implementation of OpenID Connect.


### Single Sign-on / Sign-out

Single sign-on (and out) over multiple application types.

### Access Control for APIs

Issue access tokens for APIs for various types of clients, e.g. server to server, web applications, SPAs and native/mobile apps.

### Federation Gateway

Support for external identity providers like Azure Active Directory, Google, Facebook etc. This shields your applications from the details of how to connect to these external providers.

### Focus on Customization

The most important part - many aspects of IdentityServer can be customized to fit **your** needs. Since IdentityServer is a framework and not a boxed product or a SaaS, you can write code to adapt the system the way it makes sense for your scenarios.
