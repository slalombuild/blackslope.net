using BlackSlope.Repositories.MovieRepository.DtoModels;
using System.Collections.Generic;

namespace BlackSlope.Repositories.MovieRepository
{
    public interface IMovieRepository
    {
        IEnumerable<MovieDtoModel> GetAll();
        MovieDtoModel GetSingle(int id);
        MovieDtoModel Create(MovieDtoModel movie);
        MovieDtoModel Update(MovieDtoModel movie);
        int Delete(int id);
    }
}
