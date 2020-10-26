using DaJet.Messaging;
using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.Studio.MVVM;
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

            SetupFileProvider(services);

            services.AddTransient<TabViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MetadataController>();
            services.AddSingleton<ScriptingController>();
            services.AddTransient<ScriptEditorViewModel>();
            services.AddSingleton<HttpServicesController>();

            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IQueryExecutor, QueryExecutor>();
            services.AddSingleton<IScriptingService, ScriptingService>();
            services.AddSingleton<IMessagingService, MessagingService>();

            services.AddHttpClient("test-server", client =>
            {
                client.BaseAddress = new Uri("http://localhost:5000");
            });
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
        private void SetupFileProvider(IServiceCollection services)
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

            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(_appCatalogPath));
        }
    }
}