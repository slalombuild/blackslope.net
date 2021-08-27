using System.Threading.Tasks;
using AcceptanceTests.TestServices;
using BlackSlope.Api.Operations.Movies.ViewModels;
using TechTalk.SpecFlow;
using Xunit;

namespace AcceptanceTests.Steps
{
    [Binding]
    public class GetMoviebyIdSteps
    {
        private ITestServices _movieTestService;
        private MovieViewModel _response;
        private const int _targetMovieId = 2;

        public GetMoviebyIdSteps(ITestServices movieTestService)
        {
            _movieTestService = movieTestService;
    }

        [Given(@"the user gets movie information using get movie by id endpoint")]
        public async Task GivenTheUserGetsMovieInformationUsingGetMovieByIdEndpointAsync()
        {
            _response = await _movieTestService.GetMovieById(_targetMovieId);
        }

        [Given(@"the get movie by id response is successful")]
        public void GivenTheGetMovieByIdResponseIsSuccessful()
        {
            Assert.True((_response?.Id ?? -1) == _targetMovieId);
        }
    }
}
