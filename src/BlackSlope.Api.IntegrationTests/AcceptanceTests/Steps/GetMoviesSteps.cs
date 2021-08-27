using System.Threading.Tasks;
using AcceptanceTests.TestServices;
using TechTalk.SpecFlow;
using Xunit;
using BlackSlope.Api.Operations.Movies.ViewModels;

namespace AcceptanceTests.Steps
{

    [Binding]
    public class GetMoviesSteps
    {
        private ITestServices _movieTestService;
        private MovieViewModel[] _response;

        public GetMoviesSteps(ScenarioContext injectedContext, ITestServices movieTestService)
        {
            _movieTestService = movieTestService;
        }

        [Given(@"a gets all movies using get all movies endpoint")]
        public async Task GivenAGetsAllMoviesUsingGetAllMoviesEndpoint()
        {
            _response = await _movieTestService.GetMovie();      
        }

        [Given(@"the get movies endpoint is successful")]
        public void GivenTheGetMoviesEndpointIsSuccessful()
        {            
            Assert.True((_response?.Length ?? 0) > 0);
        }
    }
}
