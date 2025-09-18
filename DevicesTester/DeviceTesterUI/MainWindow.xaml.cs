using System.Text;
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
using DeviceTesterUI.Views;

namespace DeviceTesterUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Assuming x:Name="DeviceListView" and "DeviceFormView" are set in XAML
            DeviceListView.DeviceSelected += OnDeviceSelected;
        }

        private void OnDeviceSelected(Device? device)
        {
            if (device != null)
            {
                DeviceFormView.SetDevice(device);
            }
        }
    }
}