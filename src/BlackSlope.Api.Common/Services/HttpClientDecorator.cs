using System;
using System.Net.Http;
using BlackSlope.Api.Common.Configuration;

namespace BlackSlope.Api.Common.Services
{
    public class HttpClientDecorator : IHttpClientDecorator
    {
        private readonly HostConfig _config;

        public HttpClientDecorator(HostConfig config)
        {
            _config = config;
        }

        public void Configure(HttpClient client)
        {
            client.BaseAddress = new Uri(_config.BaseUrl);
        }
    }
}
