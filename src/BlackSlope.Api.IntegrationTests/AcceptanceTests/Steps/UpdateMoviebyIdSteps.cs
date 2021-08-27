using TechTalk.SpecFlow;

namespace AcceptanceTests.Steps
{
    [Binding]
    public class UpdateMoviebyIdSteps
    {
        private readonly ScenarioContext _scenarioContext;

        public UpdateMoviebyIdSteps(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }

        [Given(@"a user updates the information of recently created movie with the following info")]
        public void GivenAUserUpdatesTheInformationOfRecentlyCreatedMovieWithTheFollowingInfo()
        {
            _scenarioContext.Pending();
        }

        [Given(@"the update movie by id response is successful")]
        public void GivenTheUpdateMovieByIdResponseIsSuccessful()
        {
            _scenarioContext.Pending();
        }

    }
}
