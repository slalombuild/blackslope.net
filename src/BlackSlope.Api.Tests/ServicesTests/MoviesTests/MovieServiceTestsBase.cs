using AutoFixture;
using AutoMapper;
using BlackSlope.Repositories.Movies;
using BlackSlope.Services.Movies;
using Moq;

namespace BlackSlope.Api.Tests.ServicesTests.MoviesTests
{
    public class MovieServiceTestsBase
    {
        protected readonly Fixture _fixture = new Fixture();
        protected Mock<IMapper> _mapper;
        protected readonly Mock<IMovieRepository> _movieRepository;
        protected readonly IMovieService _service;

        public MovieServiceTestsBase()
        {
            _movieRepository = new Mock<IMovieRepository>();
            _mapper = new Mock<IMapper>();
            _service = new MovieService(_movieRepository.Object, _mapper.Object);
        }
    }
}
