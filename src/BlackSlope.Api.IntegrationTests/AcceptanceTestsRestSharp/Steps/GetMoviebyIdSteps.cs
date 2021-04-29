using AcceptanceTestsRestSharp.Clients;
using AcceptanceTestsRestSharp.Helpers;
using BlackSlope.Api.Operations.Movies.ViewModels;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class GetMoviebyIdSteps
    {
        private readonly ApiClient _apiClient;
        private MovieViewModel _response;
        private const int _targetMovieId = 2;

        public GetMoviebyIdSteps(ITestOutputHelper outputHelper)
        {
            _apiClient = new ApiClient(outputHelper);
        }

        [Given(@"the user gets movie information using get movie by id endpoint")]
        public void GivenTheUserGetsMovieInformationUsingGetMovieByIdEndpointAsync()
        {
            _response = _apiClient.Get<MovieViewModel>($"{Constants.Movies}/{_targetMovieId}");
        }

        [Given(@"the get movie by id response is successful")]
        public void GivenTheGetMovieByIdResponseIsSuccessful()
        {
            Assert.True((_response?.Id ?? -1) == _targetMovieId);
        }
    }
}
