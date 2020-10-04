using DaJet.Messaging;
using DaJet.Metadata;
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
            
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<DataServersNodeController>();

            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IMessagingService, MessagingService>();
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
        //private static OneCSharpSettings OneCSharpSettings()
        //{
        //    OneCSharpSettings settings = new OneCSharpSettings();
        //    var config = new ConfigurationBuilder()
        //        .AddJsonFile("appsettings.json", optional: false)
        //        .Build();
        //    config.GetSection("OneCSharpSettings").Bind(settings);
        //    return settings;
        //}
    }
}