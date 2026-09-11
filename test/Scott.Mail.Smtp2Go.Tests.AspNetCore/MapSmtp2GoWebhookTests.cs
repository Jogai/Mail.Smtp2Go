using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Tests.AspNetCore;

public class MapSmtp2GoWebhookTests
{
    /// <summary>Expected kind per fixture stem, mirroring the parser's own fixture sweep.</summary>
    private static readonly Dictionary<string, WebhookEventKind> s_expectedKinds = new(StringComparer.Ordinal)
    {
        ["processed"] = WebhookEventKind.EmailProcessed,
        ["delivered"] = WebhookEventKind.EmailDelivered,
        ["bounce"] = WebhookEventKind.EmailBounce,
        ["bounce-hard"] = WebhookEventKind.EmailBounce,
        ["bounce-soft"] = WebhookEventKind.EmailBounce,
        ["open"] = WebhookEventKind.EmailOpen,
        ["opened"] = WebhookEventKind.EmailOpen,
        ["click"] = WebhookEventKind.EmailClick,
        ["clicked"] = WebhookEventKind.EmailClick,
        ["spam"] = WebhookEventKind.EmailSpam,
        ["spam_complaint"] = WebhookEventKind.EmailSpam,
        ["unsubscribe"] = WebhookEventKind.EmailUnsubscribe,
        ["unsubscribed"] = WebhookEventKind.EmailUnsubscribe,
        ["resubscribe"] = WebhookEventKind.EmailResubscribe,
        ["reject"] = WebhookEventKind.EmailReject,
        ["sms_sending"] = WebhookEventKind.SmsSending,
        ["sms_submitted"] = WebhookEventKind.SmsSubmitted,
        ["sms_delivered"] = WebhookEventKind.SmsDelivered,
        ["sms_failed"] = WebhookEventKind.SmsFailed,
        ["sms_rejected"] = WebhookEventKind.SmsRejected,
        ["unknown_future"] = WebhookEventKind.Unknown,
    };

    public static TheoryData<string> Fixtures()
    {
        TheoryData<string> data = [];
        foreach (string folder in new[] { "Docs", "Live" })
        {
            foreach (string file in Directory.GetFiles(Fixture.PathOf("Webhooks/" + folder)).Order(StringComparer.Ordinal))
            {
                data.Add(folder + "/" + Path.GetFileName(file));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Every_fixture_posted_with_its_media_type_reaches_the_handler_as_the_expected_kind(string fixture)
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostFixtureAsync(fixture);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        WebhookEvent received = host.Received.Should().ContainSingle().Subject;
        received.Kind.Should().Be(s_expectedKinds[Path.GetFileNameWithoutExtension(fixture)]);
    }

    [Fact]
    public async Task Json_body_is_parsed_into_the_typed_event()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostFixtureAsync("Live/bounce-hard.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EmailBounceEvent bounce = host.Received.Should().ContainSingle().Which.Should().BeOfType<EmailBounceEvent>().Subject;
        bounce.BounceType.Should().Be(BounceType.Hard);
        bounce.Recipient.Should().Be("invalid@nonexistent.com");
        bounce.Extra.Should().BeNull();
    }

    [Fact]
    public async Task Form_urlencoded_body_is_parsed_into_the_typed_event()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostFixtureAsync("Live/bounce-hard.form");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EmailBounceEvent bounce = host.Received.Should().ContainSingle().Which.Should().BeOfType<EmailBounceEvent>().Subject;
        bounce.Host.Should().Be("gmail-smtp-in.l.google.com [209.85.233.26]");
        bounce.Context.Should().Be("RCPT TO:<invalid@nonexistent.com>");
    }

    [Fact]
    public async Task Multipart_form_data_body_is_parsed_into_the_typed_event()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostFixtureAsMultipartAsync("Live/clicked.form");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EmailClickEvent click = host.Received.Should().ContainSingle().Which.Should().BeOfType<EmailClickEvent>().Subject;
        click.EventRaw.Should().Be("clicked");
        click.ClickUrl.Should().Be("https://alos.app/dashboard");
        click.Link.Should().Be("https://track.smtp2go.com/abc123");
    }

    [Fact]
    public async Task Json_with_a_charset_parameter_and_a_json_suffix_media_type_are_accepted()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, (_, _) => Task.CompletedTask)
            .WithOptions(options => options.AllowedContentTypes.Add("application/vnd.smtp2go+json")));
        string body = Fixture.Read("Webhooks/Docs/delivered.json");

        using HttpResponseMessage withCharset = await host.PostAsync(body, "application/json");
        using HttpResponseMessage withSuffix = await host.PostAsync(new StringContent(body, Encoding.UTF8, "application/vnd.smtp2go+json"));

        withCharset.StatusCode.Should().Be(HttpStatusCode.OK);
        withSuffix.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData(null)]
    public async Task Other_or_missing_media_types_get_415_without_reaching_the_handler(string? contentType)
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostAsync(Fixture.Read("Webhooks/Docs/delivered.json"), contentType);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        host.Received.Should().BeEmpty();
        host.Logs.Smtp2Go.Should().ContainSingle(entry => entry.EventId.Id == Smtp2GoWebhookEventIds.UnsupportedMediaType)
            .Which.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task Allowed_content_types_can_be_narrowed_to_json_only()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
            .WithOptions(options =>
            {
                options.AllowedContentTypes.Clear();
                options.AllowedContentTypes.Add("application/json");
            }));

        using HttpResponseMessage form = await host.PostFixtureAsync("Docs/delivered.form");
        using HttpResponseMessage json = await host.PostFixtureAsync("Docs/delivered.json");

        form.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        json.StatusCode.Should().Be(HttpStatusCode.OK);
        host.Received.Should().ContainSingle();
    }

    [Fact]
    public async Task Body_over_the_limit_with_a_content_length_gets_413()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
            .WithOptions(options => options.MaxBodyBytes = 64));
        string body = Fixture.Read("Webhooks/Docs/delivered.json");
        body.Length.Should().BeGreaterThan(64);

        using HttpResponseMessage response = await host.PostAsync(body, "application/json");

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        host.Received.Should().BeEmpty();
        host.Logs.Smtp2Go.Should().ContainSingle(entry => entry.EventId.Id == Smtp2GoWebhookEventIds.PayloadTooLarge);
    }

    [Fact]
    public async Task Chunked_body_over_the_limit_gets_413_while_reading()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
            .WithOptions(options => options.MaxBodyBytes = 64));
        byte[] body = Encoding.UTF8.GetBytes(Fixture.Read("Webhooks/Docs/delivered.json"));
        using StreamContent content = new(new NonSeekableStream(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using HttpResponseMessage response = await host.PostAsync(content, request => request.Headers.TransferEncodingChunked = true);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        host.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task Body_exactly_at_the_limit_is_accepted()
    {
        string body = Fixture.Read("Webhooks/Docs/delivered.json");
        int length = Encoding.UTF8.GetByteCount(body);
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
            .WithOptions(options => options.MaxBodyBytes = length));

        using HttpResponseMessage response = await host.PostAsync(body, "application/json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("{ not json", "application/json")]
    [InlineData("[1, 2, 3]", "application/json")]
    public async Task Malformed_body_gets_400(string body, string contentType)
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostAsync(body, contentType);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        host.Received.Should().BeEmpty();
        host.Logs.Smtp2Go.Should().ContainSingle(entry => entry.EventId.Id == Smtp2GoWebhookEventIds.PayloadInvalid)
            .Which.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_is_not_mapped()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(WebhookTestHost.Path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Handler_exception_returns_500_by_default_and_is_logged_as_an_error()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, (_, _) => throw new InvalidOperationException("boom")));

        using HttpResponseMessage response = await host.PostFixtureAsync("Docs/delivered.json");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        CapturingLoggerProvider.LogEntry entry = host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.HandlerFailed).Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeOfType<InvalidOperationException>();
        entry.Message.Should().Contain("EmailDelivered").And.Contain("500");
        host.Logs.Smtp2Go.Should().NotContain(e => e.EventId.Id == Smtp2GoWebhookEventIds.CallbackHandled);
    }

    [Fact]
    public async Task Handler_exception_returns_the_configured_status()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, (_, _) => throw new InvalidOperationException("boom"))
            .WithOptions(options => options.ReturnStatusOnHandlerError = StatusCodes.Status200OK));

        using HttpResponseMessage response = await host.PostFixtureAsync("Docs/delivered.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.HandlerFailed).Which.Message.Should().Contain("200");
    }

    [Fact]
    public async Task Successful_callback_logs_received_and_handled()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostFixtureAsync("Docs/bounce.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CapturingLoggerProvider.LogEntry> entries = host.Logs.Smtp2Go.ToList();
        entries.Select(e => e.EventId.Id).Should().ContainInOrder(Smtp2GoWebhookEventIds.CallbackReceived, Smtp2GoWebhookEventIds.CallbackHandled);
        entries.Should().OnlyContain(e => !e.Message.Contains('@'), "log lines must not carry recipient addresses");
    }

    [Fact]
    public async Task Known_custom_headers_are_passed_to_the_parser()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
            .WithOptions(options => options.KnownCustomHeaders.Add("X-Customer-Id")));

        using HttpResponseMessage response = await host.PostAsync("""{"event":"delivered","rcpt":"bob@example.org","X-Customer-Id":"42"}""", "application/json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EmailDeliveredEvent delivered = host.Received.Should().ContainSingle().Which.Should().BeOfType<EmailDeliveredEvent>().Subject;
        delivered.CustomHeaders.Should().ContainKey("X-Customer-Id").WhoseValue.Should().Be("42");
        delivered.Extra.Should().BeNull();
    }

    [Fact]
    public async Task Options_registered_in_the_container_seed_every_endpoint_and_WithOptions_only_changes_its_own()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services => services.Configure<Smtp2GoWebhookOptions>(options => options.MaxBodyBytes = 64),
            (test, app) =>
            {
                app.MapSmtp2GoWebhook("/small", test.RecordAsync);
                return app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).WithOptions(options => options.MaxBodyBytes = Smtp2GoWebhookOptions.DefaultMaxBodyBytes);
            });
        using StringContent small = new(Fixture.Read("Webhooks/Docs/delivered.json"), Encoding.UTF8, "application/json");

        using HttpResponseMessage limited = await host.Client.PostAsync("/small", small, TestContext.Current.CancellationToken);
        using HttpResponseMessage widened = await host.PostFixtureAsync("Docs/delivered.json");

        limited.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        widened.StatusCode.Should().Be(HttpStatusCode.OK);
        host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Smtp2GoWebhookOptions>>().Value.MaxBodyBytes.Should().Be(64, "the endpoint edits a copy");
    }

    [Fact]
    public async Task Endpoint_carries_the_marker_metadata_and_is_excluded_from_descriptions()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        Endpoint endpoint = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.Should().ContainSingle().Subject;

        Smtp2GoWebhookMetadata metadata = endpoint.Metadata.GetMetadata<Smtp2GoWebhookMetadata>()!;
        metadata.Should().NotBeNull();
        metadata.Pattern.Should().Be(WebhookTestHost.Path);
        metadata.UsesHandlerDispatch.Should().BeFalse();
        endpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription.Should().BeTrue();
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Should().Equal("POST");
    }

    [Fact]
    public async Task Other_conventions_still_apply_through_the_builder()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).WithName("smtp2go-webhook"));

        Endpoint endpoint = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.Should().ContainSingle().Subject;

        endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName.Should().Be("smtp2go-webhook");
    }

    [Theory]
    [InlineData(0L, 500)]
    [InlineData(1024L, 199)]
    [InlineData(1024L, 600)]
    public async Task Invalid_options_fail_while_mapping(long maxBodyBytes, int status)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        await using WebApplication app = builder.Build();

        Action act = () => app.MapSmtp2GoWebhook(WebhookTestHost.Path, (_, _) => Task.CompletedTask).WithOptions(options =>
        {
            options.MaxBodyBytes = maxBodyBytes;
            options.ReturnStatusOnHandlerError = status;
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("Smtp2GoWebhookOptions.*");
    }

    [Fact]
    public void Options_defaults_match_the_documentation()
    {
        Smtp2GoWebhookOptions options = new();

        options.MaxBodyBytes.Should().Be(1024 * 1024);
        options.AllowedContentTypes.Should().Equal("application/json", "application/x-www-form-urlencoded", "multipart/form-data");
        options.ReturnStatusOnHandlerError.Should().Be(500);
        options.KnownCustomHeaders.Should().BeEmpty();
    }

    /// <summary>A stream without a known length, so HttpClient sends the body chunked and the endpoint cannot check Content-Length up front.</summary>
    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data, writable: false);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
