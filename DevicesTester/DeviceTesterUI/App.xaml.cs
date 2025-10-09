using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using DeviceTesterCore.Interfaces;
using DeviceTesterCore.Models;
using DeviceTesterServices.Repositories;
using DeviceTesterServices.Services;
using DeviceTesterUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeviceTesterUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            ServiceCollection services = new ();

            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // Register Repositories
            services.AddSingleton<IDeviceRepository, DeviceRepository>();

            // Register ViewModels
            services.AddSingleton<DeviceViewModel>();
            services.AddSingleton<IDeviceDataProvider>(provider =>
            {
                // Specify your dynamic files
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] dynamicFiles =
                [
                    Path.Combine(exeDir, "DummyData", "DynamicData2.json"),
                    Path.Combine(exeDir, "DummyData", "DynamicData3.json"),
                    Path.Combine(exeDir, "DummyData", "DynamicData4.json"),
                    Path.Combine(exeDir, "DummyData", "DynamicData5.json"),
                    Path.Combine(exeDir, "DummyData", "DynamicData1.json")
                ];

                return new JsonDeviceDataProvider(dynamicFiles, intervalMs: 2000);
            });

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
