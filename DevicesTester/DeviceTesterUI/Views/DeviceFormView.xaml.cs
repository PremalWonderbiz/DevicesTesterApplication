using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        /// <summary>
        /// Handles Save button click → validates input, creates/updates device, and persists asynchronously.
        /// </summary>
        //private async void SaveDevice_Click(object sender, RoutedEventArgs e)
        //{
        //    if (DataContext is DeviceViewModel vm && !vm.HasErrors)
        //    {
        //        var deviceToSave = vm.EditingDevice;

        //        // 🔹 Duplicate IP + Port check (ignore the same DeviceId)
        //        bool duplicateIpPort = vm.Devices.Any(d =>
        //            d.IpAddress == deviceToSave.IpAddress &&
        //            d.Port == deviceToSave.Port &&
        //            d.DeviceId != deviceToSave.DeviceId);

        //        if (duplicateIpPort)
        //        {
        //            vm.ErrorMessage = "A device with the same IP and Port already exists!";
        //            return;
        //        }

        //        vm.ErrorMessage = string.Empty;

        //        if (string.IsNullOrEmpty(deviceToSave.DeviceId))
        //            deviceToSave.DeviceId = Guid.NewGuid().ToString();
        //        if (string.IsNullOrEmpty(deviceToSave.SolutionId))
        //            deviceToSave.SolutionId = Guid.NewGuid().ToString();

        //        deviceToSave.Port = vm.EditingDevice.Port;

        //        if (vm.Devices.Any(d => d.DeviceId == deviceToSave.DeviceId))
        //        {
        //            await vm.UpdateDeviceAsync(new Device(deviceToSave));
        //            MessageBox.Show("Device updated successfully!");
        //        }
        //        else
        //        {
        //            deviceToSave.IsAuthenticated = false;
        //            await vm.AddDeviceAsync(new Device(deviceToSave));
        //            MessageBox.Show("Device saved successfully!");
        //        }

        //        // Reset form and selection
        //        vm.SelectedDevice = null;
        //        vm.EditingDevice = vm.CreateDefaultDevice();
        //        vm.EditingDevice.Agent = vm.AvailableAgents.First();
        //        vm.EditingDevice.Port = vm.AvailablePorts.FirstOrDefault();
        //    }
        //}


        /// <summary>
        /// Handles port selection change → toggles "Other" mode for manual entry.
        /// </summary>
        private void PortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PortComboBox.SelectedItem is string selectedPort)
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

        private void ClearDevice_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DeviceViewModel vm)
            {
                vm.ErrorMessage = string.Empty;
                vm.SelectedDevice = null;                  // <-- reset selection
                vm.EditingDevice = vm.CreateDefaultDevice();
                vm.EditingDevice.Agent = vm.AvailableAgents.First();
                vm.EditingDevice.Port = vm.AvailablePorts.FirstOrDefault();
            }
        }
    }
}
