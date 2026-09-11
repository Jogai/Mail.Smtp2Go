# Account management and SMS

The account-management families manage the credentials, senders and subaccounts of an SMTP2GO account; `client.Sms` sends and reports SMS messages. Every method takes an optional `RequestOptions` (per-call `SubaccountId`, API key, region, timeout) and a `CancellationToken`, validates the request client-side before sending (`Smtp2GoValidationException`), and returns the `ApiResponse<T>` envelope with `RequestId` and `Data`. Unmodelled response fields land in each record's `Extra` bag.

> **These endpoints change what the account may do.** An API key can only be created or edited with a subset of its own permissions, and editing the calling key's `status` or `endpoints` can lock it out. Changing the allowed-senders mode to whitelist or blacklist disables the Sender Domains and Single Sender Emails features. Removing a sender domain, an API key or an SMTP user stops everything that sends through it. Closing a subaccount stops its sending. Use a dedicated administrative key for these families and keep sending keys limited to `/email/*`.

## Sending credentials

API keys (`client.ApiKeys`), SMTP users (`client.SmtpUsers`) and authenticated IPs (`client.IpAuth`) share one settings shape: a description, an optional custom rate limit, an IP pool, feedback (unsubscribe footer) settings, open and click tracking, archiving, an audit address, `BounceNotifications` and a `CredentialStatus` (`Allowed`, `Blocked`, `Sandbox`).

- `BounceNotifications` is a small discriminated record over the documented string: `BounceNotifications.From` (return the bounce to the sender, the server default), `BounceNotifications.Drop` (discard it) or `BounceNotifications.Email("bounces@example.com")` (forward it). Responses parse the wire value with `Parse`; `Kind` and `EmailAddress` tell them apart.
- `CustomRateLimitPeriod` is the documented free-form period (`"1 hour"`, `"2 days"`, `"0:30:00"`, `"4 months 5:00:00"`); it is not an enum because the API accepts that syntax rather than a fixed set. Responses also show `"0:00:00"` and `"unlimited"`.
- `IpPool` is the pool id from `client.DedicatedIps.ViewAsync()`. Requests send it as `ip_pool`, responses return it as `ippool`; both map to `IpPool`.

### API keys (`client.ApiKeys`)

| Method | Endpoint | Returns |
| :-- | :-- | :-- |
| `ViewAsync(ApiKeyViewRequest)` | `api_keys/view` | `IReadOnlyList<ApiKey>`, keys masked (`api-000000000000****`). |
| `AddAsync(ApiKeyAddRequest)` | `api_keys/add` (5 per minute) | The new key, unmasked, as a one-element list. |
| `EditAsync(ApiKeyEditRequest)` | `POST api_keys/edit` | Full edit: omitted fields fall back to the documented defaults. |
| `PatchAsync(ApiKeyPatchRequest)` | `PATCH api_keys/edit` | Partial edit: omitted fields are left unchanged. |
| `RemoveAsync(id)` | `api_keys/remove` | `ApiResponse<JsonElement>`; the docs describe no `data`. |
| `GetPermissionsAsync()` | `api_keys/permissions` | The endpoints the calling key may use, for example `/email/send`. Available to every key. |

`Endpoints` is a list of paths or wildcard patterns (`"/email/*"`, `"*"`) and must be a subset of what the calling key may use; `GetPermissionsAsync()` returns that list.

```csharp
ApiResponse<IReadOnlyList<ApiKey>> created = await admin.ApiKeys.AddAsync(new ApiKeyAddRequest
{
    Description = "orders service",
    Endpoints = ["/email/send", "/email/mime"],
    BounceNotifications = BounceNotifications.Email("bounces@example.com"),
    Status = CredentialStatus.Allowed,
});
string key = created.Data[0].Key!; // the only time the key is returned in full

await admin.ApiKeys.PatchAsync(new ApiKeyPatchRequest { Id = key, Status = CredentialStatus.Blocked });
```

### SMTP users (`client.SmtpUsers`)

| Method | Endpoint | Returns |
| :-- | :-- | :-- |
| `ViewAsync(SmtpUserViewRequest)` | `users/smtp/view` | `SmtpUserViewResult`: `Results` plus the account's default rate limit. |
| `AddAsync(SmtpUserAddRequest)` | `users/smtp/add` | The new user as a one-element list, including the (possibly generated) password. |
| `EditAsync(SmtpUserEditRequest)` | `POST users/smtp/edit` | Full edit. |
| `PatchAsync(SmtpUserPatchRequest)` | `PATCH users/smtp/edit` | Partial edit. |
| `RemoveAsync(username)` | `users/smtp/remove` | The removed user as a one-element list. |

The POST endpoints wrap the users in `{"results": [...]}` while the PATCH one returns a bare array; `ResultsListConverter<SmtpUser>` reads both into `IReadOnlyList<SmtpUser>`. Responses carry `EmailPassword` in clear text; treat them as secrets. `Username` is 5 to 100 characters on `add`.

### Authenticated IPs (`client.IpAuth`)

| Method | Endpoint | Returns |
| :-- | :-- | :-- |
| `ViewAsync(AuthenticatedIpViewRequest)` | `ip_auth/view` | `AuthenticatedIpViewResult`: `Results` plus the account's default rate limit. |
| `PatchAsync(AuthenticatedIpPatchRequest)` | `PATCH ip_auth/edit` | Partial edit; the docs list no `POST` variant. |
| `RemoveAsync(ipAddress)` | `ip_auth/remove` | `ApiResponse<JsonElement>`; the docs describe no `data`. |

There is no `ip_auth/add` in the reference; entries are created in the SMTP2GO app. `IpAddress` is checked with `IPAddress.TryParse` before sending.

## Senders

### Sender domains (`client.Domains`)

| Method | Endpoint | Notes |
| :-- | :-- | :-- |
| `ViewAsync(DomainViewRequest)` | `domain/view` | All domains or one by name; `SetupLink` is only filled for a single domain. |
| `AddAsync(DomainAddRequest)` | `domain/add` | Verifies immediately (`AutoVerify`, server default true) and requests SSL for the tracking domain (`RequisitionSsl`). |
| `VerifyAsync(DomainVerifyRequest)` | `domain/verify` | Instead of waiting for the 7-minute periodic check. |
| `RemoveAsync(domain)` | `domain/remove` | Returns the remaining domains. |
| `SetTrackingSubdomainAsync(DomainTrackingRequest)` | `domain/tracking` | `OldSubdomain` to `NewSubdomain`. |
| `SetReturnPathSubdomainAsync(DomainReturnPathRequest)` | `domain/returnpath` | `OldSubdomain` to `NewSubdomain`. |
| `SetSubaccountAccessAsync(DomainSubaccountAccessRequest)` | `domain/subaccount_access` | Master-account keys; the endpoint takes no `subaccount_id`. |

All but the last return `DomainViewResult.Domains`, a list of `SenderDomain`: `Domain` (`DomainDetails` with the DKIM and return-path selectors, CNAME targets, verification flags and status text), `Trackers` (`TrackingDomain` with the CNAME verification), `SubaccountAccess` and `FromMaster` (true for a domain delegated from the master account, on `view` through a subaccount).

```csharp
ApiResponse<DomainViewResult> added = await client.Domains.AddAsync(new DomainAddRequest { Domain = "example.com", AutoVerify = false });
DomainDetails details = added.Data.Domains![0].Domain!;
Console.WriteLine($"CNAME {details.DkimSelector}._domainkey.example.com -> {details.DkimValue}");
Console.WriteLine($"CNAME {details.ReturnPathSelector}.example.com -> {details.ReturnPathValue}");
// publish the records, then:
await client.Domains.VerifyAsync(new DomainVerifyRequest { Domain = "example.com" });
```

### Single sender emails (`client.SingleSenders`)

`ViewAsync(SingleSenderViewRequest)` lists the addresses with `Verified`; `AddAsync(SingleSenderAddRequest)` adds one and emails it a verification link (or resends it); `RemoveAsync(address)` removes it. The docs show only placeholders for the add and remove responses, so `AddAsync` returns `data` as `JsonElement` and `RemoveAsync` the documented string (`"OK"`); see [API notes](api-notes.md).

### Allowed senders and recipients (`client.AllowedSenders`, `client.AllowedRecipients`)

Both families have `ViewAsync()`, `AddAsync`, `RemoveAsync` and `UpdateAsync` (replace the whole list) and return the whole list every time. Senders carry an `AllowedSendersMode` (`Whitelist`, `Blacklist`, `Disabled`); recipients carry `Enabled`. `update` requires the mode or the flag; `add` and `remove` for recipients take an optional `Enabled`.

```csharp
await client.AllowedRecipients.UpdateAsync(new AllowedRecipientsUpdateRequest
{
    AllowedRecipients = ["qa@example.com", "example.org"],
    Enabled = true, // staging: only these recipients receive mail
});
```

## Subaccounts (`client.Subaccounts`)

Master-account keys only; none of these endpoints takes `subaccount_id`.

| Method | Endpoint | Notes |
| :-- | :-- | :-- |
| `SearchAsync(SubaccountSearchRequest)` | `subaccounts/search` | Filters: `SearchTerms`, `FuzzySearch`, `States` (`SubaccountStateFilter`), `SortDirection`, `PageSize`, `ContinueToken`. |
| `SearchAllAsync(SubaccountSearchRequest)` | `subaccounts/search` | Walks every page by `continue_token`. |
| `AddAsync(SubaccountAddRequest)` | `subaccount/add` (50 per hour) | `FullName` required; `Limit` must be one of the documented plan sizes. |
| `EditAsync(SubaccountEditRequest)` | `subaccount/edit` | `Id` plus the fields to change. |
| `CloseAsync(SubaccountCloseRequest)` | `subaccount/close` | `ApiResponse<string>` with a message. |
| `ReopenAsync(SubaccountReopenRequest)` | `subaccount/reopen` | `ApiResponse<string>`; the docs example is a message like `close` (the schema says an object, see [API notes](api-notes.md)). |

`Subaccount.StateRaw` is the wire value (`Active`), `State` the `SubaccountState`. The `Id` is what `RequestOptions.SubaccountId` and `Smtp2GoClientOptions.DefaultSubaccountId` take.

## Dedicated IPs (`client.DedicatedIps`)

`ViewAsync()` returns the pools (`DedicatedIpPool`: `Id`, `Name`, `IpAddresses`). The `Id` is the `IpPool` of the credential requests.

## SMS (`client.Sms`)

SMS is a paid add-on; `sms/send` needs it enabled on the account.

| Method | Endpoint | Notes |
| :-- | :-- | :-- |
| `SendAsync(SmsSendRequest)` | `sms/send` | `Destination` (1 to 100 numbers with country code, `+` optional), `Content`, optional `Sender` (E.164). Returns `Statuses` (count per status), `TotalSent` and, per the schema, `Messages` with an `SmsStatus` each. No `subaccount_id`. |
| `GetSummaryAsync(SmsSummaryRequest)` | `sms/summary` | Totals and a row per subaccount for a period. |
| `ViewReceivedAsync(SmsReceivedRequest)` | `sms/view-received` | Received messages; `UnixStart`/`UnixEnd` are `[Obsolete]` as the docs deprecate them. |
| `ViewSentAsync(SmsSentRequest)` | `sms/view-sent` | Sent messages with `Status` as free text (`Message discarded`), `Units` and the destination country. |

Date ranges include `StartDate` and exclude `EndDate` (UTC).

```csharp
ApiResponse<SmsSendResult> result = await client.Sms.SendAsync(new SmsSendRequest
{
    Destination = ["+12025550959"],
    Content = "Your verification code is 123456",
});
Console.WriteLine($"{result.Data.TotalSent} sent, queued: {result.Data.Statuses?["queued"]}");
```

## Not in the reference

The 2026-07 changelog mentions an IP allow list (`ip_allow_list/*` with a `type` of SMTP or API); the reference index does not list those pages, so the harvested spec has no operations for them and there is no typed client. Call them through `client.Raw` when SMTP2GO documents them, and re-run the harvester so the contract tests pick the pages up.
