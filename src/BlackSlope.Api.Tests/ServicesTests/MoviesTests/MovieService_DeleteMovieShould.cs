using System.Threading.Tasks;
using Moq;
using Xunit;

namespace BlackSlope.Api.Tests.ServicesTests.MoviesTests
{
    public class MovieService_DeleteMovieShould : MovieServiceTestsBase
    {
        [Fact]
        public async Task DeleteMovie_successfully()
        {
            var targetId = 123;
            _movieRepository.Setup(x => x.DeleteAsync(targetId))
             .ReturnsAsync(targetId);

            var result = await _service.DeleteMovieAsync(targetId);

            Assert.Equal(targetId, result);
        }
    }
}
