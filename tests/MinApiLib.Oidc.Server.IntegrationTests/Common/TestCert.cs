// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Common;

internal static class TestCert
{
    public static X509Certificate2 Load()
    {
        var cert = Path.Combine(System.AppContext.BaseDirectory, "identityserver_testing.pfx");
        return X509CertificateLoader.LoadPkcs12FromFile(cert, "password");
    }
}
