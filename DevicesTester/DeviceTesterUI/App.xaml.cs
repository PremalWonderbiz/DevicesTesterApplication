using System.Configuration;
using System.Data;
using System.Windows;
using DeviceTesterCore.Interfaces;
using DeviceTesterCore.Models;
using DeviceTesterServices.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DeviceTesterUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();

            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Register Repositories
            services.AddSingleton<IDeviceRepository, DeviceRepository>();

            // Register ViewModels
            services.AddSingleton<DeviceViewModel>();

            // Register MainWindow
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }
    }

}
