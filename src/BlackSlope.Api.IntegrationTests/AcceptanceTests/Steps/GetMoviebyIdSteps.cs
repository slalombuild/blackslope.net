using TechTalk.SpecFlow;

namespace AcceptanceTests.Steps
{
    [Binding]
    public class GetMoviebyIdSteps
    {
        private readonly ScenarioContext _scenarioContext;

        public GetMoviebyIdSteps(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }

        [Given(@"the user gets movie information using get movie by id endpoint")]
        public void GivenTheUserGetsMovieInformationUsingGetMovieByIdEndpoint()
        {
            _scenarioContext.Pending();
        }

        [Given(@"the get movie by id response is successful")]
        public void GivenTheGetMovieByIdResponseIsSuccessful()
        {
            _scenarioContext.Pending();
        }

    }
}
