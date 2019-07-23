using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackSlope.Repositories.MovieRepository.Context;
using BlackSlope.Repositories.MovieRepository.DtoModels;
using Microsoft.EntityFrameworkCore;

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

        public async Task<bool> MovieExistsAsync(string title, DateTime? releaseDate)
        {
            return await _context.Movies.AnyAsync(m => m.Title == title && m.ReleaseDate == releaseDate);                                   
        }

        public MovieDtoModel Update(MovieDtoModel movie)
        {
            var dto = _context.Movies.FirstOrDefault(x => x.Id == movie.Id);
            dto.Title = movie.Title;
            dto.Description = movie.Description;
            dto.ReleaseDate = movie.ReleaseDate;
            _context.SaveChanges();

            return movie;
        }
    }
}
