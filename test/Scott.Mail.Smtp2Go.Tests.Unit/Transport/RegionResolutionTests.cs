using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class RegionResolutionTests
{
    [Theory]
    [InlineData(Region.Global, "https://api.smtp2go.com/v3/stats/email_cycle")]
    [InlineData(Region.US, "https://us-api.smtp2go.com/v3/stats/email_cycle")]
    [InlineData(Region.EU, "https://eu-api.smtp2go.com/v3/stats/email_cycle")]
    [InlineData(Region.AU, "https://au-api.smtp2go.com/v3/stats/email_cycle")]
    public async Task Options_region_selects_the_regional_base_url(Region region, string expected)
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.Region = region);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        handler.LastRequest.Uri.Should().Be(new Uri(expected));
    }

    [Fact]
    public async Task Default_is_the_global_endpoint()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        handler.LastRequest.Uri.Should().Be(new Uri("https://api.smtp2go.com/v3/stats/email_cycle"));
    }

    [Fact]
    public async Task Request_region_wins_over_options_region()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.Region = Region.EU);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default, options: new RequestOptions { Region = Region.AU });

        handler.LastRequest.Uri.Host.Should().Be("au-api.smtp2go.com");
    }

    [Fact]
    public async Task Options_region_wins_over_base_url()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o =>
        {
            o.Region = Region.US;
            o.BaseUrl = new Uri("https://proxy.example.test/smtp2go/");
        });

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        handler.LastRequest.Uri.Host.Should().Be("us-api.smtp2go.com");
    }

    [Fact]
    public async Task Base_url_is_used_when_no_region_is_set_and_gets_a_trailing_slash()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.BaseUrl = new Uri("https://proxy.example.test/smtp2go/v3"));

        using JsonDocument _ = await client.Raw.SendJsonAsync("/stats/email_cycle", default);

        handler.LastRequest.Uri.Should().Be(new Uri("https://proxy.example.test/smtp2go/v3/stats/email_cycle"));
    }

    [Fact]
    public async Task Diagnostics_receive_the_resolved_region_or_null_for_a_custom_base_url()
    {
        FakeHttpMessageHandler handler = new();
        RecordingDiagnostics regional = new();
        RecordingDiagnostics custom = new();

        using JsonDocument a = await TestClient.Create(handler, o => o.Region = Region.EU, regional).Raw.SendJsonAsync("stats/email_cycle", default);
        using JsonDocument b = await TestClient.Create(handler, o => o.BaseUrl = new Uri("https://proxy.example.test/"), custom).Raw.SendJsonAsync("stats/email_cycle", default);

        regional.Started.Single().Region.Should().Be(Region.EU);
        custom.Started.Single().Region.Should().BeNull();
    }

    [Fact]
    public void RegionEndpoints_rejects_undefined_values()
    {
        Action act = () => RegionEndpoints.GetBaseUrl((Region)42);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
