using DaJet.Metadata;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DaJet.Http
{
    public sealed class Program
    {
        private const string METADATA_CATALOG_NAME = "metadata";
        private const string METADATA_SETTINGS_FILE_NAME = "metadata-settings.json";

        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            ConfigureMetadataService(host);
            host.Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseKestrel()
                    .UseStartup<Startup>();
                });
        }
        //var host = new WebHostBuilder()
        //        .UseKestrel()
        //        .UseUrls(url)
        //        .UseStartup<Startup>()
        //        .ConfigureLogging(logging =>
        //        {
        //            logging.ClearProviders();
        //            logging.AddConsole();
        //        })
        //        .Build();
        //host.Run();
        private static void ConfigureMetadataService(IHost host)
        {
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    IMetadataService metadata = services.GetRequiredService<IMetadataService>();
                    IFileProvider fileProvider = ConfigureFileProvider();
                    MetadataServiceSettings settings = InitializeMetadataSettings(fileProvider);
                    InitializeMetadataService(settings, metadata);
                    logger.LogInformation("Metadata is initialized.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred.");
                }
            }
        }
        private static IFileProvider ConfigureFileProvider()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string _appCatalogPath = Path.GetDirectoryName(asm.Location);

            string[] requiredCatalogs = new string[] { "scripts", "metadata" };

            foreach (string catalog in requiredCatalogs)
            {
                string _catalogPath = Path.Combine(_appCatalogPath, catalog);
                if (!Directory.Exists(_catalogPath))
                {
                    _ = Directory.CreateDirectory(_catalogPath);
                }
            }

            return new PhysicalFileProvider(_appCatalogPath);
        }
        private static MetadataServiceSettings InitializeMetadataSettings(IFileProvider fileProvider)
        {
            MetadataServiceSettings settings = new MetadataServiceSettings();

            IFileInfo fileInfo = fileProvider.GetFileInfo(METADATA_CATALOG_NAME);
            if (!fileInfo.Exists) { Directory.CreateDirectory(fileInfo.PhysicalPath); }

            string json = "{}";
            fileInfo = fileProvider.GetFileInfo($"{METADATA_CATALOG_NAME}/{METADATA_SETTINGS_FILE_NAME}");
            if (fileInfo.Exists)
            {
                using (StreamReader reader = new StreamReader(fileInfo.PhysicalPath, Encoding.UTF8))
                {
                    json = reader.ReadToEndAsync().Result;
                }
                settings = JsonSerializer.Deserialize<MetadataServiceSettings>(json);
            }
            else
            {
                SaveMetadataServiceSettings(settings, fileProvider);
            }

            return settings;
        }
        private static void SaveMetadataServiceSettings(MetadataServiceSettings settings, IFileProvider fileProvider)
        {
            IFileInfo fileInfo = fileProvider.GetFileInfo($"{METADATA_CATALOG_NAME}/{METADATA_SETTINGS_FILE_NAME}");

            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
            {
                writer.Write(json);
            }
        }
        private static void InitializeMetadataService(MetadataServiceSettings settings, IMetadataService metadata)
        {
            foreach (DatabaseServer server in settings.Servers)
            {
                //_ = Parallel.ForEach(server.Databases, InitializeMetadata);
                foreach (DatabaseInfo database in server.Databases)
                {
                    IMetadataProvider provider = metadata.GetMetadataProvider(database);
                    provider.UseServer(server);
                    provider.UseDatabase(database);
                    provider.InitializeMetadata(database);
                }
            }
        }
    }
}