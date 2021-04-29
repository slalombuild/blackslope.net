using AcceptanceTestsRestSharp.Clients;
using AcceptanceTestsRestSharp.Helpers;
using AcceptanceTestsRestSharp.Models;
using BlackSlope.Api.Operations.Movies.ViewModels;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class DeleteMovieSteps
    {
        private readonly ApiClient _apiClient;
        private readonly CreateMovieContext _createMovieContext;

        public DeleteMovieSteps(ITestOutputHelper outputHelper,
            CreateMovieContext createMovieContext)
        {
            _apiClient = new ApiClient(outputHelper);
            _createMovieContext = createMovieContext;
        }

        [Given(@"a deletes a recently created movie")]
        public void GivenADeletesARecentlyCreatedMovie()
        {
            var id = _createMovieContext.MovieId;
            _apiClient.Delete<MovieViewModel>($"{Constants.Movies}/{id}");
        }

        [Given(@"the movie is successfully deleted")]
        public void GivenTheMovieIsSuccessfullyDeleted()
        {
            var id = _createMovieContext.MovieId;
            var targetMovie = _apiClient.Get<MovieViewModel>($"{Constants.Movies}/{id}");
            Assert.Null(targetMovie);
        }
    }
}
