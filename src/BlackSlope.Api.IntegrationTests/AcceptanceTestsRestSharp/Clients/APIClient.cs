using System;
using AcceptanceTestsRestSharp.Helpers;
using Newtonsoft.Json;
using RestSharp;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp.Clients
{
    public class ApiClient
    {
        private readonly ITestOutputHelper _output;

        public ApiClient(ITestOutputHelper output)
        {
            _output = output;
        }

        public IRestResponse Get(string path)
        {
            return SendRequest(path, Method.GET);
        }

        public IRestResponse Put(string path, string content)
        {
            return SendRequest(path, Method.PUT, content);
        }

        public IRestResponse Post(string path, string content)
        {
            return SendRequest(path, Method.POST, content);
        }

        public IRestResponse Delete(string path)
        {
            return SendRequest(path, Method.DELETE);
        }

        private IRestResponse SendRequest(string path, Method method, string content)
        {
            var client = new RestClient(Environments.BaseUrl);
            var request = new RestRequest(path, method);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", content, ParameterType.RequestBody);
            _output.WriteLine(" Request Data for Uri:{0}", content);
            return client.Execute(request);
        }

        private IRestResponse SendRequest(string path, Method method)
        {
            var client = new RestClient(Environments.BaseUrl);
            var request = new RestRequest(path, method);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            return client.Execute(request);
        }

        public T Get<T>(string path)
        {
            var response = SendRequest(path, Method.GET);
            return ConvertToModelViewResponse<T>(response);
        }

        public T Put<T>(string path, string content)
        {
            var response = SendRequest(path, Method.PUT, content);
            return ConvertToModelViewResponse<T>(response);
        }


        public T Post<T>(string path, string content)
        {
            var response = SendRequest(path, Method.POST, content);
            return ConvertToModelViewResponse<T>(response);
        }

        public T Delete<T>(string path)
        {
            var response = SendRequest(path, Method.DELETE);
            return ConvertToModelViewResponse<T>(response);
        }

        private T ConvertToModelViewResponse<T>(IRestResponse restResponse)
        {
            _output.WriteLine(" Response Data for Uri {0}", restResponse.ResponseUri.ToString());
            _output.WriteLine(" Response Status: {0}", restResponse.StatusCode.ToString());
            _output.WriteLine(" Response Content: {0} ", StringHelper.FormatJSON(restResponse.Content.ToString()));

            try
            {
                return JsonConvert.DeserializeObject<T>(restResponse.Content);
            }
            catch (Exception e)
            {
                _output.WriteLine("Error deserializing response from {0} : {1}", restResponse.ResponseUri.ToString(), e.Message);
                _output.WriteLine("Error Response Status: {0}", restResponse.StatusCode.ToString());
                _output.WriteLine("Error Response Content: {0} ", StringHelper.FormatJSON(restResponse.Content.ToString()));

                throw;
            }
        }
    }
}
