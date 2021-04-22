using TechTalk.SpecFlow;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class DeleteMovieSteps
    {
        [Given(@"a deletes a recently created movie")]
        public void GivenADeletesARecentlyCreatedMovie()
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the movie is successfully deleted")]
        public void GivenTheMovieIsSuccessfullyDeleted()
        {
            ScenarioContext.Current.Pending();
        }

    }
}
