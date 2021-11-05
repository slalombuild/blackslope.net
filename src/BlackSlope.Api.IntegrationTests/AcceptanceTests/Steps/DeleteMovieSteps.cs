using System.Threading.Tasks;
using AcceptanceTests.Models;
using AcceptanceTests.TestServices;
using TechTalk.SpecFlow;
using Xunit;

namespace AcceptanceTests.Steps
{
    [Binding]
    public class DeleteMovieSteps
    {
        private readonly ITestServices _movieTestService;
        private readonly CreateMovieContext _createMovieContext;

        public DeleteMovieSteps(ITestServices movieTestService,
            CreateMovieContext createMovieContext)
        {
            _movieTestService = movieTestService;
            _createMovieContext = createMovieContext;
        }

        [Given(@"a deletes a recently created movie")]
        public async Task GivenADeletesARecentlyCreatedMovie()
        {
            var id = _createMovieContext.MovieId;
            await _movieTestService.DeleteMovie(id);
        }

        [Given(@"the movie is successfully deleted")]
        public async Task GivenTheMovieIsSuccessfullyDeleted()
        {
            var id = _createMovieContext.MovieId;
            var targetMovie = await _movieTestService.GetMovieById(id);
            Assert.Null(targetMovie);
        }
    }
}
