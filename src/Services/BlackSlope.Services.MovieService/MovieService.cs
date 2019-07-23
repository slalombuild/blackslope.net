using System.Collections.Generic;
using BlackSlope.Services.MovieService.DomainModels;
using BlackSlope.Repositories.MovieRepository;
using AutoMapper;
using BlackSlope.Repositories.MovieRepository.DtoModels;
using System.Threading.Tasks;
using System;

namespace BlackSlope.Services.MovieService
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IMapper _mapper;

        public MovieService(IMovieRepository movieRepository,
            IMapper mapper)
        {
            _movieRepository = movieRepository;
            _mapper = mapper;
        }

        public MovieDomainModel CreateMovie(MovieDomainModel movie)
        {
            var dto = _movieRepository.Create(_mapper.Map<MovieDtoModel>(movie));

            return _mapper.Map<MovieDomainModel>(dto);
        }

        public int DeleteMovie(int id)
        {
            _movieRepository.Delete(id);

            return id;
        }

        public IEnumerable<MovieDomainModel> GetAllMovies()
        {
            var movies = _movieRepository.GetAll();
            return _mapper.Map<List<MovieDomainModel>>(movies);
        }

        public MovieDomainModel GetMovie(int id)
        {
            var movie = _movieRepository.GetSingle(id);
            return _mapper.Map<MovieDomainModel>(movie);
        }

        public MovieDomainModel UpdateMovie(MovieDomainModel movie)
        {
            var dto = _movieRepository.Update(_mapper.Map<MovieDtoModel>(movie));

            return _mapper.Map<MovieDomainModel>(dto);
        }


        public async Task<bool> CheckIfMovieExists(string title, DateTime? releaseDate)
        {
            return await _movieRepository.MovieExistsAsync(title, releaseDate);
        }
    }
}
