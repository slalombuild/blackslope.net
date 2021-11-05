using System.Threading.Tasks;
using BlackSlope.Api.Operations.Movies.ViewModels;

namespace AcceptanceTests.TestServices
{
    public interface ITestServices
    {
        Task<MovieViewModel[]> GetMovies();
        Task<MovieViewModel> UpdateMovieById(CreateMovieViewModel movie, int movieId);
        Task<MovieViewModel> CreateMovie(CreateMovieViewModel movie);
        Task DeleteMovie(int movieId);
        Task<MovieViewModel> GetMovieById(int targetMovieId);
    }
}
