using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddHttpClient();
            builder.Services.AddFluentUIComponents();
            builder.Services.AddSingleton<AppState>();

            WebAssemblyHost host = builder.Build();

            await InitializeAppState(host.Services);

            await host.RunAsync();
        }
        private static async Task InitializeAppState(IServiceProvider services)
        {
            AppState app = services.GetRequiredService<AppState>();

            await app.InitializeOnStartup();
        }
    }
}