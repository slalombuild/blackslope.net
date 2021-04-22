using AcceptanceTestsRestSharp.Clients;
using RestSharp;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp
{
    public class BaseSteps
    {

        private readonly APIClient _apiClient;
        private readonly ITestOutputHelper _output;

        public BaseSteps(ITestOutputHelper outputHelper)
        {
            _output = outputHelper;
            _apiClient = new APIClient(outputHelper);
        }
        public IRestResponse GetWithResponse(string endpoint)
        {
            return _apiClient.Get(endpoint);
        }

        public IRestResponse PutWithResponse(string endpoint, string value = "")
        {
            return _apiClient.Put(endpoint, value);
        }

        public IRestResponse PostWithResponse(string endpoint, string value)
        {
            return _apiClient.Post(endpoint, value);
        }

        public ClientResponse<T> Get<T>(string path)
        {
            return _apiClient.Get<T>(path);
        }

        public ClientResponse<T> Put<T>(string path, string content = "")
        {
            return _apiClient.Put<T>(path, content);
        }

        public ClientResponse<T> Post<T>(string path, string content)
        {
            return _apiClient.Post<T>(path, content);
        }
        public ClientResponse<T> PostEbanking<T>(string path, string content)
        {
            return _apiClient.Post<T>(path, content);
        }
    }
}
