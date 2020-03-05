using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BlackSlope.Repositories.Movies;
using BlackSlope.Repositories.Movies.DtoModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Services.Movies
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IMapper _mapper;

        public MovieService(IMovieRepository movieRepository, IMapper mapper)
        {
            _movieRepository = movieRepository;
            _mapper = mapper;
        }

        public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
        {
            var dto = await _movieRepository.Create(_mapper.Map<MovieDtoModel>(movie));
            return _mapper.Map<MovieDomainModel>(dto);
        }

        public async Task<int> DeleteMovieAsync(int id) => await _movieRepository.DeleteAsync(id);

        public async Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync()
        {
            var movies = await _movieRepository.GetAllAsync();
            return _mapper.Map<List<MovieDomainModel>>(movies);
        }

        public async Task<MovieDomainModel> GetMovieAsync(int id)
        {
            var movie = await _movieRepository.GetSingleAsync(id);
            return _mapper.Map<MovieDomainModel>(movie);
        }

        public async Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie)
        {
            var dto = await _movieRepository.UpdateAsync(_mapper.Map<MovieDtoModel>(movie));
            return _mapper.Map<MovieDomainModel>(dto);
        }

        public async Task<bool> CheckIfMovieExistsAsync(string title, DateTime? releaseDate) =>
            await _movieRepository.MovieExistsAsync(title, releaseDate);
    }
}
