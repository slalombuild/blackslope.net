using System;
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
    public class CreateMovieSteps
    {
        private readonly ITestServices _movieTestService;
        private readonly CreateMovieContext _createMovieContext;
        private MovieViewModel _response;
        private readonly Fixture _fixture = new();

        public CreateMovieSteps(ScenarioContext injectedContext,
            ITestServices movieTestService, CreateMovieContext createMovieContext)
        {
            _movieTestService = movieTestService;
            _createMovieContext = createMovieContext;
        }

        [Given(@"a user creates a new movie using post movie endpoint")]
        public async Task GivenAUserCreatesANewMovieUsingPostMovieEndpoint()
        {
            var createMovie = new CreateMovieViewModel
            {
                Title = _fixture.Create<string>(),
                Description = "Create Movie Test",
                ReleaseDate = Convert.ToDateTime("2010/04/05")
            };
            _response = await _movieTestService.CreateMovie(createMovie);
        }

        [Given(@"the movie is successfully created")]
        public void GivenTheMovieIsSuccessfullyCreated()
        {
            Assert.NotNull(_response);
            Assert.True(_response.Id > 0);
        }

        [Given(@"a user gets the movie id of recently created movie")]
        public void GivenAUserGetsTheMovieIdOfRecentlyCreatedMovie()
        {
            _createMovieContext.MovieId = (int)_response.Id;
        }
    }
}
