using AcceptanceTestsRestSharp.Clients;
using RestSharp;
using Xunit.Abstractions;

//CZTODO: SCRUB
namespace AcceptanceTestsRestSharp
{
    public class BaseSteps
    {
        private readonly ApiClient _apiClient;

        public BaseSteps(ITestOutputHelper outputHelper)
        {
            _apiClient = new ApiClient(outputHelper);
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

        public T Get<T>(string path)
        {
            return _apiClient.Get<T>(path);
        }

        public T Put<T>(string path, string content = "")
        {
            return _apiClient.Put<T>(path, content);
        }

        public T Post<T>(string path, string content)
        {
            return _apiClient.Post<T>(path, content);
        }

        public T PostEbanking<T>(string path, string content)
        {
            return _apiClient.Post<T>(path, content);
        }
    }
}
