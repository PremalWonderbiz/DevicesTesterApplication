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
        private Device _editingDevice;

        public DeviceFormView()
        {
            InitializeComponent();
            LoadPorts("Redfish");
            ResetFormWithDefaults();
        }

       

        private void AgentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AgentComboBox.SelectedItem is ComboBoxItem selectedAgent)
            {
                string agent = selectedAgent.Content.ToString();
                LoadPorts(agent);
            }
        }

        private void SaveDevice_Click(object sender, RoutedEventArgs e)
        {
            // Collect values
            string agent = (AgentComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string deviceId = DeviceIdTextBox.Text.Trim();
            string solutionId = SolutionIdTextBox.Text.Trim();
            string username = UsernameTextBox.Text.Trim();
            string ipAddress = IPTextBox.Text.Trim();
            string portText = PortComboBox.Text.Trim();
            string password = PasswordBox.Password;
            bool? useSecureConnection = SecureCheckBox.IsChecked;

            // --- UI-Level Validations ---
            if (string.IsNullOrEmpty(agent))
            {
                MessageBox.Show("Agent is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(ipAddress) || !System.Net.IPAddress.TryParse(ipAddress, out _))
            {
                MessageBox.Show("A valid IP address is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Port must be a valid number between 1 and 65535", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Username is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Password is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(solutionId))
            {
                solutionId = Guid.NewGuid().ToString();
            }

            // --- Create Device ---
            Device newDevice = new Device
            {
                Agent = agent,
                DeviceId = deviceId,
                Username = username,
                IpAddress = ipAddress,
                Port = port,
                Password = password,
                IsAuthenticated = false,
                UseSecureConnection = useSecureConnection ?? false,
            };

            // --- Save using ViewModel ---
            if (DataContext is DeviceViewModel viewModel)
            {
                // Check duplicates
                if (viewModel.Devices.Any(d => d.DeviceId == newDevice.DeviceId))
                {
                    viewModel.UpdateDevice(newDevice);

                    MessageBox.Show("Device updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                viewModel.AddDevice(newDevice); // <- goes through ViewModel, which saves to repo
            }

            MessageBox.Show("Device saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Clear form after save
            ResetFormWithDefaults();
        }


        private void ResetFormWithDefaults()
        {
            AgentComboBox.SelectedIndex = 0; 
            UsernameTextBox.Text = "admin";
            IPTextBox.Text = "192.168.0.1";
            PortComboBox.Text = "9000";
            PasswordBox.Password = "Pass@123";
            SecureCheckBox.IsChecked = true;
        }


        private void LoadPorts(string agent)
        {
            if (PortComboBox == null) return;
            PortComboBox.Items.Clear();

            switch (agent)
            {
                case "Redfish":
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "9000" });
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "Other" });
                    break;

                case "EcoRT":
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "51443" });
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "51499" });
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "Other" });
                    break;

                case "SoftdPACManager":
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "443" });
                    PortComboBox.Items.Add(new ComboBoxItem { Content = "Other" });
                    break;
            }

            // Select first port by default
            if (PortComboBox.Items.Count > 0)
                ((ComboBoxItem)PortComboBox.Items[0]).IsSelected = true;
        }

        private void PortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Other")
                {
                    PortComboBox.IsEditable = true;   // Allow typing
                    PortComboBox.Text = "";            // Clear text
                    PortComboBox.Focus();
                }
                else
                {
                    PortComboBox.IsEditable = false;  // Lock typing
                    PortComboBox.Text = selectedItem.Content.ToString();
                }
            }
        }
    }

}
