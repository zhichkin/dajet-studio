using DaJet.Data.Scripting;
using DaJet.Metadata;
using DaJet.RabbitMQ.HttpApi;
using DaJet.Studio.MVVM;
using DaJet.Studio.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DaJet.Studio
{
    public partial class App : Application
    {
        private const string SCRIPTING_CATALOG_NAME = "scripts";

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

            IFileProvider fileProvider = ConfigureFileProvider(services);

            services.AddTransient<TabViewModel>();
            services.AddTransient<TextTabViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MetadataController>();
            services.AddSingleton<RabbitMQController>();

            services.AddTransient<ExportDataRabbitMQViewModel>();
            services.AddTransient<RabbitMQExchangeListViewModel>();

            services.AddSingleton<ScriptingController>();
            services.AddTransient<ScriptEditorViewModel>();

            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IScriptingService, ScriptingService>();
            services.AddTransient<IRabbitMQHttpManager, RabbitMQHttpManager>();

            //services.AddHttpClient();
            //foreach (WebServer server in settings.WebServers)
            //{
            //    services.AddHttpClient(server.Name, client =>
            //    {
            //        client.BaseAddress = new Uri(server.Address);
            //    });
            //}
        }
        private IFileProvider ConfigureFileProvider(IServiceCollection services)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string _appCatalogPath = Path.GetDirectoryName(asm.Location);
            string _catalogPath = Path.Combine(_appCatalogPath, SCRIPTING_CATALOG_NAME);

            if (!Directory.Exists(_catalogPath))
            {
                _ = Directory.CreateDirectory(_catalogPath);
            }

            PhysicalFileProvider fileProvider = new PhysicalFileProvider(_appCatalogPath);
            services.AddSingleton<IFileProvider>(fileProvider);
            return fileProvider;
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            MainWindowViewModel viewModel = _host.Services.GetService<MainWindowViewModel>();
            new MainWindow(viewModel).Show();
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
    }
}