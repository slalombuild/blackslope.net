using System.Threading.Tasks;
using AcceptanceTests.Models;
using AcceptanceTests.TestServices;
using AutoFixture;
using BlackSlope.Api.Operations.Movies.ViewModels;
using TechTalk.SpecFlow;
using Xunit;

namespace AcceptanceTests.Steps
{
    [Binding]
    public class UpdateMoviebyIdSteps
    {
        private readonly ITestServices _movieTestService;
        private readonly CreateMovieContext _createMovieContext;
        private readonly Fixture _fixture = new();

        private string _updatedDescription;
        private MovieViewModel _response;

        public UpdateMoviebyIdSteps(ITestServices movieTestService,
            CreateMovieContext createMovieContext)
        {
            _movieTestService = movieTestService;
            _createMovieContext = createMovieContext;
        }

        [Given(@"a user updates the information of recently created movie with the following info")]
        public async Task GivenAUserUpdatesTheInformationOfRecentlyCreatedMovieWithTheFollowingInfo()
        {
            var id = _createMovieContext.MovieId;
            var targetMovie = await _movieTestService.GetMovieById(id);

            _updatedDescription = _fixture.Create<string>();
            var updateModel = new CreateMovieViewModel
            {
                Description = _updatedDescription,
                Title = targetMovie.Title,
                ReleaseDate = targetMovie.ReleaseDate
            };
            _response = await _movieTestService.UpdateMovieById(updateModel, id);
        }

        [Given(@"the update movie by id response is successful")]
        public void GivenTheUpdateMovieByIdResponseIsSuccessful()
        {
            Assert.True(_response.Description == _updatedDescription);
        }
    }
}
