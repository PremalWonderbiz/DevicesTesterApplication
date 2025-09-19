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
using DeviceTesterServices.Repositories;

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
        }

        private void DeviceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void Authenticate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Device device)
            {
                if (device.IsAuthenticated)
                {
                    MessageBox.Show($"{device.DeviceId} is already authenticated");
                    return;
                }

                if (DataContext is DeviceViewModel viewModel)
                {
                    bool result = await viewModel.AuthenticateDeviceAsync(device);
                    MessageBox.Show($"{device.DeviceId} authentication result: {result}");
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Device device)
            {
                var viewModel = DataContext as DeviceViewModel;
                if (viewModel != null)
                {
                    await viewModel.DeleteDeviceAsync(device);
                    MessageBox.Show($"{device.DeviceId} deleted successfully");
                }
            }
        }
    }
}
