using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.IpAuth;

public class IpAuthTests
{
    [Fact]
    public void Ip_auth_family_is_seeded_with_a_patch_only_edit()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("ip_auth/", StringComparison.Ordinal));

        family.Select(e => e.Method.Method + " " + e.Path).Should().BeEquivalentTo("POST ip_auth/view", "PATCH ip_auth/edit", "POST ip_auth/remove");
        family.Should().OnlyContain(e => e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("ip_auth/view");
        EndpointTable.Get("ip_auth/edit").Method.Should().Be(Endpoint.Patch, because: "Get(path) falls back to the only registered method");
        EndpointTable.TryGet("ip_auth/edit", HttpMethod.Post, out _).Should().BeFalse();
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        AuthenticatedIpPatchRequest patch = new()
        {
            IpAddress = "127.0.0.1",
            Description = "office egress",
            CustomRateLimit = true,
            CustomRateLimitValue = 100,
            CustomRateLimitPeriod = "1 day",
            IpPool = 1234,
            FeedbackEnabled = false,
            FeedbackHtml = "",
            FeedbackText = "",
            OpenTrackingEnabled = true,
            ClickTrackingEnabled = true,
            ArchiveEnabled = true,
            AuditEmail = "audit@example.com",
            BounceNotifications = BounceNotifications.Drop,
            Status = CredentialStatus.Blocked,
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(patch, Smtp2GoJsonContext.Default.AuthenticatedIpPatchRequest), "IpAuth/patch-request.json");
        JsonSerializer.Serialize(new AuthenticatedIpPatchRequest { IpAddress = "10.0.0.1" }, Smtp2GoJsonContext.Default.AuthenticatedIpPatchRequest).Should().Be("""{"ip_address":"10.0.0.1"}""");
        JsonSerializer.Serialize(new AuthenticatedIpViewRequest(), Smtp2GoJsonContext.Default.AuthenticatedIpViewRequest).Should().Be("{}");
        JsonSerializer.Serialize(new AuthenticatedIpViewRequest { IpAddress = "10.0.0.1" }, Smtp2GoJsonContext.Default.AuthenticatedIpViewRequest).Should().Be("""{"ip_address":"10.0.0.1"}""");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<AuthenticatedIpViewResult> viewed = JsonSerializer.Deserialize(Fixture.Read("IpAuth/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseAuthenticatedIpViewResult)!;
        ApiResponse<IReadOnlyList<AuthenticatedIp>> patched = JsonSerializer.Deserialize(Fixture.Read("IpAuth/patch-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListAuthenticatedIp)!;

        viewed.Data.DefaultRateLimitValue.Should().Be(0);
        viewed.Data.DefaultRateLimitPeriod.Should().Be("unlimited");
        viewed.Data.Extra.Should().BeNull();
        AuthenticatedIp entry = viewed.Data.Results.Should().ContainSingle().Which;
        entry.IpAddress.Should().Be("127.0.0.1");
        entry.SendingAllowed.Should().BeTrue();
        entry.CustomRateLimit.Should().BeFalse();
        entry.CustomRateLimitValue.Should().BeNull();
        entry.CustomRateLimitPeriod.Should().Be("0:00:00");
        entry.FeedbackDomain.Should().Be("default");
        entry.AuditEmail.Should().BeNull();
        entry.BounceNotifications.Should().Be(BounceNotifications.From);
        entry.Status.Should().Be(CredentialStatus.Allowed);
        entry.Extra.Should().BeNull();

        AuthenticatedIp patchedEntry = patched.Data.Should().ContainSingle().Which;
        patchedEntry.IpAddress.Should().Be("127.0.0.1");
        patchedEntry.Description.Should().Be("test ip auth");
        patchedEntry.IpPool.Should().Be(1234);
        patchedEntry.Extra.Should().BeNull();
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new AuthenticatedIpPatchRequest { IpAddress = " " }).Validate(EndpointTable.Get("ip_auth/edit"), errors);
        errors.Should().Equal("ip_address is required.");

        errors.Clear();
        ((IRequestValidator)new AuthenticatedIpPatchRequest { IpAddress = "not-an-ip", Status = CredentialStatus.Unknown }).Validate(EndpointTable.Get("ip_auth/edit"), errors);
        errors.Should().Equal("ip_address must be a valid IP address.", "status must not be CredentialStatus.Unknown.");

        errors.Clear();
        ((IRequestValidator)new AuthenticatedIpPatchRequest { IpAddress = "2001:db8::1", CustomRateLimitValue = 5 }).Validate(EndpointTable.Get("ip_auth/edit"), errors);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Client_methods_use_the_documented_paths_and_methods_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("ip_auth/view", HttpStatusCode.OK, Fixture.Read("IpAuth/view-response.json"))
            .Respond("ip_auth/edit", HttpStatusCode.OK, Fixture.Read("IpAuth/patch-response.json"))
            .Respond("ip_auth/remove", HttpStatusCode.OK, """{"request_id":"r","data":{}}""");
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.IpAuth.ViewAsync(new AuthenticatedIpViewRequest { IpAddress = "127.0.0.1" })).Data.Results.Should().HaveCount(1);
        (await client.IpAuth.PatchAsync(new AuthenticatedIpPatchRequest { IpAddress = "127.0.0.1", Status = CredentialStatus.Blocked })).Data.Single().IpAddress.Should().Be("127.0.0.1");
        (await client.IpAuth.RemoveAsync("127.0.0.1")).Data.ValueKind.Should().Be(JsonValueKind.Object);

        handler.Requests.Select(r => r.Method.Method + " " + r.Endpoint!.Path).Should().Equal("POST ip_auth/view", "PATCH ip_auth/edit", "POST ip_auth/remove");
        handler.Requests[0].Body.Should().Be("""{"ip_address":"127.0.0.1","subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"ip_address":"127.0.0.1","status":"blocked","subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"ip_address":"127.0.0.1","subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.IpAuth.ViewAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.IpAuth.PatchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.IpAuth.RemoveAsync(" "))).Should().ThrowAsync<ArgumentException>();
    }
}
