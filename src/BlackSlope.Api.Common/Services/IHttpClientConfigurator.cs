using System.Net.Http;

namespace BlackSlope.Api.Common.Services
{
    public interface IHttpClientConfigurator
    {
        void Configure(HttpClient client);
    }
}
