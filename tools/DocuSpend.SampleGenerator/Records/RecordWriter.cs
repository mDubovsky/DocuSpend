using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DocuSpend.SampleGenerator.Records;

public static class RecordWriter
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        NewLine = "\n",
        // Keep Slovak diacritics readable in the file instead of \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // Explicit, so serialization also works in hosts that disable reflection by default.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    public static string Serialize(ExpectedRecord record) => JsonSerializer.Serialize(record, Options) + "\n";

    public static ExpectedRecord Deserialize(string json) =>
        JsonSerializer.Deserialize<ExpectedRecord>(json, Options)
        ?? throw new InvalidDataException("Empty record.");
}
