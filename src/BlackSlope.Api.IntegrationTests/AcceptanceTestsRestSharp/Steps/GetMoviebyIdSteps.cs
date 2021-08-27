using TechTalk.SpecFlow;

namespace AcceptanceTestsRestSharp.Steps
{
    [Binding]
    public class GetMoviebyIdSteps
    {
        [Given(@"the user gets movie information using get movie by id endpoint")]
        public void GivenTheUserGetsMovieInformationUsingGetMovieByIdEndpoint()
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the get movie by id response is successful")]
        public void GivenTheGetMovieByIdResponseIsSuccessful()
        {
            ScenarioContext.Current.Pending();
        }

    }
}
