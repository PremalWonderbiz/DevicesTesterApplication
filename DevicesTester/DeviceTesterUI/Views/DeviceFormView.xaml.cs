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
            LoadPorts("Redfish");
        }

        public void SetDevice(Device device)
        {
            AgentComboBox.Text = device.Agent;
            DeviceIdTextBox.Text = device.DeviceId;
            UsernameTextBox.Text = device.Username;
            IPTextBox.Text = device.IpAddress;
            PortComboBox.Text = device.Port.ToString();
            PasswordBox.Password = device.Password;
        }

        private void AgentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AgentComboBox.SelectedItem is ComboBoxItem selectedAgent)
            {
                string agent = selectedAgent.Content.ToString();
                LoadPorts(agent);
            }
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
