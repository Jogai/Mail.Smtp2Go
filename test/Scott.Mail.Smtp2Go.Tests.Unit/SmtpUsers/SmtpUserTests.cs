using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.SmtpUsers;

public class SmtpUserTests
{
    [Fact]
    public void Smtp_user_family_is_seeded_with_both_edit_methods()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("users/smtp/", StringComparison.Ordinal));

        family.Select(e => e.Method.Method + " " + e.Path).Should().BeEquivalentTo(
            "POST users/smtp/view", "POST users/smtp/add", "POST users/smtp/edit", "PATCH users/smtp/edit", "POST users/smtp/remove");
        family.Should().OnlyContain(e => e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("users/smtp/view");
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        SmtpUserAddRequest add = new()
        {
            Username = "smtpuser@example.com",
            EmailPassword = "qet_^3qU1341%*ert",
            Description = "test smtp user",
            CustomRateLimit = true,
            CustomRateLimitValue = 100,
            CustomRateLimitPeriod = "1 day",
            IpPool = 1234,
            FeedbackEnabled = true,
            FeedbackDomain = "default",
            FeedbackHtml = "test_html",
            FeedbackText = "test_text",
            OpenTrackingEnabled = true,
            ClickTrackingEnabled = true,
            ArchiveEnabled = false,
            AuditEmail = "audit@example.com",
            BounceNotifications = BounceNotifications.From,
            Status = CredentialStatus.Allowed,
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(add, Smtp2GoJsonContext.Default.SmtpUserAddRequest), "SmtpUsers/add-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SmtpUserEditRequest { Username = "smtpuser@example.com", EmailPassword = "new-password-Ab1!", Description = "edited smtp user", Status = CredentialStatus.Sandbox }, Smtp2GoJsonContext.Default.SmtpUserEditRequest), "SmtpUsers/edit-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SmtpUserPatchRequest { Username = "smtpuser@example.com", BounceNotifications = BounceNotifications.Drop }, Smtp2GoJsonContext.Default.SmtpUserPatchRequest), "SmtpUsers/patch-request.json");
        JsonSerializer.Serialize(new SmtpUserViewRequest(), Smtp2GoJsonContext.Default.SmtpUserViewRequest).Should().Be("{}");
        JsonSerializer.Serialize(new SmtpUserViewRequest { Username = "u" }, Smtp2GoJsonContext.Default.SmtpUserViewRequest).Should().Be("""{"username":"u"}""");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<SmtpUserViewResult> viewed = JsonSerializer.Deserialize(Fixture.Read("SmtpUsers/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseSmtpUserViewResult)!;
        ApiResponse<IReadOnlyList<SmtpUser>> added = JsonSerializer.Deserialize(Fixture.Read("SmtpUsers/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser)!;
        ApiResponse<IReadOnlyList<SmtpUser>> patched = JsonSerializer.Deserialize(Fixture.Read("SmtpUsers/patch-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser)!;
        ApiResponse<IReadOnlyList<SmtpUser>> removed = JsonSerializer.Deserialize(Fixture.Read("SmtpUsers/remove-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser)!;

        viewed.Data.DefaultRateLimitValue.Should().Be(0);
        viewed.Data.DefaultRateLimitPeriod.Should().Be("unlimited");
        viewed.Data.Extra.Should().BeNull();
        SmtpUser user = viewed.Data.Results.Should().ContainSingle().Which;
        user.Username.Should().Be("smtpuser@example.com");
        user.EmailPassword.Should().Be(",w0z9YFTT[izrH7>");
        user.SendingAllowed.Should().BeTrue();
        user.CustomRateLimit.Should().BeFalse();
        user.CustomRateLimitValue.Should().BeNull();
        user.CustomRateLimitPeriod.Should().Be("0:00:00");
        user.Description.Should().BeEmpty();
        user.FeedbackDomain.Should().Be("default");
        user.ArchiveEnabled.Should().BeFalse();
        user.AuditEmail.Should().BeNull();
        user.BounceNotifications.Should().Be(BounceNotifications.From);
        user.Status.Should().Be(CredentialStatus.Allowed);
        user.Extra.Should().BeNull();

        added.Data.Should().ContainSingle().Which.EmailPassword.Should().Be("aklkweiyasdaf", because: "the results wrapper is unwrapped");
        added.Data[0].Extra.Should().BeNull();
        patched.Data.Should().ContainSingle().Which.Username.Should().Be("my_user", because: "the PATCH response is a bare array");
        patched.Data[0].IpPool.Should().Be(1234);
        patched.Data[0].Extra.Should().BeNull();
        removed.Data.Single().Username.Should().Be("temp2");
        removed.Data.Single().Extra.Should().BeNull();
    }

    [Fact]
    public void Results_wrapper_without_results_reads_as_empty_and_other_shapes_are_rejected()
    {
        JsonSerializer.Deserialize("""{"request_id":"r","data":{"total":0}}""", Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser)!.Data.Should().BeEmpty();
        JsonSerializer.Deserialize("""{"request_id":"r","data":{"other":[1],"results":[{"username":"a"}],"more":{"x":1}}}""", Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser)!.Data.Single().Username.Should().Be("a");
        ((Action)(() => JsonSerializer.Deserialize("""{"request_id":"r","data":"nope"}""", Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser))).Should().Throw<JsonException>();
        JsonSerializer.Serialize(new ApiResponse<IReadOnlyList<SmtpUser>> { RequestId = "r", Data = [new SmtpUser { Username = "a" }] }, Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListSmtpUser).Should().Be("""{"request_id":"r","data":[{"username":"a"}]}""");
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new SmtpUserAddRequest { Username = "abc" }).Validate(EndpointTable.Get("users/smtp/add"), errors);
        errors.Should().Equal("username must be 5 to 100 characters.");

        errors.Clear();
        ((IRequestValidator)new SmtpUserAddRequest { Username = " ", Status = CredentialStatus.Unknown, CustomRateLimitValue = -1 }).Validate(EndpointTable.Get("users/smtp/add"), errors);
        errors.Should().Equal("username is required.", "status must not be CredentialStatus.Unknown.", "custom_ratelimit_value must be positive.");

        errors.Clear();
        ((IRequestValidator)new SmtpUserEditRequest { Username = "abc" }).Validate(EndpointTable.Get("users/smtp/edit"), errors);
        ((IRequestValidator)new SmtpUserPatchRequest { Username = "abc", CustomRateLimitPeriod = "1 hour" }).Validate(EndpointTable.Get("users/smtp/edit", Endpoint.Patch), errors);
        errors.Should().BeEmpty(because: "edit and patch only require the username to be present");

        ((IRequestValidator)new SmtpUserPatchRequest { Username = "" }).Validate(EndpointTable.Get("users/smtp/edit", Endpoint.Patch), errors);
        errors.Should().Equal("username is required.");
    }

    [Fact]
    public async Task Client_methods_use_the_documented_paths_and_methods_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("users/smtp/view", HttpStatusCode.OK, Fixture.Read("SmtpUsers/view-response.json"))
            .Respond("users/smtp/add", HttpStatusCode.OK, Fixture.Read("SmtpUsers/add-response.json"))
            .Respond("users/smtp/edit", async (request, ct) => FakeHttpMessageHandler.Json(HttpStatusCode.OK, request.Method == HttpMethod.Post ? Fixture.Read("SmtpUsers/add-response.json") : Fixture.Read("SmtpUsers/patch-response.json")))
            .Respond("users/smtp/remove", HttpStatusCode.OK, Fixture.Read("SmtpUsers/remove-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.SmtpUsers.ViewAsync(new SmtpUserViewRequest { Username = "smtpuser@example.com" })).Data.Results.Should().HaveCount(1);
        (await client.SmtpUsers.AddAsync(new SmtpUserAddRequest { Username = "test@example.com" })).Data.Single().Username.Should().Be("test@example.com");
        (await client.SmtpUsers.EditAsync(new SmtpUserEditRequest { Username = "test@example.com", Description = "d" })).Data.Should().HaveCount(1);
        (await client.SmtpUsers.PatchAsync(new SmtpUserPatchRequest { Username = "my_user", Status = CredentialStatus.Blocked })).Data.Single().Username.Should().Be("my_user");
        (await client.SmtpUsers.RemoveAsync("temp2")).Data.Single().Username.Should().Be("temp2");

        handler.Requests.Select(r => r.Method.Method + " " + r.Endpoint!.Path).Should().Equal(
            "POST users/smtp/view", "POST users/smtp/add", "POST users/smtp/edit", "PATCH users/smtp/edit", "POST users/smtp/remove");
        handler.Requests[0].Body.Should().Be("""{"username":"smtpuser@example.com","subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"username":"test@example.com","subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"username":"test@example.com","description":"d","subaccount_id":"sub-1"}""");
        handler.Requests[3].Body.Should().Be("""{"username":"my_user","status":"blocked","subaccount_id":"sub-1"}""");
        handler.Requests[4].Body.Should().Be("""{"username":"temp2","subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.SmtpUsers.ViewAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.SmtpUsers.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.SmtpUsers.EditAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.SmtpUsers.PatchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.SmtpUsers.RemoveAsync(""))).Should().ThrowAsync<ArgumentException>();
    }
}
