using RestSharp;

namespace AcceptanceTestsRestSharp.Helpers
{

    public static class RequestHelper
    {

        public static IRestResponse Get(string endpoint)
        {
            RestClient rc = new RestClient(endpoint);
            return rc.Get(new RestRequest(""));
        }
        public static IRestResponse Get(string baseEndpoint, string endpoint)
        {
            RestClient rc = new RestClient(baseEndpoint);
            return rc.Get(new RestRequest(endpoint));
        }

        public static IRestResponse Post(string baseEndpoint, string endpoint, string value)
        {
            var rc = new RestClient(baseEndpoint);
            return Post(endpoint, value, rc);
        }
        public static IRestResponse Post(string endpoint, string value)
        {
            var rc = new RestClient(endpoint);
            return Post("", value, rc);
        }

        public static IRestResponse Put(string endpoint)
        {
            RestClient rc = new RestClient(endpoint);
            return rc.Get(new RestRequest(""));
        }

        public static IRestResponse Put(string endpoint, string value)
        {
            var rc = new RestClient(endpoint);
            return Put("", value, rc);
        }

        private static IRestResponse Put(string endpoint, string value, IRestClient rc)
        {
            var restRequest = new RestRequest(endpoint)
            {
                RequestFormat = DataFormat.Json
            }.AddParameter("application/json", value, ParameterType.RequestBody);
            return rc.Put(restRequest);
        }

        private static IRestResponse Post(string endpoint, string value, IRestClient rc)
        {
            var restRequest = new RestRequest(endpoint)
            {
                RequestFormat = DataFormat.Json
            }.AddParameter("application/json", value, ParameterType.RequestBody);
            return rc.Post(restRequest);
        }

    }
}
