using AcceptanceTestsRestSharp.Clients;
using AcceptanceTestsRestSharp.Helpers;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace AcceptanceTestsRestSharp.Steps
{

    [Binding]
    public class GetMoviesSteps : BaseSteps
    {       
       private ClientResponse<MovieViewModel> _Response;
        private readonly ITestOutputHelper _output;


        public GetMoviesSteps(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _output = outputHelper;

        }

        [Given(@"a gets all movies using get all movies endpoint")]
        public void GivenAGetsAllMoviesUsingGetAllMoviesEndpoint()
        {
            _Response =  Get<MovieViewModel>(Constants.Movies);

        }

        [Given(@"the get movies endpoint is sucsessfull")]
        public void GivenTheGetMoviesEndpointIsSucsessfull()
        {
            Assert.Equal("OK",_Response.Status.ToString());
        }

    }
}
