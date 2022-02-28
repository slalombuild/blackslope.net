using System.Text.Json;

namespace AcceptanceTests.Helpers
{
    public class StringHelper
    {
        public static string FormatJSON(string unformattedJson)
        {
            var parsedJson = JsonSerializer.Deserialize<object>(unformattedJson);
            var formattedJson = JsonSerializer.Serialize(parsedJson, new JsonSerializerOptions { WriteIndented = true });

            return formattedJson;
        }
    }
}
