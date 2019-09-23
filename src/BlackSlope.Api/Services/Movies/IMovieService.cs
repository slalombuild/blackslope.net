using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Services.Movies
{
    public interface IMovieService
    {
        Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync();

        Task<MovieDomainModel> GetMovieAsync(int id);

        Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie);

        Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie);

        Task<int> DeleteMovieAsync(int id);

        Task<bool> CheckIfMovieExistsAsync(string title, DateTime? releaseDate);
    }
}
