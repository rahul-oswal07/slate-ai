using System.Text.Json;
using SlateAi.Api.Ai;

namespace SlateAi.Api.Tests.Ai;

public sealed class FakeLlmClientTests
{
    [Fact]
    public async Task Answers_with_the_smallest_json_that_fits_the_schema()
    {
        var schema = JsonDocument.Parse("""
            { "type": "object", "additionalProperties": false,
              "properties": {
                "competencyIds": { "type": "array", "items": { "type": "string", "enum": ["M3.ADD.CARRY", "M3.PV.TENS"] } },
                "summaryKn": { "type": "string" },
                "confidence": { "type": "number" },
                "count": { "type": "integer" },
                "ok": { "type": "boolean" },
                "kind": { "const": "lesson" },
                "note": { "anyOf": [{ "type": "null" }, { "type": "string" }] },
                "check": { "type": "object", "additionalProperties": false,
                           "properties": { "correct": { "type": "string", "enum": ["a", "b"] } } } } }
            """).RootElement;

        var response = await new FakeLlmClient().CompleteJsonAsync(
            new LlmRequest(new Prompt("p", 1, "Mapping", "t"), "x", schema), TestContext.Current.CancellationToken);

        var json = JsonDocument.Parse(response.Json).RootElement;
        Assert.Equal("M3.ADD.CARRY", Assert.Single(json.GetProperty("competencyIds").EnumerateArray()).GetString());
        Assert.Equal("fake", json.GetProperty("summaryKn").GetString());
        Assert.Equal(0.9, json.GetProperty("confidence").GetDouble());
        Assert.Equal(1, json.GetProperty("count").GetInt32());
        Assert.True(json.GetProperty("ok").GetBoolean());
        Assert.Equal("lesson", json.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("note").ValueKind);
        Assert.Equal("a", json.GetProperty("check").GetProperty("correct").GetString());
        Assert.Equal("fake", response.Model);
    }
}
