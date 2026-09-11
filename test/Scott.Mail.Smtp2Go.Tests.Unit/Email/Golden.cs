using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

/// <summary>Golden-file assertions for serialised requests: exact JSON (including property order) and nothing the docs do not list.</summary>
internal static class Golden
{
    private static readonly string[] s_forbiddenProperties = ["api_key", "version"];

    /// <summary>Asserts <paramref name="actualJson"/> equals the fixture once both are normalised to compact form, and that it carries no forbidden content.</summary>
    public static void AssertMatches(string actualJson, string fixtureName)
    {
        JsonNode actual = JsonNode.Parse(actualJson)!;
        JsonNode expected = JsonNode.Parse(Fixture.Read("Email/" + fixtureName))!;

        actual.ToJsonString().Should().Be(expected.ToJsonString(), because: "the payload must match Fixtures/Email/{0} exactly", fixtureName);
        AssertNothingUndocumented(actual);
    }

    /// <summary>Asserts the tree has no <c>api_key</c> or <c>version</c> property, no null value and no empty array anywhere.</summary>
    public static void AssertNothingUndocumented(JsonNode? node, string path = "$")
    {
        switch (node)
        {
            case null:
                throw new Xunit.Sdk.XunitException($"{path} is null; nulls must not be serialised.");
            case JsonObject obj:
                foreach (KeyValuePair<string, JsonNode?> property in obj)
                {
                    s_forbiddenProperties.Should().NotContain(property.Key, because: "{0} must not be serialised (it is not a documented body field)", path + "." + property.Key);
                    AssertNothingUndocumented(property.Value, path + "." + property.Key);
                }

                break;
            case JsonArray array:
                array.Count.Should().BeGreaterThan(0, because: "{0} is an empty array; empty arrays must not be serialised", path);
                for (int i = 0; i < array.Count; i++)
                {
                    AssertNothingUndocumented(array[i], path + "[" + i + "]");
                }

                break;
            case JsonValue value:
                value.GetValueKind().Should().NotBe(JsonValueKind.Null, because: "{0} is null", path);
                break;
        }
    }
}
