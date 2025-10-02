using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;

/// <summary>
/// This converter is necessary to keep all Context data on deserialization.
/// At the moment we are using it to handle the Context properties as a raw JSON string, in the future we might want to consider a more sophisticated way.
/// </summary>
internal class ContextJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        return jsonDoc.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value);
    }
}
