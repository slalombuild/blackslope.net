using System.Threading.Tasks;
using AcceptanceTests.Client;
using AcceptanceTests.Helpers;
using BlackSlope.Api.Operations.Movies.ViewModels;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace AcceptanceTests.TestServices
{
    public class MovieService : ITestServices
    {
        protected readonly ITestOutputHelper outputHelper;

        public MovieService(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public async Task<MovieViewModel[]> GetMovie()
        {
            var client = new Client<MovieViewModel[]>(outputHelper);

            var response = await client.Get($"{Constants.BaseRoute}{"/movies"}");
            return response;
        }

        public async Task<MovieViewModel> EditMovie(CreateMovieViewModel movie, string movieId)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var data = JsonConvert.SerializeObject(movie).ToString();
            var movieEditResponse = await client.UpdateAsStringAsync(data, $"{Constants.BaseRoute}{Constants.Movies}{movieId}");
            return movieEditResponse;
        }
        public async Task<MovieViewModel> CreateMovie(CreateMovieViewModel movie)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var body = JsonConvert.SerializeObject(movie).ToString();
            var url = $"{Constants.BaseRoute}{Constants.Movies}";
            var movieResponse = await client.CreateAsStringAsync(body, url);
            return movieResponse;
        }

        public async Task<MovieViewModel> DeleteMovie(int movieId)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var response = await client.Delete($"{Constants.BaseRoute}{movieId}");
            return response;
        }
    }
}







