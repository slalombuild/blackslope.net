using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AcceptanceTestsRestSharp.Helpers
{

    public static class IoHelper
    {
        public static string ReadFile(string fileName)
        {
            string directory = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(directory, "Data", fileName);

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Could not find file at path: {filePath}");
            }

            string fileContents = File.ReadAllText(filePath);
            return fileContents;
        }

        public static JObject ReadJsonFile(string fileName)
        {
            string fileContents = ReadFile(fileName);
            return JObject.Parse(fileContents);
        }
    }
}

