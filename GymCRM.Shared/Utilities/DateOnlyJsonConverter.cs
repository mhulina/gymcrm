using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymCRM.Shared.Utilities;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = DateOnly.ParseExact(reader.GetString()!, Format, CultureInfo.InvariantCulture);

        return result;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}