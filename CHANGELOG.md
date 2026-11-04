# Changelog

All notable changes to the `Scott.Mail.Smtp2Go` packages are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the packages use [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The three packages (`Scott.Mail.Smtp2Go`, `Scott.Mail.Smtp2Go.DependencyInjection`, `Scott.Mail.Smtp2Go.AspNetCore`) share one version.

Pull requests opened by the `spec-drift` workflow record what changed in the published API reference under "API compatibility".

## [Unreleased]

### Added

- `ActivitySearchRequest.SearchSubjectOperator`, `SearchSenderOperator` and `SearchRecipientOperator` choose how the subject, sender and recipient filters compare (`ActivitySearchOperator`: `EqualTo`, `Contains`, `NotEqualTo`, `DoesNotContain`).
- `WebhookAddRequest.CustomHeaders` and `WebhookEditRequest.CustomHeaders` set custom HTTP headers that SMTP2GO sends with each callback; `Webhook.CustomHeaders` reads them back. `AddOrUpdateByUrlAsync` carries them over.
- `ApiKey.SendingStatus` reports whether SMTP2GO has blocked a key's sending for misuse, separately from its `Status`.

### Fixed

- The DependencyInjection package no longer picks up `Microsoft.Extensions.Configuration`, `DependencyInjection` and `Logging` as dependencies (added by a Dependabot update), and the lock files restore in locked mode again.
- The spec harvester reports a reader error under its own path only; Microsoft.OpenApi prefixed it with every path read before it, so the location changed whenever an operation was added.

### API compatibility

- Spec snapshot of 2026-11-04: `search_subject_op`, `search_sender_op` and `search_recipient_op` on `activity/search`; `custom_headers` on the four webhook endpoints (its schema is malformed upstream, accepted in `validation-baseline.json`); `sending_status` on API key responses. The API key, SMTP user and IP authentication response schemas no longer describe each item as an envelope, so the ten allow-list entries for that are gone; the rewritten schemas spell `custom_ratelimit` as `custom_rate_limit`, treated as a docs typo (docs/api-notes.md). `webhook/view` no longer lists `url` as required.

### Changed

- Build-time code generation (`src/tools/Scott.Mail.Smtp2Go.Generators`, a Roslyn source generator referenced as an analyzer; nothing is added to the packages): the `EndpointTable` rows now come from `docs/api-spec/endpoints.json` with the traits the reference does not state kept as `[EndpointTraits]` patterns, 47 pass-through client methods are generated from the client interfaces, the enum wire-name tables replace reflection for the library's own enums, and the webhook event type switches (record update and handler dispatch) follow the event hierarchy. No public API or behaviour changes; the generated endpoint table equals the previous hand-written one.

## [1.4.0] - 2026-10-05

### Added

- `WebhookEditRequest.Disabled` switches a webhook off or back on without deleting it, and `Webhook.Disabled` reports the state on every webhook response.
- The spec harvester validates the merged OpenAPI document with `Microsoft.OpenApi` before writing it, plus three checks of its own (OpenAPI 3.x, at least one operation, unique `operationId`s). Findings are recorded in `docs/api-spec/validation.json`; `validation-baseline.json` lists the accepted ones, the contract tests fail on any other, `harvest --strict` turns them into a failing exit code and a new `validate` command re-checks the committed snapshot. The current snapshot has no findings.

### API compatibility

- SMTP2GO documented a `disabled` property on webhooks: a request field of `webhook/edit` and a response field of `webhook/add`, `edit`, `remove` and `view` (spec snapshot of 2026-10-05).

## [1.3.6] - 2026-10-03

### Added

- `archive/search` and `archive/email` accept `subaccount_id`: `client.Archive.SearchAsync`, `SearchAllAsync` and `GetAsync` send `RequestOptions.SubaccountId` (or `DefaultSubaccountId`), so a master account key can read a subaccount's archive.

### Fixed

- The spec harvester reads the grouped `## API Reference: GROUP` headings that `llms.txt` uses since September 2026; with the old single-section parser it found no reference pages and harvested nothing.
- Test fixtures no longer expire: outgoing `schedule` values in the tests are fixed at 2038-01-19T03:14:07Z instead of a date in September 2026.

### API compatibility

- SMTP2GO documented `subaccount_id` on `archive/search` and `archive/email` (spec snapshot of 2026-09-21).

## [1.3.5] - 2026-09-11

First release.

### Added

- `Scott.Mail.Smtp2Go` (`netstandard2.0`, `net8.0`, `net10.0`; trim- and AOT-compatible): a typed client for every one of the 69 operations in the published SMTP2GO v3 API reference, one client per family on `ISmtp2GoClient`: `Email` (JSON, MIME and batch sends, `fastaccept`, scheduling, attachments, templates, scheduled-email search and removal), `Webhooks`, `Stats`, `Activity`, `Templates`, `Suppressions`, `Archive` (search and download of originals), `AllowedSenders`, `AllowedRecipients`, `ApiKeys`, `SmtpUsers`, `IpAuth`, `Domains`, `SingleSenders`, `Subaccounts`, `DedicatedIps` and `Sms`, plus `Raw` for any endpoint. `docs/api-coverage.md` maps each operation to its member.
- Transport: API key sent in the `X-Smtp2go-Api-Key` header (`AuthenticationScheme.Bearer` as the alternative), regional base URLs, `subaccount_id` injection from `RequestOptions` or the options, per-endpoint body limits and client-side rate limiting from the documented limits, client-side validation, the `ApiResponse<T>` envelope, `EmailSendResult.EnsureAccepted()`, and an exception hierarchy (`Smtp2GoException`; `Smtp2GoApiException` with `Smtp2GoAuthenticationException`, `Smtp2GoPermissionException`, `Smtp2GoRateLimitException` and `Smtp2GoSendException`; `Smtp2GoValidationException`) built from the API's own error payloads.
- Webhook callbacks: `WebhookPayloadParser` reads the JSON, form-urlencoded and multipart callback formats into typed events (`EmailBounceEvent`, `EmailClickEvent`, `EmailOpenEvent`, ..., `SmsStatusEvent`, `UnknownWebhookEvent`), tolerant of the documented-versus-live name differences recorded in `docs/api-notes.md`.
- Observability: `[LoggerMessage]` log events, `System.Diagnostics.Metrics` instruments and an `ActivitySource`, all behind `ISmtp2GoDiagnostics`.
- `Scott.Mail.Smtp2Go.DependencyInjection` (`net8.0`, `net10.0`): `AddSmtp2Go` with options binding and validation (`Smtp2GoOptions`), `IHttpClientFactory` integration, a resilience pipeline (retry, circuit breaker, timeout, Polly rate limiter) through `Microsoft.Extensions.Http.Resilience`, named clients, and wiring of the logging and metrics diagnostics.
- `Scott.Mail.Smtp2Go.AspNetCore` (`net8.0`, `net10.0`): `MapSmtp2GoWebhook` minimal-API endpoint that authenticates callbacks (Basic, Bearer, URL user info), checks the source address against SMTP2GO's published hosts, bounds the body size, accepts every callback content type, and dispatches typed events to `IWebhookEventHandler<TEvent>` registrations made with `AddSmtp2GoWebhooks`.
- Spec harvester (`src/tools/Scott.Mail.Smtp2Go.SpecHarvester`, not packaged): harvests the API reference into `docs/api-spec/`, diffs snapshots and generates `docs/api-coverage.md`; the weekly `spec-drift` workflow opens a pull request when the published reference changes and the contract tests show which models disagree.
- Documentation under `docs/` (getting started, sending, configuration, observability, errors, webhooks, reporting, archive, account management and SMS, API notes, API coverage, contributing) and three runnable demos (`demo/`: send-and-archive round trip, hosted client, webhook receiver).

### API compatibility

- Public API tracked with `Microsoft.CodeAnalysis.PublicApiAnalyzers`; the shipped surface for this release is in each project's `PublicAPI.Shipped.txt`. Package validation runs across target frameworks on every pack; a baseline against 1.3.5 is enabled from the next release.

[Unreleased]: https://github.com/Jogai/Mail.Smtp2Go/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/Jogai/Mail.Smtp2Go/compare/v1.3.6...v1.4.0
[1.3.6]: https://github.com/Jogai/Mail.Smtp2Go/compare/v1.3.5...v1.3.6
[1.3.5]: https://github.com/Jogai/Mail.Smtp2Go/releases/tag/v1.3.5
