using System.Threading.Tasks;
using AutoFixture;
using BlackSlope.Repositories.Movies.DtoModels;
using BlackSlope.Services.Movies.DomainModels;
using Moq;
using Xunit;

namespace BlackSlope.Api.Tests.ServicesTests.MoviesTests
{
    public class MovieService_GetMovieShould : MovieServiceTestsBase
    {
        [Fact]
        public async Task ReturnAMovie()
        {
            _mapper.Setup(_ => _.Map<MovieDomainModel>(It.IsAny<MovieDtoModel>()))
                .Returns(_fixture.Create<MovieDomainModel>());
            _movieRepository.Setup(x => x.GetSingleAsync(It.IsAny<int>()))
                .ReturnsAsync(new MovieDtoModel());

            var result = await _service.GetMovieAsync(312);

            Assert.NotNull(result);
            Assert.IsType<MovieDomainModel>(result);
        }
    }
}
