using DaJet.Metadata;
using DaJet.Scripting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;

namespace DaJet.Http
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IQueryExecutor, QueryExecutor>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IScriptingService, ScriptingService>();

            IFileProvider fileProvider = ConfigureFileProvider(services);
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        private IFileProvider ConfigureFileProvider(IServiceCollection services)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string _appCatalogPath = Path.GetDirectoryName(asm.Location);

            string[] requiredCatalogs = new string[] { "web", "scripts", "metadata" };

            foreach (string catalog in requiredCatalogs)
            {
                string _catalogPath = Path.Combine(_appCatalogPath, catalog);
                if (!Directory.Exists(_catalogPath))
                {
                    _ = Directory.CreateDirectory(_catalogPath);
                }
            }
            PhysicalFileProvider fileProvider = new PhysicalFileProvider(_appCatalogPath);
            services.AddSingleton<IFileProvider>(fileProvider);
            return fileProvider;
        }
    }
}