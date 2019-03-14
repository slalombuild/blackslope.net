using System;
using System.Collections.Generic;
using AutoMapper;
using BlackSlope.Hosts.Api;
using BlackSlope.Repositories.MovieRepository;
using BlackSlope.Repositories.MovieRepository.DtoModels;
using BlackSlope.Services.MovieService;
using BlackSlope.Services.MovieService.DomainModels;
using Moq;
using Xunit;
using AutoFixture;

namespace Blackslope.Services.MovieService.Tests
{
    public class MoviesServiceTests : IDisposable
    {
        private readonly IMapper _mapper;
        private readonly Mock<IMovieRepository> _movieRepository;

        private readonly IMovieService _service;
        private readonly Fixture _fixture = new Fixture();

        public MoviesServiceTests()
        {
            _movieRepository = new Mock<IMovieRepository>();
            _mapper = Startup.GenerateMapperConfiguration();

            _service = new BlackSlope.Services.MovieService.MovieService(_movieRepository.Object,
                _mapper);
        }

        public void Dispose()
        {
        }

        [Fact]
        public void MoviesService_CreateMovie_Success()
        {
            var newMovie = _fixture.Create<MovieDomainModel>();
            var newMovieDto = _mapper.Map<MovieDtoModel>(newMovie);
            _movieRepository.Setup(x => x.Create(It.IsAny<MovieDtoModel>()))
                .Returns(newMovieDto);

            var result = _service.CreateMovie(newMovie);

            Assert.NotNull(result);
            Assert.IsType<MovieDomainModel>(result);
        }

        [Fact]
        public void MoviesService_DeleteMovie_Success()
        {
            var targetId = 123;

            var result = _service.DeleteMovie(targetId);

            Assert.Equal(targetId, result);
        }

        [Fact]
        public void MoviesService_GetAllMovies_Success()
        {
            _movieRepository.Setup(x => x.GetAll())
                .Returns(new List<MovieDtoModel>());

            var result = _service.GetAllMovies();

            Assert.NotNull(result);
            Assert.IsType<List<MovieDomainModel>>(result);
        }

        [Fact]
        public void MoviesService_GetMovie_Success()
        {
            _movieRepository.Setup(x => x.GetSingle(It.IsAny<int>()))
                .Returns(new MovieDtoModel());

            var result = _service.GetMovie(312);

            Assert.NotNull(result);
            Assert.IsType<MovieDomainModel>(result);
        }

        [Fact]
        public void MoviesService_UpdateMovie_Success()
        {
            var target = _fixture.Create<MovieDomainModel>();
            _movieRepository.Setup(x => x.Update(It.IsAny<MovieDtoModel>()))
                .Returns(new MovieDtoModel());

            var result = _service.UpdateMovie(target);

            Assert.NotNull(result);
            Assert.IsType<MovieDomainModel>(result);
        }
    }
}
