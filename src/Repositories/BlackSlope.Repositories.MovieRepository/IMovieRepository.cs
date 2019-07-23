using BlackSlope.Repositories.MovieRepository.DtoModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSlope.Repositories.MovieRepository
{
    public interface IMovieRepository
    {
        IEnumerable<MovieDtoModel> GetAll();
        MovieDtoModel GetSingle(int id);
        MovieDtoModel Create(MovieDtoModel movie);
        MovieDtoModel Update(MovieDtoModel movie);
        int Delete(int id);
        Task<bool> MovieExistsAsync(string title, DateTime? releaseDate);
    }
}
