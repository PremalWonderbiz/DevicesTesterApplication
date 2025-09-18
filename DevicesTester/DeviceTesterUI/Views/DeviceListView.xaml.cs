using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeviceTesterCore.Models;

namespace DeviceTesterUI.Views
{
    /// <summary>
    /// Interaction logic for DeviceListView.xaml
    /// </summary>
    public partial class DeviceListView : UserControl
    {
        public DeviceListView()
        {
            InitializeComponent();

            var deviceList = new List<Device>();

            for (int i = 1; i <= 50; i++)  // 50 rows to test scrolling
            {
                deviceList.Add(new Device
                {
                    Agent = (i % 3 == 0) ? "Redfish" : (i % 3 == 1) ? "EcoRT" : "SoftdPACManager",
                    DeviceId = $"D{i:000}",
                    IpAddress = $"192.168.0.{i}",
                    Port = 9000 + i,
                    IsAuthenticated = (i % 2 == 0) ? true : false,
                    Username = "account1",
                    Password = "password"
                });
            }

            // Bind the DataGrid
            DeviceDataGrid.ItemsSource = deviceList;

            DeviceDataGrid.SelectionChanged += DeviceDataGrid_SelectionChanged;
        }

        // Expose selected device via an event
        public event Action<Device?> DeviceSelected;

        private void DeviceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedDevice = DeviceDataGrid.SelectedItem as Device;
            DeviceSelected?.Invoke(selectedDevice);
        }

        private void Authenticate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Device device)
            {
                MessageBox.Show($"Authenticate clicked for {device.DeviceId}");
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Device device)
            {
                MessageBox.Show($"Delete clicked for {device.DeviceId}");
                // Optional: remove from collection
                // ((ObservableCollection<DeviceItem>)DeviceDataGrid.ItemsSource).Remove(device);
            }
        }

    }
}
