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

        private IObjectContainer objectContainer;
        private readonly ITestOutputHelper _OutputHelper;

        public EnvironmentSetup(IObjectContainer objectContainer, ITestOutputHelper outputHelper)
        {
            this.objectContainer = objectContainer;
            _OutputHelper = outputHelper;

        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            
           var configuration = new ConfigurationBuilder()
                                     //.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
                                     .SetBasePath(Directory.GetCurrentDirectory())
                                     .AddJsonFile("appTestSettings.json")
                                     .Build(); 
               Environments.BaseUrl = configuration["BlackSlopeHost"];
          //  Environments.BaseUrl = "http://localhost:5010";
            Environments.DBConnection = configuration["DBConnectionString"];
                

        }



    }
}
