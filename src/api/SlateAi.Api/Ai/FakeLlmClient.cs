using System.Text;
using System.Text.Json;

namespace SlateAi.Api.Ai;

/// <summary>
/// <c>Ai:Provider=Fake</c>: answers any request with the smallest JSON that fits its schema (first enum value,
/// one array item, 0.9 for numbers), so the app runs end to end without keys or credits.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    public Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            Write(writer, request.Schema);
        }

        return Task.FromResult(new LlmResponse(
            Encoding.UTF8.GetString(buffer.ToArray()), "fake", new LlmUsage(0, 0, 0), Guid.Empty));
    }

    private static void Write(Utf8JsonWriter writer, JsonElement schema)
    {
        if (schema.TryGetProperty("const", out var constant))
        {
            constant.WriteTo(writer);
            return;
        }

        if (schema.TryGetProperty("enum", out var values) && values.GetArrayLength() > 0)
        {
            values[0].WriteTo(writer);
            return;
        }

        if (schema.TryGetProperty("anyOf", out var options) && options.GetArrayLength() > 0)
        {
            Write(writer, options[0]);
            return;
        }

        var type = schema.TryGetProperty("type", out var t)
            ? t.ValueKind == JsonValueKind.Array ? t[0].GetString() : t.GetString()
            : "object";
        switch (type)
        {
            case "object":
                writer.WriteStartObject();
                if (schema.TryGetProperty("properties", out var properties))
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        Write(writer, property.Value);
                    }
                }

                writer.WriteEndObject();
                break;
            case "array":
                writer.WriteStartArray();
                if (schema.TryGetProperty("items", out var items))
                {
                    Write(writer, items);
                }

                writer.WriteEndArray();
                break;
            case "integer":
                writer.WriteNumberValue(1);
                break;
            case "number":
                writer.WriteNumberValue(0.9);
                break;
            case "boolean":
                writer.WriteBooleanValue(true);
                break;
            case "null":
                writer.WriteNullValue();
                break;
            default:
                writer.WriteStringValue("fake");
                break;
        }
    }
}
