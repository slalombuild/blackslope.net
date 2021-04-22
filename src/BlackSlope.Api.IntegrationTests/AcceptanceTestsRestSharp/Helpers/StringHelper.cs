using Newtonsoft.Json;

namespace AcceptanceTestsRestSharp.Helpers
{
    public class StringHelper
    {

        public static string FormatJSON(string unformattedJson)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(unformattedJson);
            string formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

            return formattedJson;
        }
    }
}
