using BlackSlope.Services.MovieService.DomainModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSlope.Services.MovieService
{
    public interface IMovieService
    {
        IEnumerable<MovieDomainModel> GetAllMovies();
        MovieDomainModel GetMovie(int id);
        MovieDomainModel CreateMovie(MovieDomainModel movie);
        MovieDomainModel UpdateMovie(MovieDomainModel movie);
        int DeleteMovie(int id);

        Task<bool> CheckIfMovieExists(string title, DateTime? releaseDate);
    }
}
