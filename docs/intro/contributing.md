[[go back to index]](../index.md#documentation-structure)

# Contributing

Contributions are welcome. Open an issue first to discuss a change, then send a pull request with the smallest change
that does the job and tests that cover it.

## Building and testing

Requirements: the .NET SDK pinned in `global.json` (11.0.100 or a later feature band), and Docker for the MongoDB
tests.

```bash
dotnet build MinApiLib.Oidc.slnx
```

```bash
dotnet test --solution MinApiLib.Oidc.slnx
```

The tests run on xUnit v3 with Microsoft.Testing.Platform. The MongoDB tests start MongoDB in Docker
(Testcontainers), so they need Docker running.

`tests/MinApiLib.Oidc.Server.IntegrationTests/Snapshots` pins the discovery document and the shape of an access
token in `*.verified.json` files. A change writes a `*.received.json` file next to it; if the change is intended,
replace the verified file with it.

Mutation testing with [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) is set up as a local
tool. A full run takes long, so narrow it with `--mutate`:

```bash
dotnet tool restore
dotnet stryker --mutate "**/Core/DPoP/DefaultDPoPProofValidator.cs"
```

## Conventions

* Build settings shared by every project live in `Directory.Build.props`; package versions are managed centrally in
  `Directory.Packages.props`.
* Warnings are errors, code style is enforced in the build (`.editorconfig`) and the .NET analyzers run in
  `Recommended` mode. `tests/.editorconfig` relaxes some rules for test code.
* Nullable reference types are enabled in every library.
* Library code logs only through source-generated `[LoggerMessage]` methods (CA1848 is an error).
* Tests use the xUnit assertions only. The server's integration tests carry a small test-only protocol client in
  `Common/ProtocolClient`.
* The server is organized in vertical slices, one folder per protocol feature, with what they share in `Core`. Default
  services are registered with MinApiLib's `[RegisterInIServiceCollection]` attribute. See the
  [server README](../../src/MinApiLib.Oidc.Server/README.md#layout).
* Bug fixes and new features come with tests. Check that a fix's test fails without the fix.

## Licensing

The code is licensed under the [Apache License 2.0](../../LICENSE). Parts of the server, of its integration tests and
of the documentation derive from IdentityServer4 and IdentityModel: keep their attribution in [NOTICE](../../NOTICE)
and the copyright headers of the files that have them. A file that you derive from IdentityServer4 gets the same
three-line header (the original copyright, the license and a line saying it was modified); a new file gets none. By
contributing, you agree that your contribution is licensed under the same terms.

[[go back to index]](../index.md#documentation-structure)
