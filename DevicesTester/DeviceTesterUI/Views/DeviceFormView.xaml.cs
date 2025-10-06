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

    }
}
