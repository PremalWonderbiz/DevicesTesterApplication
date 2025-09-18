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
    /// Interaction logic for DeviceFormView.xaml
    /// </summary>
    public partial class DeviceFormView : UserControl
    {

        public DeviceFormView()
        {
            InitializeComponent();
        }


        private void SaveDevice_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DeviceViewModel vm)
            {
                var deviceToSave = vm.EditingDevice;

                // --- UI-Level Validations ---
                if (string.IsNullOrEmpty(deviceToSave.Agent))
                {
                    MessageBox.Show("Agent is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(deviceToSave.IpAddress) || !System.Net.IPAddress.TryParse(deviceToSave.IpAddress, out _))
                {
                    MessageBox.Show("A valid IP address is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(PortComboBox.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Port must be a valid number between 1 and 65535", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(deviceToSave.Username))
                {
                    MessageBox.Show("Username is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(deviceToSave.Password))
                {
                    MessageBox.Show("Password is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(deviceToSave.DeviceId))
                {
                    deviceToSave.DeviceId = Guid.NewGuid().ToString();
                }

                if (string.IsNullOrEmpty(deviceToSave.SolutionId))
                {
                    deviceToSave.SolutionId = Guid.NewGuid().ToString();
                }

                deviceToSave.Port = PortComboBox.Text;

                // check existing
                if (vm.Devices.Any(d => d.DeviceId == deviceToSave.DeviceId))
                {                    
                    vm.UpdateDevice(new Device(deviceToSave));
                    MessageBox.Show("Device updated successfully!");
                }
                else
                {
                    vm.AddDevice(new Device(deviceToSave));
                    MessageBox.Show("Device saved successfully!");
                }

                // Reset form with defaults
                vm.EditingDevice = vm.CreateDefaultDevice();
            }
        }

        private void PortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PortComboBox.SelectedItem is string selectedPort) // <- string, not ComboBoxItem
            {
                if (selectedPort == "Other")
                {
                    PortComboBox.IsEditable = true;
                    PortComboBox.Text = "";
                    PortComboBox.Focus();
                }
                else
                {
                    PortComboBox.IsEditable = false;
                    PortComboBox.Text = selectedPort;
                }
            }
        }
    }

}
