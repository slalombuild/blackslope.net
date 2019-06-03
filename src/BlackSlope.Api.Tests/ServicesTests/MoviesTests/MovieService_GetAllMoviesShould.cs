using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using BlackSlope.Repositories.Movies.DtoModels;
using BlackSlope.Services.Movies.DomainModels;
using Moq;
using Xunit;

namespace BlackSlope.Api.Tests.ServicesTests.MoviesTests
{
    public class MovieService_GetAllMoviesShould : MovieServiceTestsBase
    {
        [Fact]
        public async Task ReturnAllMovies()
        {
            _mapper.Setup(_ => _.Map<List<MovieDomainModel>>(It.IsAny<List<MovieDtoModel>>())).Returns(_fixture.Create<List<MovieDomainModel>>());
            _movieRepository.Setup(_ => _.GetAllAsync())
              .ReturnsAsync(new List<MovieDtoModel>());

            var result = await _service.GetAllMoviesAsync();

            Assert.NotNull(result);
            Assert.IsType<List<MovieDomainModel>>(result);
        }
    }
}
