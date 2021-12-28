using DaJet.Metadata;
using DaJet.Scripting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DaJet.Http
{
    public sealed class Startup
    {
        internal const string METADATA_SETTINGS_FILE_NAME = "metadata-settings.json";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
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

            IMetadataService metadata = app.ApplicationServices.GetService<IMetadataService>();
            IOptions<MetadataSettings> settings = app.ApplicationServices.GetService<IOptions<MetadataSettings>>();
            InitializeMetadata(metadata, settings.Value);
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            IFileProvider fileProvider = ConfigureFileProvider(services);
            MetadataSettings metadataSettings = ConfigureMetadataSettings(fileProvider, services);

            services.AddControllers();
            services.AddSingleton<IQueryExecutor, QueryExecutor>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IScriptingService, ScriptingService>();
        }
        private IFileProvider ConfigureFileProvider(IServiceCollection services)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string _appCatalogPath = Path.GetDirectoryName(asm.Location);
            PhysicalFileProvider fileProvider = new PhysicalFileProvider(_appCatalogPath);
            services.AddSingleton<IFileProvider>(fileProvider);
            return fileProvider;
        }
        private MetadataSettings ConfigureMetadataSettings(IFileProvider fileProvider, IServiceCollection services)
        {
            string filePath = MetadataSettingsFilePath(fileProvider);

            MetadataSettings settings = new MetadataSettings();
            var config = new ConfigurationBuilder()
                .AddJsonFile(filePath, optional: false)
                .Build();
            config.Bind(settings);

            services.Configure<MetadataSettings>(config);

            return settings;
        }
        private string MetadataSettingsFilePath(IFileProvider fileProvider)
        {
            string filePath = $"{METADATA_SETTINGS_FILE_NAME}";

            IFileInfo fileInfo = fileProvider.GetFileInfo(filePath);
            if (!fileInfo.Exists)
            {
                MetadataSettings settings = new MetadataSettings();
                JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
                {
                    writer.Write(json);
                }
            }

            return filePath;
        }
        private void InitializeMetadata(IMetadataService metadata, MetadataSettings settings)
        {
            foreach (DatabaseServer server in settings.Servers)
            {
                //_ = Parallel.ForEach(server.Databases, InitializeMetadata);
                foreach (DatabaseInfo database in server.Databases)
                {
                    metadata.Initialize(server, database);
                }
            }
        }
    }
}