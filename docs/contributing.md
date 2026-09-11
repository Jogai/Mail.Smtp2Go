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

Unit and contract tests never touch the network. The integration tests marked `[Trait("Category", "Sandbox")]` call the SMTP2GO sandbox, which accepts without delivering; they skip unless a sandbox key is present in the `SMTP2GO_SANDBOX_API_KEY` environment variable or the user secret `Smtp2Go:ApiKey:Sandbox`:

```shell
dotnet user-secrets set "Smtp2Go:ApiKey:Sandbox" "api-..." --project test/Scott.Mail.Smtp2Go.Tests.Integration
dotnet test --project test/Scott.Mail.Smtp2Go.Tests.Integration
```

Tests marked `[Trait("Category", "Live")]` send real email and need a live key with Email Archiving, a verified sender and a recipient: `SMTP2GO_LIVE_API_KEY`, `SMTP2GO_SENDER`, `SMTP2GO_RECIPIENT` (or the user secrets `Smtp2Go:ApiKey:Live`, `Smtp2Go:Live:Sender`, `Smtp2Go:Live:Recipient`). The webhook capture test additionally needs `cloudflared` on the PATH and, with `SMTP2GO_CAPTURE=1`, overwrites the fixtures in `test/Scott.Mail.Smtp2Go.Tests.Shared/Fixtures/Webhooks/Live/` with the callbacks it receives. Run them on purpose: `dotnet test --project test/Scott.Mail.Smtp2Go.Tests.Integration -- --filter-trait Category=Live`.

The `demo/` project is the manual end-to-end check for the archive: see [archive.md](archive.md).

## Conventions

- Central Package Management: versions live only in `Directory.Packages.props`; never put `Version=` on a `PackageReference`. Lock files (`packages.lock.json`) are committed.
- Every public member of a package is listed in that project's `PublicAPI.Unshipped.txt`; the build fails otherwise. Move entries to `PublicAPI.Shipped.txt` at release time.
- Warnings are errors, code style is enforced in the build, namespaces are file-scoped.
- Commits use conventional-commit prefixes (`feat`, `fix`, `test`, `docs`, `chore`, `ci`) and stay small.
