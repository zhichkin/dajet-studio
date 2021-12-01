using DaJet.Metadata;
using DaJet.Studio.MVVM;
using DaJet.Studio.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;

namespace DaJet.Studio
{
    public partial class App : Application
    {
        #region Constants

        private const string WEB_SETTINGS_FILE_NAME = "web-settings.json";
        private const string WEB_SETTINGS_CATALOG_NAME = "web";

        private const string METADATA_SETTINGS_FILE_NAME = "metadata-settings.json";
        private const string METADATA_SETTINGS_CATALOG_NAME = "metadata";

        private const string SCRIPTING_SETTINGS_FILE_NAME = "scripting-settings.json";
        private const string SCRIPTING_SETTINGS_CATALOG_NAME = "scripts";

        #endregion

        private readonly IHost _host;
        public App()
        {
            _host = new HostBuilder()
                .ConfigureAppConfiguration(SetupConfiguration)
                .ConfigureServices((context, services) =>
                {
                    SetupServices(context.Configuration, services);
                })
                .Build();
        }
        private void SetupConfiguration(IConfigurationBuilder configuration)
        {
            configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
        private void SetupServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

            //IFileProvider fileProvider = ConfigureFileProvider(services);
            //WebSettings settings = ConfigureWebSettings(fileProvider, services);
            //MetadataSettings metadataSettings = ConfigureMetadataSettings(fileProvider, services);

            services.AddTransient<TabViewModel>();
            services.AddTransient<TextTabViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MetadataController>();

            services.AddTransient<ExportDataViewModel>();

            //services.AddSingleton<ScriptingController>();
            //services.AddTransient<ScriptEditorViewModel>();
            //services.AddSingleton<HttpServicesController>();
            //services.AddSingleton<MessagingController>();

            services.AddSingleton<IMetadataService, MetadataService>();
            //services.AddSingleton<IQueryExecutor, QueryExecutor>();
            //services.AddSingleton<IScriptingService, ScriptingService>();
            //services.AddSingleton<IMessagingService, MessagingService>();

            //services.AddHttpClient();
            //foreach (WebServer server in settings.WebServers)
            //{
            //    services.AddHttpClient(server.Name, client =>
            //    {
            //        client.BaseAddress = new Uri(server.Address);
            //    });
            //}
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            var viewModel = _host.Services.GetService<MainWindowViewModel>();
            (new MainWindow(viewModel)).Show();
            base.OnStartup(e);
        }
        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            base.OnExit(e);
        }
        
        //private IFileProvider ConfigureFileProvider(IServiceCollection services)
        //{
        //    Assembly asm = Assembly.GetExecutingAssembly();
        //    string _appCatalogPath = Path.GetDirectoryName(asm.Location);

        //    string[] requiredCatalogs = new string[] { "web", "scripts", "metadata" };

        //    foreach (string catalog in requiredCatalogs)
        //    {
        //        string _catalogPath = Path.Combine(_appCatalogPath, catalog);
        //        if (!Directory.Exists(_catalogPath))
        //        {
        //            _ = Directory.CreateDirectory(_catalogPath);
        //        }
        //    }
        //    PhysicalFileProvider fileProvider = new PhysicalFileProvider(_appCatalogPath);
        //    services.AddSingleton<IFileProvider>(fileProvider);
        //    return fileProvider;
        //}
        
        //private WebSettings ConfigureWebSettings(IFileProvider fileProvider, IServiceCollection services)
        //{
        //    string filePath = WebSettingsFilePath(fileProvider);

        //    WebSettings settings = new WebSettings();
        //    var config = new ConfigurationBuilder()
        //        .AddJsonFile(filePath, optional: false)
        //        .Build();
        //    config.Bind(settings);

        //    services.Configure<WebSettings>(config);

        //    return settings;
        //}
        //private string WebSettingsFilePath(IFileProvider fileProvider)
        //{
        //    string filePath = $"{WEB_SETTINGS_CATALOG_NAME}/{WEB_SETTINGS_FILE_NAME}";

        //    IFileInfo fileInfo = fileProvider.GetFileInfo(WEB_SETTINGS_CATALOG_NAME);
        //    if (!fileInfo.Exists)
        //    {
        //        Directory.CreateDirectory(fileInfo.PhysicalPath);
        //    }

        //    fileInfo = fileProvider.GetFileInfo(filePath);
        //    if (!fileInfo.Exists)
        //    {
        //        WebSettings settings = new WebSettings();
        //        JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
        //        string json = JsonSerializer.Serialize(settings, options);
        //        using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
        //        {
        //            writer.Write(json);
        //        }
        //    }

        //    return filePath;
        //}
        
        //private MetadataSettings ConfigureMetadataSettings(IFileProvider fileProvider, IServiceCollection services)
        //{
        //    string filePath = MetadataSettingsFilePath(fileProvider);

        //    MetadataSettings settings = new MetadataSettings();
        //    var config = new ConfigurationBuilder()
        //        .AddJsonFile(filePath, optional: false)
        //        .Build();
        //    config.Bind(settings);

        //    services.Configure<MetadataSettings>(config);

        //    return settings;
        //}
        //private string MetadataSettingsFilePath(IFileProvider fileProvider)
        //{
        //    string filePath = $"{METADATA_SETTINGS_CATALOG_NAME}/{METADATA_SETTINGS_FILE_NAME}";

        //    IFileInfo fileInfo = fileProvider.GetFileInfo(METADATA_SETTINGS_CATALOG_NAME);
        //    if (!fileInfo.Exists)
        //    {
        //        Directory.CreateDirectory(fileInfo.PhysicalPath);
        //    }

        //    fileInfo = fileProvider.GetFileInfo(filePath);
        //    if (!fileInfo.Exists)
        //    {
        //        MetadataSettings settings = new MetadataSettings();
        //        JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
        //        string json = JsonSerializer.Serialize(settings, options);
        //        using (StreamWriter writer = new StreamWriter(fileInfo.PhysicalPath, false, Encoding.UTF8))
        //        {
        //            writer.Write(json);
        //        }
        //    }

        //    return filePath;
        //}
    }
}