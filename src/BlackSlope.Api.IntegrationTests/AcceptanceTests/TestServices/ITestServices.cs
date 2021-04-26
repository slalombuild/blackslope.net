using System.Threading.Tasks;
using BlackSlope.Api.Operations.Movies.ViewModels;

namespace AcceptanceTests.TestServices
{
    public interface ITestServices
    {
        Task<MovieViewModel[]> GetMovie();
        Task<MovieViewModel> EditMovie(CreateMovieViewModel movie, string movieId);
        Task<MovieViewModel> CreateMovie(CreateMovieViewModel movie);
        Task<MovieViewModel> DeleteMovie(int movieId);
    }
}
