# Reporting: statistics and activity

## Statistics (`client.Stats`)

Six endpoints, one method each. The summary, bounce, spam and unsubscribe reports take only an optional `username` (an SMTP user, API key or authenticated IP); they cover the billing cycle (summary) or the last 30 days (the rest), and the API documents no date range for them, so none is offered. `email_cycle` takes nothing. Only `email_history` takes a date range, a grouping and a subaccount filter.

| Method | Endpoint | Returns |
| :-- | :-- | :-- |
| `GetSummaryAsync(username?)` | `stats/email_summary` | `EmailSummary`: cycle figures, `EmailCount`, `Rejects`, hard/soft bounces, spam, unsubscribes, opens, clicks, percentages. `BounceRejects` and `SpamRejects` are `[Obsolete]`; `Rejects` replaces them. |
| `GetCycleAsync()` | `stats/email_cycle` | `EmailCycle`: `CycleStart`, `CycleEnd`, `CycleUsed`, `CycleRemaining`, `CycleMax`. |
| `GetQuotaAsync()` | `stats/email_cycle` | `Quota` (`Used`, `Remaining`, `Max`, `CycleStart`, `CycleEnd`, `RequestId`, `UsedFraction`): the usual "are we about to run out" check. |
| `GetBouncesAsync(username?)` | `stats/email_bounces` | `EmailBounces` |
| `GetSpamAsync(username?)` | `stats/email_spam` | `EmailSpam` |
| `GetUnsubscribesAsync(username?)` | `stats/email_unsubs` | `EmailUnsubscribes` |
| `GetHistoryAsync(EmailHistoryRequest)` | `stats/email_history` | `EmailHistory`: rows per sender address, username, domain or subaccount (`GroupBy`), with totals. |

```csharp
Quota quota = await client.Stats.GetQuotaAsync();
if (quota.UsedFraction > 0.9)
{
    Console.WriteLine($"{quota.Remaining} of {quota.Max} left until {quota.CycleEnd:d}");
}

ApiResponse<EmailHistory> history = await client.Stats.GetHistoryAsync(new EmailHistoryRequest
{
    GroupBy = EmailHistoryGroupBy.Domain,
    StartDate = DateTimeOffset.UtcNow.AddDays(-7),
});
foreach (EmailHistoryEntry row in history.Data.History ?? [])
{
    Console.WriteLine($"{row.Domain}: {row.Used} sent, {row.BouncePercent?.Value}% bounced");
}
```

Percentages are `Percentage` values (`Value` as `decimal?`, `Raw` as the wire text): the summary endpoints return them as strings (`"0.00"`), `email_history` as numbers, and the type reads both. Timestamps such as `2022-11-01 00:00:00+00:00` are read into `DateTimeOffset`.

## Activity search (`client.Activity`)

`activity/search` returns every event (processed, delivered, soft/hard bounced, rejected, spam, unsubscribed, resubscribed, opened, clicked) of every email, with filters for dates, free text, email id, subject, sender, recipient, usernames, subaccounts and event types, plus `IncludeHeaders`, `CustomHeaders` and `Region`. Pages are at most 1,000 events (`Limit`, default 100) and continue with `continue_token`.

```csharp
ApiResponse<ActivitySearchResult> page = await client.Activity.SearchAsync(new ActivitySearchRequest
{
    StartDate = DateTimeOffset.UtcNow.AddDays(-1),
    EventTypes = [ActivityEventType.HardBounced, ActivityEventType.SoftBounced],
    Limit = 500,
});
Console.WriteLine($"{page.Data.TotalEvents} bounces, {page.Data.Events?.Count} on this page");

await foreach (ActivityEvent evt in client.Activity.SearchAllAsync(new ActivitySearchRequest { SearchEmailId = "1u0SwL-B9zBpi9ffUq-JAB2" }))
{
    Console.WriteLine($"{evt.Date:u} {evt.Event} ({evt.EventRaw}) {evt.Recipient} {evt.SmtpResponse}");
}
```

`ActivityEvent.EventRaw` is the wire string and `Event` its `ActivityEventType` (`Unknown` for names this library does not know). Activity uses its own names (`soft-bounced`, `hard-bounced`, `opened`), which is why it has a separate enum from the webhook callbacks. `EmailClient` and `Metadata` are kept as raw `JsonElement`s because the docs do not describe their shape; everything else is typed, and unmodelled fields go to `Extra`.

### Rate limit

The endpoint allows 60 requests per minute. The client throttles itself with a token bucket (60 tokens, one refilled per second) shared by every call through the same `Smtp2GoClient`, so `SearchAllAsync` over many pages slows down instead of getting 429s. Turn it off with `Smtp2GoClientOptions.ClientSideRateLimiting = false` when a resilience pipeline (the dependency injection package) handles limits; the API's 429 responses still surface as `Smtp2GoRateLimitException` either way.

## Paging

`SearchAllAsync` (activity, templates, archive) and `ViewAllAsync` (suppressions) return `IAsyncEnumerable<T>`: one API call per page as the enumeration advances, starting from the request's `ContinueToken` and stopping when the server returns a null, empty or unchanged token. Page-level data (`TotalEvents`, `TotalCount`, `TotalResults`, `EmailCount`) is only on the single-page methods.
