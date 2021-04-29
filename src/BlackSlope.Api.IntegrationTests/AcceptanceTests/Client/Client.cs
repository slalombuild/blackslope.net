using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Xunit.Abstractions;
using AcceptanceTests.Helpers;

namespace AcceptanceTests.Client
{
    public class Client<T>
    {
        private HttpClient client;
        private readonly string baseUrl;
        private readonly ITestOutputHelper _output;

        public Client(ITestOutputHelper output)
        {
            baseUrl = Environments.BaseUrl;
            _output = output;
        }

        private void ClientSetup()
        {
            client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        }

        public async Task<T> CreateAsStringAsync(string body, string path)
        {
            ClientSetup();
            HttpContent payload = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(path, payload);
            var json = await response.Content.ReadAsStringAsync();
            var jsonModel = Deserialize(json, response);
            _output.WriteLine(" client {0}", response.RequestMessage);
            _output.WriteLine(" payload Data for Uri {0}", payload.ToString());
            _output.WriteLine(" Response Data for Uri {0}", response.ToString());
            _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
            _output.WriteLine(" Response Content: {0} ", json.ToString());

            return jsonModel;
        }

        public async Task<T> Get(string path)
        {
            ClientSetup();
            var response = await client.GetAsync(path);
            var json = await response.Content.ReadAsStringAsync();
            var jsonModel = Deserialize(json, response);
            _output.WriteLine(" client {0}", response.RequestMessage);
            _output.WriteLine(" Response Data for Uri {0}", response.ToString());
            _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
            _output.WriteLine(" Response Content: {0} ", json.ToString());

            return jsonModel;
        }

        public async Task<T> Delete(string path)
        {
            ClientSetup();
            var response = await client.DeleteAsync(path);
            var json = await response.Content.ReadAsStringAsync();
            var jsonModel = Deserialize(json, response);
            _output.WriteLine(" client {0}", response.RequestMessage);
            _output.WriteLine(" Response Data for Uri {0}", response.ToString());
            _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
            _output.WriteLine(" Response Content: {0} ", json.ToString());
            return jsonModel;
        }

        public async Task<T> UpdateAsStringAsync(string body, string path)
        {
            ClientSetup();
            HttpContent payload = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PutAsync(path, payload);
            var json = await response.Content.ReadAsStringAsync();
            var jsonModel = Deserialize(json, response);
            _output.WriteLine(" client {0}", response.RequestMessage);
            _output.WriteLine(" payload Data for Uri {0}", payload.ToString());
            _output.WriteLine(" Response Data for Uri {0}", response.ToString());
            _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
            _output.WriteLine(" Response Content: {0} ", json.ToString());

            return jsonModel;
        }

        private T Deserialize(string json, HttpResponseMessage response)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };

            try
            {
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception e)
            {
                _output.WriteLine("Error deserializing response from {0} : {1}", response.RequestMessage.ToString(), e.Message);
                _output.WriteLine("Error Response Status: {0}", response.StatusCode.ToString());
                _output.WriteLine("Error Response Content: {0} ", StringHelper.FormatJSON(response.Content.ToString()));

                throw;
            }
        }
    }
}
