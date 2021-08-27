using System.Collections.Generic;
using System.Linq;
using AcceptanceTestsRestSharp.Clients;
using AcceptanceTestsRestSharp.Helpers;
using BlackSlope.Api.Operations.Movies.ViewModels;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class GetMoviesSteps
    {       
        private IEnumerable<MovieViewModel> _response;
        private readonly ApiClient _apiClient;

        public GetMoviesSteps(ITestOutputHelper outputHelper)
        {
            _apiClient = new ApiClient(outputHelper);
        }

        [Given(@"a gets all movies using get all movies endpoint")]
        public void GivenAGetsAllMoviesUsingGetAllMoviesEndpoint()
        {
            _response = _apiClient.Get<IEnumerable<MovieViewModel>>(Constants.Movies);
        }

        [Given(@"the get movies endpoint is successful")]
        public void GivenTheGetMoviesEndpointIsSuccessful()
        {
            Assert.True(_response.Any());
        }
    }
}
