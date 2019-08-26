using BlackSlope.Repositories.Movies.DtoModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSlope.Repositories.Movies
{
    public interface IMovieRepository
    {
        Task<IEnumerable<MovieDtoModel>> GetAllAsync();
        Task<MovieDtoModel> GetSingleAsync(int id);
        Task<MovieDtoModel> Create(MovieDtoModel movie);
        Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie);
        Task<int> DeleteAsync(int id);
        Task<bool> MovieExistsAsync(string title, DateTime? releaseDate);
    }
}
