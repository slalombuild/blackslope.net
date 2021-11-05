using AcceptanceTestsRestSharp.Clients;
using AcceptanceTestsRestSharp.Helpers;
using AcceptanceTestsRestSharp.Models;
using AutoFixture;
using BlackSlope.Api.Operations.Movies.ViewModels;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class UpdateMoviebyIdSteps
    {
        private readonly ApiClient _apiClient;
        private readonly CreateMovieContext _createMovieContext;
        private readonly Fixture _fixture = new();

        private string _updatedDescription;
        private MovieViewModel _response;

        public UpdateMoviebyIdSteps(ITestOutputHelper outputHelper,
            CreateMovieContext createMovieContext)
        {
            _apiClient = new ApiClient(outputHelper);
            _createMovieContext = createMovieContext;
        }

        [Given(@"a user updates the information of recently created movie with the following info")]
        public void GivenAUserUpdatesTheInformationOfRecentlyCreatedMovieWithTheFollowingInfo()
        {
            var id = _createMovieContext.MovieId;
            var targetMovie = _apiClient.Get<MovieViewModel>($"{Constants.Movies}/{id}");

            _updatedDescription = _fixture.Create<string>();
            var updateModel = new CreateMovieViewModel
            {
                Description = _updatedDescription,
                Title = targetMovie.Title,
                ReleaseDate = targetMovie.ReleaseDate
            };
            _response = _apiClient.Put<MovieViewModel>($"{Constants.Movies}/{id}", JsonConvert.SerializeObject(updateModel));
        }

        [Given(@"the update movie by id response is successful")]
        public void GivenTheUpdateMovieByIdResponseIsSuccessful()
        {
            Assert.True(_response.Description == _updatedDescription);
        }
    }
}
