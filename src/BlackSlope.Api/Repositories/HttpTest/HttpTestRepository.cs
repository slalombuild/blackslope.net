using System.Net.Http;
using System.Threading.Tasks;

namespace BlackSlope.Repositories.HttpTest
{
    public class HttpTestRepository : IHttpTestRepository
    {
        private readonly HttpClient _httpClient;

        public HttpTestRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<dynamic> GetExponentialBackoff()
        {
            return await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos");
        }
    }
}
