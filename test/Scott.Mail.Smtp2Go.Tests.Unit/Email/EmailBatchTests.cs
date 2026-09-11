using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailBatchTests
{
    internal static EmailBatchRequest Mixed()
    {
        return new EmailBatchRequest
        {
            Emails =
            [
                new EmailSendRequest { Sender = "alice@example.com", To = ["bob@example.com"], Subject = "Now", TextBody = "Sent immediately." },
                new EmailSendRequest
                {
                    Sender = "alice@example.com",
                    To = ["carol@example.com", "dave@example.com"],
                    TemplateId = "reminder",
                    TemplateData = new Dictionary<string, object?> { ["when"] = "tomorrow" },
                    Schedule = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero),
                },
            ],
        };
    }

    [Fact]
    public void Mixed_batch_serialises_immediate_and_scheduled_items()
    {
        Golden.AssertMatches(JsonSerializer.Serialize(Mixed(), Smtp2GoJsonContext.Default.EmailBatchRequest), "batch-mixed.json");
    }

    [Fact]
    public void Docs_response_deserialises_to_items_in_order()
    {
        ApiResponse<IReadOnlyList<EmailBatchItem>> response = JsonSerializer.Deserialize(Fixture.Read("Email/batch-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListEmailBatchItem)!;

        response.Data.Should().HaveCount(2);
        response.Data[0].EmailId.Should().Be("123456-1234-12");
        response.Data[0].IsScheduled.Should().BeFalse();
        response.Data[1].ScheduleId.Should().Be("188262b6-f6cc-4c98-bbe6-84c39d1c0ef4");
        response.Data[1].IsScheduled.Should().BeTrue();
        response.Data[1].EmailId.Should().BeNull();
    }

    [Fact]
    public void Unknown_item_fields_land_in_Extra()
    {
        ApiResponse<IReadOnlyList<EmailBatchItem>> response = JsonSerializer.Deserialize(
            """{"request_id":"r","data":[{"email_id":"e","succeeded":1}]}""", Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListEmailBatchItem)!;

        response.Data[0].Extra.Should().ContainKey("succeeded");
    }

    [Fact]
    public async Task SendBatchAsync_posts_to_email_batch_and_parses_the_list()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/batch", HttpStatusCode.OK, Fixture.Read("Email/batch-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultFastAccept = true);

        ApiResponse<IReadOnlyList<EmailBatchItem>> response = await client.Email.SendBatchAsync(Mixed());

        handler.LastRequest.Endpoint!.Path.Should().Be("email/batch");
        handler.LastRequest.Uri.AbsolutePath.Should().EndWith("/v3/email/batch");
        Golden.AssertMatches(handler.LastRequest.Body!, "batch-mixed.json");
        handler.LastRequest.Body.Should().NotContain("fastaccept", because: "the batch endpoint does not document fastaccept per email");
        response.Data.Select(i => i.EmailId ?? i.ScheduleId).Should().Equal("123456-1234-12", "188262b6-f6cc-4c98-bbe6-84c39d1c0ef4");
    }

    [Fact]
    public async Task Null_request_is_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        Func<Task> act = () => client.Email.SendBatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
