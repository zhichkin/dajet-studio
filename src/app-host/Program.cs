namespace DaJet.Studio.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            WebApplication app = builder.Build();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}

// dotnet publish -f net10.0 --os win -a x64 -c Release -o bin/publish -p:PublishSingleFile=true -p:IsWebConfigTransformDisabled=true