using System.Text.Json;
using Scott.Mail.Smtp2Go.Generators;

namespace Scott.Mail.Smtp2Go.Tests.Generators;

public class SnakeCaseTests
{
    [Theory]
    [InlineData("Unknown")]
    [InlineData("SoftBounced")]
    [InlineData("OptOut")]
    [InlineData("HTMLBody")]
    [InlineData("ABCDef")]
    [InlineData("Html2Pdf")]
    [InlineData("IPv6Address")]
    [InlineData("X")]
    [InlineData("already_snake")]
    [InlineData("With Space")]
    [InlineData("  padded  ")]
    [InlineData("Mixed-Punctuation.Here")]
    [InlineData("Ünïcödé")]
    [InlineData("")]
    public void Matches_the_serializer_policy(string name)
    {
        SnakeCase.Convert(name).Should().Be(JsonNamingPolicy.SnakeCaseLower.ConvertName(name));
    }
}

public class MiniJsonTests
{
    [Fact]
    public void Parses_the_constructs_the_snapshot_uses()
    {
        object? value = MiniJson.Parse("""{ "a": [1, 2.5, -3e2], "b": { "c": "x\"y\\zA\n" }, "t": true, "f": false, "n": null }""");

        Dictionary<string, object?> root = value.Should().BeOfType<Dictionary<string, object?>>().Subject;
        root["a"].Should().BeEquivalentTo(new List<object?> { 1d, 2.5d, -300d });
        ((Dictionary<string, object?>)root["b"]!)["c"].Should().Be("x\"y\\zA\n");
        root["t"].Should().Be(true);
        root["f"].Should().Be(false);
        root["n"].Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[1,]")]
    [InlineData("{\"a\" 1}")]
    [InlineData("tru")]
    [InlineData("\"unterminated")]
    [InlineData("{} extra")]
    public void Rejects_malformed_input(string text)
    {
        Action act = () => MiniJson.Parse(text);

        act.Should().Throw<FormatException>();
    }
}

public class GlobTests
{
    [Theory]
    [InlineData("*/view", "api_keys/view", true)]
    [InlineData("*/view", "users/smtp/view", true)]
    [InlineData("*/view", "api_keys/view-all", false)]
    [InlineData("sms/view-*", "sms/view-sent", true)]
    [InlineData("stats/*", "stats/email_cycle", true)]
    [InlineData("stats/*", "stat/email_cycle", false)]
    [InlineData("email/send", "email/send", true)]
    [InlineData("email/send", "email/sendx", false)]
    [InlineData("*", "anything/at/all", true)]
    [InlineData("a*b*c", "aXXbYYc", true)]
    [InlineData("a*b*c", "aXXbYY", false)]
    public void Star_matches_any_run(string pattern, string path, bool expected)
    {
        Glob.IsMatch(pattern, path).Should().Be(expected);
    }
}

public class EndpointSnapshotTests
{
    [Fact]
    public void Reads_path_method_and_subaccount_support()
    {
        EndpointSnapshot snapshot = EndpointSnapshot.Parse(Stubs.Snapshot);

        snapshot.Found.Should().BeTrue();
        snapshot.Error.Should().BeNull();
        snapshot.Operations.Should().HaveCount(5);
        snapshot.Operations[2].Should().Be(new OperationInfo("api_keys/edit", "POST", true));
        snapshot.MethodsFor("api_keys/edit").Should().Equal("POST", "PATCH");
        snapshot.MethodsFor("nowhere").Should().BeEmpty();
    }

    [Theory]
    [InlineData("{}", "no 'operations' array")]
    [InlineData("{ \"operations\": [ { \"path\": \"x\" } ] }", "lacks 'path' or 'method'")]
    [InlineData("not json", "Invalid JSON")]
    public void Reports_what_is_wrong(string json, string expected)
    {
        EndpointSnapshot snapshot = EndpointSnapshot.Parse(json);

        snapshot.Found.Should().BeTrue();
        snapshot.Error.Should().Contain(expected);
        snapshot.Operations.Should().BeEmpty();
    }
}
