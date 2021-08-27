using System.Net.Http;
using System.Threading.Tasks;

namespace BlackSlope.Repositories.FakeApi
{
    public class FakeApiRepository : IFakeApiRepository
    {
        private readonly HttpClient _httpClient;

        public FakeApiRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<dynamic> GetExponentialBackoff()
        {
            return await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos");
        }
    }
}
