using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTests.Client;
using AcceptanceTests.Helpers;
using BlackSlope.Api.Operations.Movies.ViewModels;
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

        public async Task<MovieViewModel[]> GetMovies()
        {
            var client = new Client<MovieViewModel[]>(outputHelper);

            return await client.Get($"{Constants.BaseRoute}{Constants.Movies}");
        }

        public async Task<MovieViewModel> UpdateMovieById(CreateMovieViewModel movie, int movieId)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var data = JsonSerializer.Serialize(movie).ToString();
            var movieEditResponse = await client.UpdateAsStringAsync(data, $"{Constants.BaseRoute}{Constants.Movies}/{movieId}");
            return movieEditResponse;
        }
        public async Task<MovieViewModel> CreateMovie(CreateMovieViewModel movie)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var body = JsonSerializer.Serialize(movie).ToString();
            var url = $"{Constants.BaseRoute}{Constants.Movies}";
            var movieResponse = await client.CreateAsStringAsync(body, url);
            return movieResponse;
        }

        public async Task DeleteMovie(int movieId)
        {
            var client = new Client<object>(outputHelper);
            await client.Delete($"{Constants.BaseRoute}{Constants.Movies}/{movieId}");
        }

        public async Task<MovieViewModel> GetMovieById(int targetMovieId)
        {
            var client = new Client<MovieViewModel>(outputHelper);

            return await client.Get($"{Constants.BaseRoute}{Constants.Movies}/{targetMovieId}");
        }
    }
}
