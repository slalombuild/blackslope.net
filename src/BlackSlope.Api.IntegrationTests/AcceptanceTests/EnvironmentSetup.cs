using AcceptanceTests.Helpers;
using AcceptanceTests.TestServices;
using System.IO;
using Microsoft.Extensions.Configuration;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using BoDi;

namespace AcceptanceTests
{
   
    [Binding]
    public sealed class EnvironmentSetup
    {
        private readonly IObjectContainer objectContainer;
        private readonly ITestOutputHelper _outputHelper;

        public EnvironmentSetup(IObjectContainer objectContainer, ITestOutputHelper outputHelper)
        {
            this.objectContainer = objectContainer;
            _outputHelper = outputHelper;
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
           var configuration = new ConfigurationBuilder()
                                     .SetBasePath(Directory.GetCurrentDirectory())
                                     .AddJsonFile("appsettings.test.json")
                                     .Build(); 
            Environments.BaseUrl = configuration["BlackSlopeHost"];
            Environments.DBConnection = configuration["DBConnectionString"];
        }

        [BeforeScenario]
        public void InitializeWebServices()
        {
            var movieService = new MovieService(_outputHelper);
            objectContainer.RegisterInstanceAs<ITestServices>(movieService);
        }
    }
}
