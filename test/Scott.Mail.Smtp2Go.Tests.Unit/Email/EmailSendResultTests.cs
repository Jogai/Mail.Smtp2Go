using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailSendResultTests
{
    private static ApiResponse<EmailSendResult> Read(string fixture)
    {
        return JsonSerializer.Deserialize(Fixture.Read("Email/" + fixture), Smtp2GoJsonContext.Default.ApiResponseEmailSendResult)!;
    }

    [Fact]
    public void Docs_success_example_deserialises()
    {
        ApiResponse<EmailSendResult> response = Read("send-response-ok.json");

        response.RequestId.Should().Be("aa253464-0bd0-467a-b24b-6159dcd7be60");
        response.Data.Succeeded.Should().Be(1);
        response.Data.Failed.Should().Be(0);
        response.Data.Failures.Should().BeEmpty();
        response.Data.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
        response.Data.ScheduleId.Should().BeNull();
        response.Data.IsScheduled.Should().BeFalse();
        response.Data.HasFailures.Should().BeFalse();
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Failed_example_reports_failures()
    {
        ApiResponse<EmailSendResult> response = Read("send-response-failed.json");

        response.Data.Failed.Should().Be(1);
        response.Data.Failures.Should().ContainSingle().Which.Should().Contain("nobody@invalid.example");
        response.Data.HasFailures.Should().BeTrue();
    }

    [Fact]
    public void Scheduled_example_carries_schedule_id_and_no_email_id()
    {
        ApiResponse<EmailSendResult> response = Read("send-response-scheduled.json");

        response.Data.ScheduleId.Should().Be("188262b6-f6cc-4c98-bbe6-84c39d1c0ef4");
        response.Data.EmailId.Should().BeNull();
        response.Data.IsScheduled.Should().BeTrue();
    }

    [Fact]
    public void Fastaccept_shape_has_no_counts()
    {
        ApiResponse<EmailSendResult> response = Read("send-response-fastaccept.json");

        response.Data.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB4");
        response.Data.Succeeded.Should().BeNull();
        response.Data.Failed.Should().BeNull();
        response.Data.Failures.Should().BeNull();
        response.Data.HasFailures.Should().BeFalse();
        response.Data.EnsureAccepted().Should().BeSameAs(response.Data);
    }

    [Fact]
    public void Unknown_fields_land_in_Extra()
    {
        ApiResponse<EmailSendResult> response = JsonSerializer.Deserialize(
            """{"request_id":"r","data":{"email_id":"e","queued_at":"2026-01-01"}}""", Smtp2GoJsonContext.Default.ApiResponseEmailSendResult)!;

        response.Data.Extra.Should().ContainKey("queued_at");
    }

    [Fact]
    public void EnsureAccepted_returns_the_result_when_nothing_failed()
    {
        ApiResponse<EmailSendResult> response = Read("send-response-ok.json");

        response.EnsureAccepted().Should().BeSameAs(response.Data);
        response.Data.EnsureAccepted().Should().BeSameAs(response.Data);
    }

    [Fact]
    public void EnsureAccepted_throws_with_the_failure_list_and_request_id()
    {
        ApiResponse<EmailSendResult> response = Read("send-response-failed.json");

        Func<EmailSendResult> act = () => response.EnsureAccepted();

        Smtp2GoSendException exception = act.Should().Throw<Smtp2GoSendException>().Which;
        exception.Failures.Should().Equal("nobody@invalid.example: recipient domain does not exist");
        exception.RequestId.Should().Be("0f6ed5a1-5c1e-4f0e-9b0c-2f0a4a6b9d11");
        exception.Result.Should().BeSameAs(response.Data);
        exception.Message.Should().Contain("1 failed").And.Contain("1 succeeded").And.Contain("nobody@invalid.example").And.Contain("0f6ed5a1");
        exception.Should().BeAssignableTo<Smtp2GoException>();
    }

    [Fact]
    public void EnsureAccepted_on_the_result_alone_has_no_request_id()
    {
        EmailSendResult result = new() { Failed = 2, Succeeded = 0 };

        Func<EmailSendResult> act = () => result.EnsureAccepted();

        Smtp2GoSendException exception = act.Should().Throw<Smtp2GoSendException>().Which;
        exception.RequestId.Should().BeNull();
        exception.Failures.Should().BeEmpty();
        exception.Message.Should().Be("SMTP2GO reported 2 failed and 0 succeeded recipients.");
    }

    [Fact]
    public void Failures_without_a_count_still_count_as_failures()
    {
        EmailSendResult result = new() { Failures = ["x"] };

        result.HasFailures.Should().BeTrue();
        Func<EmailSendResult> act = () => result.EnsureAccepted();
        act.Should().Throw<Smtp2GoSendException>().Which.Message.Should().StartWith("SMTP2GO reported 1 failed");
    }

    [Fact]
    public void Standard_exception_constructors_have_empty_failures()
    {
        new Smtp2GoSendException().Failures.Should().BeEmpty();
        new Smtp2GoSendException("m").Failures.Should().BeEmpty();
        new Smtp2GoSendException("m", new InvalidOperationException()).Failures.Should().BeEmpty();
        new Smtp2GoSendException("m", null!, null, null).Failures.Should().BeEmpty();
    }
}
