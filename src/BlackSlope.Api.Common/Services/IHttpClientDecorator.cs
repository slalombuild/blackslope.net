using System.Net.Http;

namespace BlackSlope.Api.Common.Services
{
    public interface IHttpClientDecorator
    {
        void Configure(HttpClient client);
    }
}
