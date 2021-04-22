using TechTalk.SpecFlow;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class CreateMovieSteps
    {

        [Given(@"a user creates a new movie using post movie endpoint")]
        public void GivenAUserCreatesANewMovieUsingPostMovieEndpoint()
        {
            ScenarioContext.Current.Pending();
        }



        [Given(@"the movie is successfully created")]
        public void GivenTheMovieIsSuccessfullyCreated()
        {
            ScenarioContext.Current.Pending();
        }


        [Given(@"a user gets the movie id of recently created movie")]
        public void GivenAUserGetsTheMovieIdOfRecentlyCreatedMovie()
        {
            ScenarioContext.Current.Pending();
        }

    }
}
