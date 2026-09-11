namespace Scott.Mail.Smtp2Go.Tests.Integration.Webhooks;

/// <summary>webhook/add, view, edit, remove against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxWebhookTests
{
    [Fact]
    public async Task Add_view_edit_remove_round_trip()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;
        string url = $"https://example.com/hooks/scott-mail-smtp2go-{Guid.NewGuid():N}";

        ApiResponse<Webhook> added = await client.Webhooks.AddAsync(new WebhookAddRequest
        {
            Url = url,
            Events = [WebhookEmailEvent.Processed, WebhookEmailEvent.Delivered],
            OutputFormat = WebhookOutputFormat.Json,
            Headers = ["X-Test-Run"],
        }, cancellationToken: ct);
        long id = added.Data.Id ?? throw new InvalidOperationException("webhook/add returned no id.");
        try
        {
            added.Data.Url.Should().Be(url);
            added.Data.Events.Should().BeEquivalentTo([WebhookEmailEvent.Processed, WebhookEmailEvent.Delivered]);
            added.Data.OutputFormat.Should().Be(WebhookOutputFormat.Json);

            ApiResponse<IReadOnlyList<Webhook>> listed = await client.Webhooks.ViewAsync(cancellationToken: ct);
            listed.Data.Should().Contain(w => w.Id == id && w.Url == url);

            ApiResponse<Webhook> edited = await client.Webhooks.EditAsync(new WebhookEditRequest { Id = id, Events = [WebhookEmailEvent.Bounce], OutputFormat = WebhookOutputFormat.Form }, cancellationToken: ct);
            edited.Data.Id.Should().Be(id);
            edited.Data.Events.Should().BeEquivalentTo([WebhookEmailEvent.Bounce]);

            ApiResponse<Webhook> upserted = await client.Webhooks.AddOrUpdateByUrlAsync(new WebhookAddRequest { Url = url, Events = [WebhookEmailEvent.Open] }, cancellationToken: ct);
            upserted.Data.Id.Should().Be(id, because: "the same url must edit, not add");
        }
        finally
        {
            ApiResponse<Webhook> removed = await client.Webhooks.RemoveAsync(id, cancellationToken: ct);
            removed.RequestId.Should().NotBeNullOrWhiteSpace();
        }
    }
}
