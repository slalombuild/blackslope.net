using System.Threading.Tasks;
using AcceptanceTests.Models;
using AcceptanceTests.TestServices;
using TechTalk.SpecFlow;

namespace AcceptanceTests.Steps
{
    [Binding]
    public class DeleteMovieSteps
    {

        private readonly ITestServices _movieTestService;
        private readonly CreateMovieContext _createMovieContext;
        private readonly ScenarioContext _scenarioContext;

        public DeleteMovieSteps(ScenarioContext injectedContext,
          ITestServices movieTestService, CreateMovieContext createMovieContext)
        {
            _movieTestService = movieTestService;
            _createMovieContext = createMovieContext;
            _scenarioContext = injectedContext;
        }

        [Given(@"a deletes a recently created movie")]
        public async Task GivenADeletesARecentlyCreatedMovie()
        {
            var id = _createMovieContext.MovieId;
            await _movieTestService.DeleteMovie(id);
        }

        [Given(@"the movie is successfully deleted")]
        public void GivenTheMovieIsSuccessfullyDeleted()
        {
            _scenarioContext.Pending();
        }
    }
}
