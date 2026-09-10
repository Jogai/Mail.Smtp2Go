# Contributing

## SDK

The repository pins .NET SDK `10.0.401` in `global.json`. With [vfox](https://vfox.dev/) and its `dotnet` plugin:

```shell
vfox install dotnet@10.0.12
vfox use dotnet@10.0.12
dotnet --version   # 10.0.401
```

`.vfox.toml` records the same version for vfox users. Without vfox, install SDK 10.0.401 (or a later 10.0.4xx patch; `global.json` rolls forward to the latest patch).

## Build, test, pack

```shell
dotnet restore --locked-mode
dotnet build -warnaserror
dotnet test
dotnet pack -c Release -o artifacts
```

Tests run on Microsoft.Testing.Platform (selected by `test.runner` in `global.json`) with xUnit v3.

## Conventions

- Central Package Management: versions live only in `Directory.Packages.props`; never put `Version=` on a `PackageReference`. Lock files (`packages.lock.json`) are committed.
- Every public member of a package is listed in that project's `PublicAPI.Unshipped.txt`; the build fails otherwise. Move entries to `PublicAPI.Shipped.txt` at release time.
- Warnings are errors, code style is enforced in the build, namespaces are file-scoped.
- Commits use conventional-commit prefixes (`feat`, `fix`, `test`, `docs`, `chore`, `ci`) and stay small.
