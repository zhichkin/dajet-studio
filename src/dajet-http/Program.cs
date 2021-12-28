using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DaJet.Http
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            host.Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder
                    .UseKestrel()
                    .UseStartup<Startup>();
                });
        }
    }
}