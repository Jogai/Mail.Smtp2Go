using Microsoft.CodeAnalysis;

namespace Scott.Mail.Smtp2Go.Tests.Generators;

public class EndpointTablePipelineTests
{
    private const string Table = """
        using Scott.Mail.Smtp2Go.Generation;
        namespace Scott.Mail.Smtp2Go.Transport;

        [EndpointTraits("*/search", Idempotent = true)]
        [EndpointTraits("*/view", Idempotent = true)]
        [EndpointTraits("activity/search", RateLimit = RateLimitClass.ActivitySearch)]
        [EndpointTraits("email/send", MaxBodyBytes = Endpoint.EmailMaxBodyBytes)]
        public static partial class EndpointTable
        {
        }
        """;

    [Fact]
    public void Emits_one_row_per_operation_with_the_traits_applied()
    {
        GeneratorRun run = GeneratorHost.Run(Table, Stubs.Snapshot);

        run.Diagnostics.Should().BeEmpty();
        run.CompileErrors().Should().BeEmpty();
        string seed = run.Source("EndpointTable.Seed.g.cs");
        seed.Should().Contain("""table.Add(("email/send", "POST"), new global::Scott.Mail.Smtp2Go.Transport.Endpoint("email/send", global::System.Net.Http.HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: false, global::Scott.Mail.Smtp2Go.Transport.RateLimitClass.None, 52428800L));""");
        seed.Should().Contain("""table.Add(("activity/search", "POST"), new global::Scott.Mail.Smtp2Go.Transport.Endpoint("activity/search", global::System.Net.Http.HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, global::Scott.Mail.Smtp2Go.Transport.RateLimitClass.ActivitySearch, 1048576L));""");
        seed.Should().Contain("""table.Add(("api_keys/edit", "PATCH"), new global::Scott.Mail.Smtp2Go.Transport.Endpoint("api_keys/edit", global::Scott.Mail.Smtp2Go.Transport.Endpoint.Patch, Idempotent: false, AcceptsSubaccountId: true,""");
        seed.Should().Contain("""("webhook/view", global::System.Net.Http.HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true,""");
        seed.Split("table.Add(").Should().HaveCount(6, because: "five operations are documented");
    }

    [Fact]
    public void A_pattern_that_matches_nothing_is_an_error()
    {
        GeneratorRun run = GeneratorHost.Run(Table.Replace("\"*/view\"", "\"*/vieww\"", StringComparison.Ordinal), Stubs.Snapshot);

        Diagnostic diagnostic = run.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("SMTP2GO003");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Should().Contain("*/vieww");
        diagnostic.Location.GetLineSpan().StartLinePosition.Line.Should().Be(4, because: "the attribute is on the fifth line of Test.cs");
        run.Sources.Keys.Should().Contain("EndpointTable.Seed.g.cs", because: "the table is still emitted so the other errors show");
    }

    [Fact]
    public void A_missing_snapshot_is_an_error()
    {
        GeneratorRun run = GeneratorHost.Run(Table);

        run.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("SMTP2GO001");
        run.Sources.Keys.Should().NotContain("EndpointTable.Seed.g.cs");
    }

    [Fact]
    public void An_unreadable_snapshot_is_an_error_with_the_reason()
    {
        GeneratorRun run = GeneratorHost.Run(Table, "{ \"operations\": 1 }");

        Diagnostic diagnostic = run.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("SMTP2GO002");
        diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Should().Contain("no 'operations' array");
    }

    [Fact]
    public void A_file_with_another_name_is_not_the_snapshot()
    {
        GeneratorRun run = GeneratorHost.Run(Table, Stubs.Snapshot, snapshotFileName: "known-unmodelled.json");

        run.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("SMTP2GO001");
    }

    [Fact]
    public void The_class_must_be_partial()
    {
        GeneratorRun run = GeneratorHost.Run(Table.Replace("static partial class", "static class", StringComparison.Ordinal), Stubs.Snapshot);

        run.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("SMTP2GO004");
    }
}

public class ClientPipelineTests
{
    private const string Client = """
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Scott.Mail.Smtp2Go.Generation;
        using Scott.Mail.Smtp2Go.Transport;

        namespace Scott.Mail.Smtp2Go;

        [Smtp2GoEndpoint("email/send")] public sealed record EmailSendRequest;
        public sealed record EmailSendResult;
        [Smtp2GoEndpoint("webhook/view")] public sealed record Webhook;
        [Smtp2GoEndpoint("api_keys/edit")] public sealed record ApiKeyEditRequest;
        [Smtp2GoEndpoint("api_keys/edit")] public sealed record ApiKeyPatchRequest;
        public sealed record ApiKey;
        public sealed record Unmapped;

        public interface IProbeClient
        {
            Task<ApiResponse<EmailSendResult>> SendAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
            Task<ApiResponse<IReadOnlyList<Webhook>>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);
            Task<ApiResponse<IReadOnlyList<ApiKey>>> EditAsync(ApiKeyEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
            Task<ApiResponse<IReadOnlyList<ApiKey>>> PatchAsync(ApiKeyPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
            Task<ApiResponse<ApiKey>> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default);
            Task<ApiResponse<Unmapped>> UnmappedAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);
            Task<ApiResponse<EmailSendResult>> HandWrittenAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
        }

        [GeneratedClient]
        internal sealed partial class ProbeClient(Smtp2GoConnection connection) : IProbeClient
        {
            //TWINS//
            public Task<ApiResponse<ApiKey>> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
                => Task.FromResult(new ApiResponse<ApiKey>());

            public Task<ApiResponse<Unmapped>> UnmappedAsync(RequestOptions? options = null, CancellationToken cancellationToken = default)
                => Task.FromResult(new ApiResponse<Unmapped>());

            public Task<ApiResponse<EmailSendResult>> HandWrittenAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
                => Task.FromResult(new ApiResponse<EmailSendResult>());
        }
        """;

    private const string Twins = """
            private static readonly Endpoint s_edit = EndpointTable.Get("api_keys/edit", System.Net.Http.HttpMethod.Post);
            private static readonly Endpoint s_patch = EndpointTable.Get("api_keys/edit", Endpoint.Patch);

            public Task<ApiResponse<IReadOnlyList<ApiKey>>> EditAsync(ApiKeyEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
                => connection.SendAsync<ApiKeyEditRequest, IReadOnlyList<ApiKey>>(s_edit, request, options, cancellationToken);

            public Task<ApiResponse<IReadOnlyList<ApiKey>>> PatchAsync(ApiKeyPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
                => connection.SendAsync<ApiKeyPatchRequest, IReadOnlyList<ApiKey>>(s_patch, request, options, cancellationToken);
        """;

    [Fact]
    public void Generates_the_request_and_no_request_shapes_and_leaves_the_rest_alone()
    {
        GeneratorRun run = GeneratorHost.Run(Client.Replace("//TWINS//", Twins, StringComparison.Ordinal), Stubs.Snapshot);

        run.Diagnostics.Should().BeEmpty();
        run.CompileErrors().Should().BeEmpty();
        string source = run.Source("ProbeClient.PassThrough.g.cs");
        source.Should().Contain("""EndpointTable.Get("email/send", global::System.Net.Http.HttpMethod.Post)""");
        source.Should().Contain("public global::System.Threading.Tasks.Task<global::Scott.Mail.Smtp2Go.ApiResponse<global::Scott.Mail.Smtp2Go.EmailSendResult>> SendAsync(global::Scott.Mail.Smtp2Go.EmailSendRequest request, global::Scott.Mail.Smtp2Go.RequestOptions? options = null, global::System.Threading.CancellationToken cancellationToken = default)");
        source.Should().Contain("global::Scott.Mail.Smtp2Go.Argument.ThrowIfNull(request);");
        source.Should().Contain("return connection.SendAsync<global::Scott.Mail.Smtp2Go.EmailSendRequest, global::Scott.Mail.Smtp2Go.EmailSendResult>(s_sendAsyncEndpoint, request, options, cancellationToken);");
        source.Should().Contain("return connection.SendAsync<global::System.Text.Json.JsonElement, global::System.Collections.Generic.IReadOnlyList<global::Scott.Mail.Smtp2Go.Webhook>>(s_viewAsyncEndpoint, default, options, cancellationToken);");
        source.Should().NotContain("EditAsync").And.NotContain("PatchAsync").And.NotContain("RemoveAsync").And.NotContain("UnmappedAsync").And.NotContain("HandWrittenAsync");
    }

    [Fact]
    public void A_twin_path_is_never_guessed()
    {
        // Without the hand-written Edit/Patch the generator still refuses the ambiguous path: the compiler reports the missing members instead.
        GeneratorRun run = GeneratorHost.Run(Client.Replace("//TWINS//", string.Empty, StringComparison.Ordinal), Stubs.Snapshot);

        run.Diagnostics.Should().BeEmpty();
        run.Source("ProbeClient.PassThrough.g.cs").Should().NotContain("api_keys/edit");
        run.CompileErrors().Select(d => d.Id).Distinct().Should().Equal("CS0535");
    }

    [Fact]
    public void A_class_without_a_connection_is_an_error()
    {
        GeneratorRun run = GeneratorHost.Run(Client.Replace("//TWINS//", Twins, StringComparison.Ordinal).Replace("ProbeClient(Smtp2GoConnection connection)", "ProbeClient()", StringComparison.Ordinal), Stubs.Snapshot);

        run.Diagnostics.Should().ContainSingle(d => d.Id == "SMTP2GO005");
    }
}

public class EnumWireNamesPipelineTests
{
    private const string Enums = """
        using System.Text.Json.Serialization;
        using Scott.Mail.Smtp2Go.Generation;
        using Scott.Mail.Smtp2Go.Json.Converters;

        namespace Scott.Mail.Smtp2Go
        {
            [JsonConverter(typeof(TolerantEnumConverter<Probe>))]
            public enum Probe { Unknown = 0, SoftBounced = 1, [JsonStringEnumMemberName("opt-out")] OptOut = 2 }

            public enum Plain { A, B }

            [JsonConverter(typeof(TolerantEnumConverter<Probe>))]
            public enum WrongConverter { A }
        }

        namespace Scott.Mail.Smtp2Go.Json
        {
            [GeneratedEnumWireNames]
            internal static partial class EnumWireNames { }
        }
        """;

    [Fact]
    public void Lists_only_enums_read_with_their_own_tolerant_converter()
    {
        GeneratorRun run = GeneratorHost.Run(Enums);

        run.Diagnostics.Should().BeEmpty();
        run.CompileErrors().Should().BeEmpty();
        string source = run.Source("EnumWireNames.g.cs");
        source.Should().Contain("""new(global::Scott.Mail.Smtp2Go.Probe.SoftBounced, "SoftBounced", "soft_bounced"),""");
        source.Should().Contain("""new(global::Scott.Mail.Smtp2Go.Probe.OptOut, "OptOut", "opt-out"),""");
        source.Should().Contain("if (typeof(TEnum) == typeof(global::Scott.Mail.Smtp2Go.Probe))");
        source.Should().NotContain("Plain").And.NotContain("WrongConverter");
        source.Should().Contain("return null;");
    }
}

public class WebhookPipelineTests
{
    private const string Events = """
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Scott.Mail.Smtp2Go.Generation;

        namespace Scott.Mail.Smtp2Go.Webhooks;

        public abstract record WebhookEvent { public Dictionary<string, string>? Extra { get; init; } }
        public abstract record EmailWebhookEvent : WebhookEvent { public Dictionary<string, string>? CustomHeaders { get; init; } }
        public record EmailOpenEvent : EmailWebhookEvent;
        public sealed record EmailClickEvent : EmailOpenEvent;
        public sealed record EmailBounceEvent : EmailWebhookEvent;
        public sealed record SmsStatusEvent : WebhookEvent;
        public sealed record UnknownWebhookEvent : WebhookEvent;

        internal static partial class Factory
        {
            [GeneratedWebhookUpdate]
            private static partial WebhookEvent WithExtras(WebhookEvent result, Dictionary<string, string>? extra, Dictionary<string, string>? customHeaders);
            public static WebhookEvent Probe(WebhookEvent e) => WithExtras(e, null, null);
        }

        internal sealed partial class Dispatcher
        {
            [GeneratedWebhookDispatch]
            private partial Task<int> DispatchByTypeAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
            private static IEnumerable<System.Func<CancellationToken, Task>> Handlers<TEvent>(TEvent webhookEvent) where TEvent : WebhookEvent => [];
            private static Task<int> RunAsync(CancellationToken cancellationToken, params IEnumerable<System.Func<CancellationToken, Task>>[] groups) => Task.FromResult(groups.Length);
            public Task<int> Probe(WebhookEvent e) => DispatchByTypeAsync(e, default);
        }
        """;

    [Fact]
    public void Dispatch_lists_every_concrete_event_most_derived_first_with_its_base_chain()
    {
        GeneratorRun run = GeneratorHost.Run(Events);

        run.Diagnostics.Should().BeEmpty();
        run.CompileErrors().Should().BeEmpty();
        string source = run.Source("Dispatcher.Dispatch.g.cs");
        string[] arms = source.Split('\n').Where(l => l.Contains(" e => RunAsync(", StringComparison.Ordinal)).Select(l => l.Trim()).ToArray();
        arms.Select(a => a.Split(' ')[0]).Should().Equal(
            "global::Scott.Mail.Smtp2Go.Webhooks.EmailClickEvent",
            "global::Scott.Mail.Smtp2Go.Webhooks.EmailBounceEvent",
            "global::Scott.Mail.Smtp2Go.Webhooks.EmailOpenEvent",
            "global::Scott.Mail.Smtp2Go.Webhooks.SmsStatusEvent",
            "global::Scott.Mail.Smtp2Go.Webhooks.UnknownWebhookEvent");
        arms[0].Should().Be("global::Scott.Mail.Smtp2Go.Webhooks.EmailClickEvent e => RunAsync(cancellationToken, Handlers<global::Scott.Mail.Smtp2Go.Webhooks.EmailClickEvent>(e), Handlers<global::Scott.Mail.Smtp2Go.Webhooks.EmailOpenEvent>(e), Handlers<global::Scott.Mail.Smtp2Go.Webhooks.EmailWebhookEvent>(e), Handlers<global::Scott.Mail.Smtp2Go.Webhooks.WebhookEvent>(e)),");
        source.Should().Contain("_ => RunAsync(cancellationToken, Handlers<global::Scott.Mail.Smtp2Go.Webhooks.WebhookEvent>(webhookEvent)),");
    }

    [Fact]
    public void Update_covers_the_records_that_have_every_property()
    {
        GeneratorRun run = GeneratorHost.Run(Events);

        string source = run.Source("Factory.Update.g.cs");
        source.Should().Contain("global::Scott.Mail.Smtp2Go.Webhooks.EmailClickEvent value => value with { Extra = extra, CustomHeaders = customHeaders },");
        source.Should().Contain("global::Scott.Mail.Smtp2Go.Webhooks.EmailOpenEvent value => value with");
        source.Should().Contain("global::Scott.Mail.Smtp2Go.Webhooks.EmailBounceEvent value => value with");
        source.Should().NotContain("SmsStatusEvent").And.NotContain("UnknownWebhookEvent", because: "neither has CustomHeaders");
        source.Should().Contain("_ => result,");
        source.IndexOf("EmailClickEvent", StringComparison.Ordinal).Should().BeLessThan(source.IndexOf("EmailOpenEvent", StringComparison.Ordinal), because: "the subtype arm must come first");
    }

    [Fact]
    public void The_declaring_type_must_be_partial()
    {
        GeneratorRun run = GeneratorHost.Run(Events.Replace("internal sealed partial class Dispatcher", "internal sealed class Dispatcher", StringComparison.Ordinal));

        run.Diagnostics.Should().ContainSingle(d => d.Id == "SMTP2GO004" && d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("Dispatcher"));
    }
}
