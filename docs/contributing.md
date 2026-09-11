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
- `dotnet pack` runs package validation across the core package's target frameworks (`EnablePackageValidation`). Abstract records trip it: net8.0+ redeclares `<Clone>$` with a covariant return that netstandard2.0 lacks. Such differences are suppressed in `src/Scott.Mail.Smtp2Go/CompatibilitySuppressions.xml`; regenerate it with `dotnet pack src/Scott.Mail.Smtp2Go -c Release /p:ApiCompatGenerateSuppressionFile=true` when a new abstract record is added and review the diff.
- Warnings are errors, code style is enforced in the build, namespaces are file-scoped.
- Commits use conventional-commit prefixes (`feat`, `fix`, `test`, `docs`, `chore`, `ci`) and stay small.

## When the API changes

`docs/api-spec/` is a snapshot of the published API reference, harvested from [developers.smtp2go.com](https://developers.smtp2go.com/llms.txt) by `src/tools/Scott.Mail.Smtp2Go.SpecHarvester`. The `spec-drift` workflow re-harvests weekly and opens a pull request when the snapshot changes; the contract tests (`test/Scott.Mail.Smtp2Go.Tests.Contract`) then show which models disagree with the docs. To do the same by hand:

```shell
dotnet run --project src/tools/Scott.Mail.Smtp2Go.SpecHarvester -- harvest --out docs/api-spec
dotnet run --project src/tools/Scott.Mail.Smtp2Go.SpecHarvester -- diff --against HEAD
dotnet test --project test/Scott.Mail.Smtp2Go.Tests.Contract
```

1. Read the diff: added or removed operations, request and response properties, deprecations, rate-limit notes, `subaccount_id` support, and the webhook callback parameters (`callbacks/email`, `callbacks/sms`).
2. Update the models. Every request record carries `[Smtp2GoEndpoint("family/op")]` (for an endpoint without a request body, the response data record carries it); the tests find models through that attribute and the client interfaces, so a new property only needs its `[JsonPropertyName]`. Add or adjust the `EndpointTable` descriptor when the method, `subaccount_id` support or rate limit changed; descriptors are keyed by path and method, so a path documented as both `POST` and `PATCH` gets two rows.
3. Edit `docs/api-spec/known-unmodelled.json` only with a reason. A whole-operation entry (no `scope`) says "not implemented yet"; delete it when the family lands. A `request`, `response` or `callback` entry with a `field` says "documented, deliberately not modelled"; a `response` entry with `fixture` says "the docs example is wrong, test this live fixture instead". `CoverageTests` fails on stale entries.
4. Regenerate the coverage page and commit it with the snapshot:

```shell
dotnet run --project src/tools/Scott.Mail.Smtp2Go.SpecHarvester -- coverage
```

`harvest --cache <dir>` keeps the downloaded pages for offline re-runs; `harvest --source <dir>` reads a directory laid out like the site (`llms.txt`, `reference/*.md`, `docs/webhooks-overview.md`). A page whose OpenAPI block does not parse is listed under `unparsedPages` in `endpoints.json` with its prose, and the run still succeeds.
