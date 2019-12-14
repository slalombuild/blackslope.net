using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using BlackSlope.Repositories.Movies.Context;
using BlackSlope.Repositories.Movies.DtoModels;
using Microsoft.EntityFrameworkCore;

namespace BlackSlope.Repositories.Movies
{
    public class MovieRepository : IMovieRepository
    {
        private readonly MovieContext _context;

        public MovieRepository(MovieContext movieContext)
        {
            _context = movieContext;
        }

        public async Task<MovieDtoModel> Create(MovieDtoModel movie)
        {
            await _context.Movies.AddAsync(movie);
            await _context.SaveChangesAsync();

            return movie;
        }

        public async Task<int> DeleteAsync(int id)
        {
            var dto = _context.Movies.FirstOrDefault(x => x.Id == id);
            _context.Movies.Remove(dto);
            await _context.SaveChangesAsync();

            return id;
        }

        public async Task<IEnumerable<MovieDtoModel>> GetAllAsync() =>
            await _context.Movies.ToListAsync();

        public async Task<MovieDtoModel> GetSingleAsync(int id) =>
            await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);

        public async Task<bool> MovieExistsAsync(string title, DateTime? releaseDate) =>
            await _context.Movies.AnyAsync(m => m.Title == title && m.ReleaseDate == releaseDate);

        public async Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie)
        {
            Contract.Requires(movie != null);
            var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
            dto.Title = movie.Title;
            dto.Description = movie.Description;
            await _context.SaveChangesAsync();

            return movie;
        }
    }
}
