using System.Collections.Generic;
using System.Linq;
using BlackSlope.Repositories.MovieRepository.Context;
using BlackSlope.Repositories.MovieRepository.DtoModels;

namespace BlackSlope.Repositories.MovieRepository
{
    public class MovieRepository : IMovieRepository
    {
        private MovieContext _context;

        public MovieRepository(MovieContext movieContext)
        {
            _context = movieContext;
        }

        public MovieDtoModel Create(MovieDtoModel movie)
        {
            _context.Movies.Add(movie);
            _context.SaveChanges();

            return movie;
        }

        public int Delete(int id)
        {
            var dto = _context.Movies.FirstOrDefault(x => x.Id == id);
            _context.Movies.Remove(dto);
            _context.SaveChanges();

            return id;
        }

        public IEnumerable<MovieDtoModel> GetAll()
        {
            return _context.Movies.ToList();
        }

        public MovieDtoModel GetSingle(int id)
        {
            return _context.Movies.FirstOrDefault(x => x.Id == id);
        }

        public MovieDtoModel Update(MovieDtoModel movie)
        {
            var dto = _context.Movies.FirstOrDefault(x => x.Id == movie.Id);
            dto.Title = movie.Title;
            dto.Description = movie.Description;
            _context.SaveChanges();

            return movie;
        }
    }
}
