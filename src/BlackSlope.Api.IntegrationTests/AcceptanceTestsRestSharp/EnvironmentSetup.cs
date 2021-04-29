using AcceptanceTestsRestSharp.Helpers;
using System.IO;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using BoDi;
using Microsoft.Extensions.Configuration;

namespace AcceptanceTestsRestSharp
{

    [Binding]
    public sealed class EnvironmentSetup
    {
        private IObjectContainer _objectContainer;
        private readonly ITestOutputHelper _outputHelper;

        public EnvironmentSetup(IObjectContainer objectContainer, ITestOutputHelper outputHelper)
        {
            this._objectContainer = objectContainer;
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
    }
}
