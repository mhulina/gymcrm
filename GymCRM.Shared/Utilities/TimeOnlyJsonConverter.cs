using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymCRM.Shared.Utilities;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm:ss";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = TimeOnly.ParseExact(reader.GetString()!, Format, CultureInfo.InvariantCulture);

        return result;
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}