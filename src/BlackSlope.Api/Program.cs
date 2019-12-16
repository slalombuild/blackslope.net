using System.Reflection;
using BlackSlope.Api.Common.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace BlackSlope.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSerilog(Assembly.GetExecutingAssembly().GetName().Name)
                .UseStartup<Startup>();
    }
}
