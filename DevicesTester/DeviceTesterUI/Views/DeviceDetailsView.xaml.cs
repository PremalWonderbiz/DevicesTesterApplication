using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml;
using DeviceTesterCore.Models;
using DeviceTesterUI.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceTesterUI.Views
{
    /// <summary>
    /// Interaction logic for DeviceDetailsView.xaml
    /// </summary>
    public partial class DeviceDetailsView : UserControl
    {
        private string _resourceInputText = string.Empty;
        private DeviceViewModel ViewModel => DataContext as DeviceViewModel;

        public DeviceDetailsView()
        {
            InitializeComponent();
            DeviceJsonTextBox.Text = string.Empty;
        }


        private void ManageResources_Click(object sender, RoutedEventArgs e)
        {
            var popup = new ResourceInputWindow
            {
                Owner = Window.GetWindow(this),
                // Pass previous data to popup
                ResourceText = _resourceInputText
            };

            if (popup.ShowDialog() == true)
            {
                _resourceInputText = popup.ResourceText; // Save persistent data
                MessageBox.Show("Saved resource data:\n" + _resourceInputText);
            }
        }

        private void GetStaticInfo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDevice == null)
            {
                DeviceJsonTextBox.Text = "No device selected.";
                return;
            }
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = System.IO.Path.Combine(exeDir, "DummyData", "dummyStaticData.json");
                if (!File.Exists(filePath))
                {
                    DeviceJsonTextBox.Text = "File not found: " + filePath;
                    return;
                }

                string jsonContent = File.ReadAllText(filePath);

                // Pretty-print JSON
                var parsedJson = JsonConvert.DeserializeObject(jsonContent);
                string prettyJson = JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);

                DeviceJsonTextBox.Text = prettyJson;
            }
            catch (Exception ex)
            {
                DeviceJsonTextBox.Text = "Error reading JSON: " + ex.Message;
            }
        }

        private void GetDynamicInfo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDevice == null)
            {
                DeviceJsonTextBox.Text = "No device selected.";
                return;
            }
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = System.IO.Path.Combine(exeDir, "DummyData", "dummyDynamicData.json");
                if (!File.Exists(filePath))
                {
                    DeviceJsonTextBox.Text = "File not found: " + filePath;
                    return;
                }

                string jsonContent = File.ReadAllText(filePath);

                // Pretty-print JSON
                var parsedJson = JsonConvert.DeserializeObject(jsonContent);
                string prettyJson = JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);

                DeviceJsonTextBox.Text = prettyJson;
            }
            catch (Exception ex)
            {
                DeviceJsonTextBox.Text = "Error reading JSON: " + ex.Message;
            }
        }

    }
}
