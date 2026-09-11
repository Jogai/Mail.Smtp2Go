using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Domains;

public class DomainTests
{
    [Fact]
    public void Domain_family_is_seeded()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("domain/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("domain/view", "domain/add", "domain/verify", "domain/remove", "domain/tracking", "domain/returnpath", "domain/subaccount_access");
        family.Should().OnlyContain(e => e.Method == HttpMethod.Post && e.RateLimit == RateLimitClass.None);
        family.Where(e => !e.AcceptsSubaccountId).Select(e => e.Path).Should().Equal("domain/subaccount_access");
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("domain/view");
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        DomainAddRequest add = new()
        {
            Domain = "example.com",
            TrackingSubdomain = "link",
            ReturnPathSubdomain = "return",
            AutoVerify = false,
            RequisitionSsl = true,
            SubaccountAccess = new DomainSubaccountAccess { Subaccounts = ["GnlKn5", "34l8oj"], FutureSubaccounts = true },
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(add, Smtp2GoJsonContext.Default.DomainAddRequest), "Domains/add-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new DomainTrackingRequest { Domain = "exampledomain.com", OldSubdomain = "track", NewSubdomain = "newsubdomain" }, Smtp2GoJsonContext.Default.DomainTrackingRequest), "Domains/tracking-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new DomainSubaccountAccessRequest { Domain = "my-verified-domain.com", Subaccounts = ["GnlKn5"], FutureSubaccounts = false }, Smtp2GoJsonContext.Default.DomainSubaccountAccessRequest), "Domains/subaccount-access-request.json");
        JsonSerializer.Serialize(new DomainReturnPathRequest { Domain = "example.com", OldSubdomain = "returns", NewSubdomain = "return" }, Smtp2GoJsonContext.Default.DomainReturnPathRequest).Should().Be("""{"domain":"example.com","old_subdomain":"returns","new_subdomain":"return"}""");
        JsonSerializer.Serialize(new DomainVerifyRequest { Domain = "example.com", RequisitionSsl = false }, Smtp2GoJsonContext.Default.DomainVerifyRequest).Should().Be("""{"domain":"example.com","requisition_ssl":false}""");
        JsonSerializer.Serialize(new DomainViewRequest(), Smtp2GoJsonContext.Default.DomainViewRequest).Should().Be("{}");
        JsonSerializer.Serialize(new DomainAddRequest { Domain = "example.com" }, Smtp2GoJsonContext.Default.DomainAddRequest).Should().Be("""{"domain":"example.com"}""");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<DomainViewResult> viewed = JsonSerializer.Deserialize(Fixture.Read("Domains/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseDomainViewResult)!;
        ApiResponse<DomainViewResult> removed = JsonSerializer.Deserialize(Fixture.Read("Domains/remove-response.json"), Smtp2GoJsonContext.Default.ApiResponseDomainViewResult)!;
        ApiResponse<DomainViewResult> tracking = JsonSerializer.Deserialize(Fixture.Read("Domains/tracking-response.json"), Smtp2GoJsonContext.Default.ApiResponseDomainViewResult)!;
        ApiResponse<DomainViewResult> returnPath = JsonSerializer.Deserialize(Fixture.Read("Domains/returnpath-response.json"), Smtp2GoJsonContext.Default.ApiResponseDomainViewResult)!;
        ApiResponse<DomainSubaccountAccessResult> access = JsonSerializer.Deserialize(Fixture.Read("Domains/subaccount-access-response.json"), Smtp2GoJsonContext.Default.ApiResponseDomainSubaccountAccessResult)!;

        SenderDomain domain = viewed.Data.Domains.Should().ContainSingle().Which;
        domain.Domain!.FullDomain.Should().Be("example.com");
        domain.Domain.Subdomain.Should().BeNull();
        domain.Domain.Domain.Should().Be("example");
        domain.Domain.Suffix.Should().Be("com");
        domain.Domain.DkimSelector.Should().Be("s123456");
        domain.Domain.DkimVerified.Should().BeTrue();
        domain.Domain.DkimStatus.Should().BeEmpty();
        domain.Domain.DkimValue.Should().Be("dkim.smtp2go.net");
        domain.Domain.ReturnPathSelector.Should().Be("em744766");
        domain.Domain.ReturnPathVerified.Should().BeTrue();
        domain.Domain.ReturnPathValue.Should().Be("return.smtp2go.net");
        domain.Domain.SetupLink.Should().Be("<url>");
        domain.Domain.Extra.Should().BeNull();
        TrackingDomain tracker = domain.Trackers.Should().ContainSingle().Which;
        tracker.FullDomain.Should().Be("link.example.com");
        tracker.Subdomain.Should().Be("link");
        tracker.CnameVerified.Should().BeTrue();
        tracker.CnameValue.Should().Be("track.smtp2go.net");
        tracker.Enabled.Should().BeTrue();
        tracker.Extra.Should().BeNull();
        domain.SubaccountAccess!.Subaccounts.Should().BeEmpty();
        domain.SubaccountAccess.FutureSubaccounts.Should().BeFalse();
        domain.SubaccountAccess.Extra.Should().BeNull();
        domain.FromMaster.Should().BeNull();
        domain.Extra.Should().BeNull();
        viewed.Data.Extra.Should().BeNull();

        removed.Data.Domains.Should().BeEmpty();
        tracking.Data.Domains![0].Domain!.Subdomain.Should().BeEmpty();
        tracking.Data.Domains[0].Trackers![0].FullDomain.Should().Be("newsubdomain.exampledomain.com");
        returnPath.Data.Domains![0].Trackers![0].CnameStatus.Should().Contain("returned no results");
        returnPath.Data.Domains[0].Trackers![0].Suffix.Should().Be("co.uk");

        access.Data.Domain.Should().Be("my-verified-domain.com");
        access.Data.Subaccounts.Should().BeEmpty();
        access.Data.FutureSubaccounts.Should().BeFalse();
        access.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Delegated_domains_carry_from_master()
    {
        ApiResponse<DomainViewResult> viewed = JsonSerializer.Deserialize("""{"request_id":"r","data":{"domains":[{"domain":{"fulldomain":"example.com"},"trackers":[],"from_master":true}]}}""", Smtp2GoJsonContext.Default.ApiResponseDomainViewResult)!;

        viewed.Data.Domains![0].FromMaster.Should().BeTrue();
        viewed.Data.Domains[0].Extra.Should().BeNull();
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new DomainAddRequest { Domain = " ", SubaccountAccess = new DomainSubaccountAccess { Subaccounts = ["a", ""] } }).Validate(EndpointTable.Get("domain/add"), errors);
        errors.Should().Equal("domain is required.", "subaccounts[1] must not be blank.");

        errors.Clear();
        ((IRequestValidator)new DomainVerifyRequest { Domain = "http://example.com/" }).Validate(EndpointTable.Get("domain/verify"), errors);
        errors.Should().Equal("domain must be a bare domain name such as example.com.");

        errors.Clear();
        ((IRequestValidator)new DomainTrackingRequest { Domain = "example.com", OldSubdomain = "", NewSubdomain = " " }).Validate(EndpointTable.Get("domain/tracking"), errors);
        errors.Should().Equal("old_subdomain is required.", "new_subdomain is required.");

        errors.Clear();
        ((IRequestValidator)new DomainReturnPathRequest { Domain = "example.com", OldSubdomain = "returns", NewSubdomain = "return" }).Validate(EndpointTable.Get("domain/returnpath"), errors);
        ((IRequestValidator)new DomainSubaccountAccessRequest { Domain = "example.com", Subaccounts = [] }).Validate(EndpointTable.Get("domain/subaccount_access"), errors);
        errors.Should().BeEmpty();

        ((IRequestValidator)new DomainSubaccountAccessRequest { Domain = "example.com", Subaccounts = null! }).Validate(EndpointTable.Get("domain/subaccount_access"), errors);
        errors.Should().Equal("subaccounts is required (an empty list revokes access).");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_inject_the_subaccount_id_where_documented()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("domain/view", HttpStatusCode.OK, Fixture.Read("Domains/view-response.json"))
            .Respond("domain/add", HttpStatusCode.OK, Fixture.Read("Domains/view-response.json"))
            .Respond("domain/verify", HttpStatusCode.OK, Fixture.Read("Domains/view-response.json"))
            .Respond("domain/remove", HttpStatusCode.OK, Fixture.Read("Domains/remove-response.json"))
            .Respond("domain/tracking", HttpStatusCode.OK, Fixture.Read("Domains/tracking-response.json"))
            .Respond("domain/returnpath", HttpStatusCode.OK, Fixture.Read("Domains/returnpath-response.json"))
            .Respond("domain/subaccount_access", HttpStatusCode.OK, Fixture.Read("Domains/subaccount-access-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.Domains.ViewAsync(new DomainViewRequest { Domain = "example.com" })).Data.Domains.Should().HaveCount(1);
        (await client.Domains.AddAsync(new DomainAddRequest { Domain = "example.com" })).Data.Domains.Should().HaveCount(1);
        (await client.Domains.VerifyAsync(new DomainVerifyRequest { Domain = "example.com" })).Data.Domains.Should().HaveCount(1);
        (await client.Domains.RemoveAsync("example.com")).Data.Domains.Should().BeEmpty();
        (await client.Domains.SetTrackingSubdomainAsync(new DomainTrackingRequest { Domain = "exampledomain.com", OldSubdomain = "track", NewSubdomain = "newsubdomain" })).Data.Domains.Should().HaveCount(1);
        (await client.Domains.SetReturnPathSubdomainAsync(new DomainReturnPathRequest { Domain = "example.com", OldSubdomain = "returns", NewSubdomain = "return" })).Data.Domains.Should().HaveCount(1);
        (await client.Domains.SetSubaccountAccessAsync(new DomainSubaccountAccessRequest { Domain = "my-verified-domain.com", Subaccounts = [] })).Data.Domain.Should().Be("my-verified-domain.com");

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("domain/view", "domain/add", "domain/verify", "domain/remove", "domain/tracking", "domain/returnpath", "domain/subaccount_access");
        handler.Requests.Should().OnlyContain(r => r.Method == HttpMethod.Post);
        handler.Requests[0].Body.Should().Be("""{"domain":"example.com","subaccount_id":"sub-1"}""");
        handler.Requests[3].Body.Should().Be("""{"domain":"example.com","subaccount_id":"sub-1"}""");
        handler.Requests[4].Body.Should().Be("""{"domain":"exampledomain.com","old_subdomain":"track","new_subdomain":"newsubdomain","subaccount_id":"sub-1"}""");
        handler.Requests[6].Body.Should().Be("""{"domain":"my-verified-domain.com","subaccounts":[]}""", because: "domain/subaccount_access does not document subaccount_id");
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Domains.ViewAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Domains.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Domains.VerifyAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Domains.RemoveAsync(""))).Should().ThrowAsync<ArgumentException>();
        await ((Func<Task>)(() => client.Domains.SetTrackingSubdomainAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Domains.SetReturnPathSubdomainAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Domains.SetSubaccountAccessAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
