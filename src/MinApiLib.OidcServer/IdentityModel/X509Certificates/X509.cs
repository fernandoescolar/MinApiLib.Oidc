// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel); modified for MinApiLib.OidcServer.

using System.Security.Cryptography.X509Certificates;

#pragma warning disable 1591

namespace MinApiLib.OidcServer.IdentityModel;

public static class X509
{
    public static X509CertificatesLocation CurrentUser => new X509CertificatesLocation(StoreLocation.CurrentUser);
    public static X509CertificatesLocation LocalMachine => new X509CertificatesLocation(StoreLocation.LocalMachine);
}