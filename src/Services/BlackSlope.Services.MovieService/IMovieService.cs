using BlackSlope.Services.MovieService.DomainModels;
using System.Collections.Generic;

namespace BlackSlope.Services.MovieService
{
    public interface IMovieService
    {
        IEnumerable<MovieDomainModel> GetAllMovies();
        MovieDomainModel GetMovie(int id);
        MovieDomainModel CreateMovie(MovieDomainModel movie);
        MovieDomainModel UpdateMovie(MovieDomainModel movie);
        int DeleteMovie(int id);
    }
}
