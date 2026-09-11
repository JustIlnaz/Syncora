using System.Text.Json.Serialization;
using System.Text.Json;

namespace Syncora.Helpers
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        private const string Format = @"hh\:mm\:ss";

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
                throw new JsonException("TimeSpan не может быть пустым");

            if (TimeSpan.TryParseExact(value, Format, null, out var ts1)) return ts1;
            if (TimeSpan.TryParse(value, out var ts2)) return ts2;

            throw new JsonException($"Некорректный формат времени: '{value}'. Ожидается 'HH:mm:ss'");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}
