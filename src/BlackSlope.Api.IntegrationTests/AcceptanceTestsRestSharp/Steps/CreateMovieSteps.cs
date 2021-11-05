using System;
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
    public class CreateMovieSteps
    {
        private readonly ApiClient _apiClient;
        private readonly Fixture _fixture = new();
        private readonly CreateMovieContext _createMovieContext;

        private MovieViewModel _response;

        public CreateMovieSteps(ITestOutputHelper outputHelper, CreateMovieContext createMovieContext)
        {
            _apiClient = new ApiClient(outputHelper);
            _createMovieContext = createMovieContext;
        }

        [Given(@"a user creates a new movie using post movie endpoint")]
        public void GivenAUserCreatesANewMovieUsingPostMovieEndpoint()
        {
            var createMovie = new CreateMovieViewModel
            {
                Title = _fixture.Create<string>(),
                Description = $"Create Movie_{_fixture.Create<string>()}",
                ReleaseDate = Convert.ToDateTime("2010/04/05")
            };
            
            _response = _apiClient.Post<MovieViewModel>(Constants.Movies, JsonConvert.SerializeObject(createMovie));
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
