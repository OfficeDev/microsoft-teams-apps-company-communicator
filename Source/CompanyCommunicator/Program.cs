using CompanyCommunicator.Bot;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CompanyCommunicator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
