using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using BlackSlope.Api.Common.Versioning;
using Xunit;

namespace BlackSlope.Api.Common.Tests.VersioningTests
{
    public class VersionJsonConverterTests
    {
        private VersionJsonConverter converter
            = new VersionJsonConverter();

        [Fact]
        public void Read_Successful()
        {
            var stubJson = "{\"version\": \"1.0\"}";
            var jsonReader = SeedReader(stubJson);
            
            var version = converter.Read(ref jsonReader, typeof(Version), null);
            Assert.IsType<Version>(version);
        }

        [Fact]
        public void Json_No_StartObject_Failure()
        {
            var stubJson = "{\"version\": \"1.0\"}";
            var seq = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(stubJson));
            var jsonReader = new Utf8JsonReader(seq);
            try
            {
                converter.Read(ref jsonReader, typeof(Version), null);
            }
            catch (JsonException)
            {
                Assert.True(true);
                return;
            }

            Assert.True(false, "Exception not caught.");
        }

        [Fact]
        public void Json_No_Properties_Failure()
        {
            var stubJson = "{}";
            var jsonReader = SeedReader(stubJson);
            var converter = new VersionJsonConverter();
            try
            {
                converter.Read(ref jsonReader, typeof(Version), null);
            }
            catch (JsonException)
            {
                Assert.True(true);
                return;
            }

            Assert.True(false, "Exception not caught.");
        }

        [Fact]
        public void Json_No_EndObject_Failure()
        {
            var stubJson = "{\"version\": \"1.0\", \"failure\": 123}";
            var jsonReader = SeedReader(stubJson);
            try
            {
                converter.Read(ref jsonReader, typeof(Version), null);
            }
            catch (JsonException)
            {
                Assert.True(true);
                return;
            }

            Assert.True(false, "Exception not caught.");
        }

        [Fact]
        public void Write_Successful()
        {
            var version = new Version("1.0.0");

            using (var stream = new MemoryStream())
            {
                using(var jsonWriter = new Utf8JsonWriter(stream))
                {
                    converter.Write(jsonWriter, version, null);
                    var json = Encoding.UTF8.GetString(stream.ToArray());

                    Assert.Equal("{\"version\":\"1.0.0\"}", json);
                }
            }
        }

        private static Utf8JsonReader SeedReader(string json)
        {
            var seq = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(json));
            var reader = new Utf8JsonReader(seq);

            while (reader.TokenType == JsonTokenType.None)
            {
                if (!reader.Read())
                    break;
            }
            return reader;
        }
    }
}
