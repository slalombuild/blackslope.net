using BlackSlope.Hosts.Api.Common.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;


namespace BlackSlope.Hosts.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Build().Run();
        }

        public static IWebHostBuilder BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSerilog("BlackSlope.Hosts.Api.Configuration")
                .UseStartup<Startup>();
    }
}
