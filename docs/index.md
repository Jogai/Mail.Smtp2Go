# Scott.Mail.Smtp2Go documentation

- [Getting started](getting-started.md): install, create a client, send a first email.
- [Sending email](sending.md): `client.Email`, the three send paths, `fastaccept`, scheduling, attachments, templates, `EnsureAccepted`, limits.
- [Configuration](configuration.md): options, regions, authentication scheme, endpoint descriptors, diagnostics, dependency injection.
- [Observability](observability.md): logging event ids, `System.Diagnostics.Metrics` instruments, the `ActivitySource`, and how the dependency injection package wires them.
- [Errors](errors.md): the exception hierarchy and how each API response maps to it.
- [Webhooks](webhooks.md): registering webhooks, choosing JSON output, authentication, the callback parser and event types, docs-versus-live names.
- [Reporting](reporting.md): `client.Stats` (six endpoints, `GetQuotaAsync`), `client.Activity` (search, paging, the 60/min throttle).
- [Archive](archive.md): searching archived email, the indexing delay, downloading originals, the demo.
- [API notes](api-notes.md): every place the live API and the documentation disagree, with what the library does about it.
- [API coverage](api-coverage.md): generated table of every SMTP2GO endpoint and the client member that covers it.
- [Contributing](contributing.md): SDK setup, build, test, pack, release.
