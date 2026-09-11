using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.ApiKeys;

public class ApiKeyTests
{
    [Fact]
    public void Api_key_family_is_seeded_with_both_edit_methods()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("api_keys/", StringComparison.Ordinal));

        family.Select(e => e.Method.Method + " " + e.Path).Should().BeEquivalentTo(
            "POST api_keys/view", "POST api_keys/add", "POST api_keys/edit", "PATCH api_keys/edit", "POST api_keys/remove", "POST api_keys/permissions");
        family.Where(e => e.Path != "api_keys/permissions").Should().OnlyContain(e => e.AcceptsSubaccountId);
        EndpointTable.Get("api_keys/permissions").AcceptsSubaccountId.Should().BeFalse();
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().BeEquivalentTo("api_keys/view", "api_keys/permissions");
        EndpointTable.Get("api_keys/add").RateLimit.Should().Be(RateLimitClass.ApiKeyAdd);
        family.Where(e => e.Path != "api_keys/add").Should().OnlyContain(e => e.RateLimit == RateLimitClass.None);
        EndpointTable.Get("api_keys/edit").Method.Should().Be(HttpMethod.Post, because: "Get(path) prefers the POST descriptor");
        EndpointTable.Get("api_keys/edit", Endpoint.Patch).AcceptsSubaccountId.Should().BeTrue(because: "the PATCH descriptor is registered, not defaulted");
    }

    [Fact]
    public void Add_request_serialises_every_documented_field()
    {
        ApiKeyAddRequest request = new()
        {
            Description = "test api key",
            CustomRateLimit = true,
            CustomRateLimitValue = 500,
            CustomRateLimitPeriod = "1 hour",
            IpPool = 1234,
            FeedbackEnabled = true,
            FeedbackHtml = "test_html",
            FeedbackText = "test_text",
            OpenTrackingEnabled = true,
            ClickTrackingEnabled = false,
            ArchiveEnabled = true,
            AuditEmail = "audit@example.com",
            BounceNotifications = BounceNotifications.Email("bounces@example.com"),
            Status = CredentialStatus.Sandbox,
            Endpoints = ["/email/send", "/stats/*"],
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.ApiKeyAddRequest), "ApiKeys/add-request.json");
        JsonSerializer.Serialize(new ApiKeyAddRequest(), Smtp2GoJsonContext.Default.ApiKeyAddRequest).Should().Be("{}");
    }

    [Fact]
    public void Edit_patch_and_view_requests_serialise_the_documented_fields()
    {
        const string id = "api-00000000000000000000000000000000";
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new ApiKeyEditRequest { Id = id, Description = "edited key", BounceNotifications = BounceNotifications.Drop, Status = CredentialStatus.Allowed, Endpoints = ["*"] }, Smtp2GoJsonContext.Default.ApiKeyEditRequest), "ApiKeys/edit-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new ApiKeyPatchRequest { Id = id, Status = CredentialStatus.Blocked }, Smtp2GoJsonContext.Default.ApiKeyPatchRequest), "ApiKeys/patch-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new ApiKeyViewRequest { Id = id, Search = "test" }, Smtp2GoJsonContext.Default.ApiKeyViewRequest), "ApiKeys/view-request.json");
        JsonSerializer.Serialize(new ApiKeyViewRequest(), Smtp2GoJsonContext.Default.ApiKeyViewRequest).Should().Be("{}");
        JsonSerializer.Serialize(new ApiKeyPermissionsRequest(), Smtp2GoJsonContext.Default.ApiKeyPermissionsRequest).Should().Be("{}");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<IReadOnlyList<ApiKey>> viewed = JsonSerializer.Deserialize(Fixture.Read("ApiKeys/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListApiKey)!;
        ApiResponse<IReadOnlyList<ApiKey>> added = JsonSerializer.Deserialize(Fixture.Read("ApiKeys/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListApiKey)!;
        ApiResponse<IReadOnlyList<string>> permissions = JsonSerializer.Deserialize(Fixture.Read("ApiKeys/permissions-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListString)!;

        ApiKey key = viewed.Data.Should().ContainSingle().Which;
        key.Key.Should().Be("api-000000000000********************");
        key.Description.Should().Be("test api key again");
        key.FeedbackEnabled.Should().BeTrue();
        key.FeedbackHtml.Should().Be("test_html");
        key.FeedbackText.Should().Be("test_text");
        key.IpPool.Should().Be(1234);
        key.BounceNotifications.Should().Be(BounceNotifications.From);
        key.Status.Should().Be(CredentialStatus.Allowed);
        key.Endpoints.Should().Equal("/email/send");
        key.Extra.Should().BeNull();

        added.Data.Single().Key.Should().Be("api-00000000000000000000000000000000");
        added.Data.Single().Extra.Should().BeNull();
        permissions.Data.Should().Equal("/email/send", "/api_keys/view");
    }

    [Fact]
    public void Unknown_status_and_forwarding_addresses_are_readable()
    {
        ApiResponse<IReadOnlyList<ApiKey>> viewed = JsonSerializer.Deserialize("""{"request_id":"r","data":[{"api_key":"k","status":"paused","bounce_notifications":"ops@example.com","custom_ratelimit_value":null,"audit_email":null}]}""", Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListApiKey)!;

        viewed.Data[0].Status.Should().Be(CredentialStatus.Unknown);
        viewed.Data[0].BounceNotifications!.Kind.Should().Be(BounceNotificationsKind.Email);
        viewed.Data[0].BounceNotifications!.EmailAddress.Should().Be("ops@example.com");
        viewed.Data[0].CustomRateLimitValue.Should().BeNull();
        viewed.Data[0].Extra.Should().BeNull();
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new ApiKeyAddRequest { Status = CredentialStatus.Unknown, CustomRateLimitValue = 0, CustomRateLimitPeriod = " ", Endpoints = ["/email/send", ""] }).Validate(EndpointTable.Get("api_keys/add"), errors);
        errors.Should().Equal(
            "status must not be CredentialStatus.Unknown.",
            "custom_ratelimit_value must be positive.",
            "custom_ratelimit_period must not be blank; use a period such as \"1 hour\", \"2 days\" or \"0:30:00\".",
            "endpoints[1] must not be blank.");

        errors.Clear();
        ((IRequestValidator)new ApiKeyEditRequest { Id = " " }).Validate(EndpointTable.Get("api_keys/edit"), errors);
        errors.Should().Equal("id is required.");

        errors.Clear();
        ((IRequestValidator)new ApiKeyPatchRequest { Id = "", Status = CredentialStatus.Unknown }).Validate(EndpointTable.Get("api_keys/edit", Endpoint.Patch), errors);
        errors.Should().Equal("id is required.", "status must not be CredentialStatus.Unknown.");

        errors.Clear();
        ((IRequestValidator)new ApiKeyPatchRequest { Id = "api-x", CustomRateLimit = true, CustomRateLimitValue = 10, CustomRateLimitPeriod = "1 day" }).Validate(EndpointTable.Get("api_keys/edit", Endpoint.Patch), errors);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Client_methods_use_the_documented_paths_and_methods_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("api_keys/view", HttpStatusCode.OK, Fixture.Read("ApiKeys/view-response.json"))
            .Respond("api_keys/add", HttpStatusCode.OK, Fixture.Read("ApiKeys/add-response.json"))
            .Respond("api_keys/edit", HttpStatusCode.OK, Fixture.Read("ApiKeys/view-response.json"))
            .Respond("api_keys/remove", HttpStatusCode.OK, """{"request_id":"r","data":{}}""")
            .Respond("api_keys/permissions", HttpStatusCode.OK, Fixture.Read("ApiKeys/permissions-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.ApiKeys.ViewAsync(new ApiKeyViewRequest { Search = "test" })).Data.Should().HaveCount(1);
        (await client.ApiKeys.AddAsync(new ApiKeyAddRequest { Description = "d" })).Data.Single().Key.Should().StartWith("api-0000");
        (await client.ApiKeys.EditAsync(new ApiKeyEditRequest { Id = "api-1", Status = CredentialStatus.Allowed })).Data.Should().HaveCount(1);
        (await client.ApiKeys.PatchAsync(new ApiKeyPatchRequest { Id = "api-1", Description = "renamed" })).Data.Should().HaveCount(1);
        (await client.ApiKeys.RemoveAsync("api-1")).Data.ValueKind.Should().Be(JsonValueKind.Object);
        (await client.ApiKeys.GetPermissionsAsync()).Data.Should().Contain("/email/send");

        handler.Requests.Select(r => r.Method.Method + " " + r.Endpoint!.Path).Should().Equal(
            "POST api_keys/view", "POST api_keys/add", "POST api_keys/edit", "PATCH api_keys/edit", "POST api_keys/remove", "POST api_keys/permissions");
        handler.Requests[0].Body.Should().Be("""{"search":"test","subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"description":"d","subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"id":"api-1","status":"allowed","subaccount_id":"sub-1"}""");
        handler.Requests[3].Body.Should().Be("""{"id":"api-1","description":"renamed","subaccount_id":"sub-1"}""");
        handler.Requests[4].Body.Should().Be("""{"id":"api-1","subaccount_id":"sub-1"}""");
        handler.Requests[5].Body.Should().Be("{}", because: "api_keys/permissions does not document subaccount_id");
        handler.Requests[3].Endpoint.Should().Be(EndpointTable.Get("api_keys/edit", Endpoint.Patch));
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.ApiKeys.ViewAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.ApiKeys.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.ApiKeys.EditAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.ApiKeys.PatchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.ApiKeys.RemoveAsync(" "))).Should().ThrowAsync<ArgumentException>();
    }
}
