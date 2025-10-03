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
        private string _staticResourceInput = string.Empty;
        private string _dynamicResourceInput = string.Empty;
        private DeviceViewModel _vm;
        private DeviceViewModel ViewModel => DataContext as DeviceViewModel;

        public DeviceDetailsView()
        {
            InitializeComponent();
            DeviceJsonTextBox.Text = string.Empty;
            DataContextChanged += DeviceDetailsView_DataContextChanged;
        }

        private void DeviceDetailsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.PropertyChanged -= Vm_PropertyChanged;

            _vm = DataContext as DeviceViewModel;

            if (_vm != null)
                _vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceViewModel.SelectedDevice))
            {
                _vm.StopDynamicUpdates();

                DeviceJsonTextBox.Text = string.Empty;
            }
        }

        private void ManageResources_Click(object sender, RoutedEventArgs e)
        {
            var popup = new ResourceInputWindow
            {
                Owner = Window.GetWindow(this),

                StaticData = _staticResourceInput,   
                DynamicData = _dynamicResourceInput  
            };

            if (popup.ShowDialog() == true)
            {
                _staticResourceInput = popup.StaticData;
                _dynamicResourceInput = popup.DynamicData;

                MessageBox.Show(
                    "Configurations saved"
                );
            }
        }


        //private async void GetStaticInfo_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_vm.SelectedDevice == null)
        //    {
        //        DeviceJsonTextBox.Text = "No device selected.";
        //        return;
        //    }

        //    try
        //    {
        //        DeviceGetStaticDataBtn.IsEnabled = false;
        //        DeviceGetDynamicDataBtn.IsEnabled = false;
        //        DeviceManageResourcesBtn.IsEnabled = false;
        //        LoadingSpinner.Visibility = Visibility.Visible;
        //        _vm.StopDynamicUpdates();

        //        await _vm.GetStaticDataAsync();

        //        if (string.IsNullOrWhiteSpace(_vm.DeviceJson))
        //        {
        //            DeviceJsonTextBox.Text = "Static data is empty.";
        //        }
        //        else
        //        {
        //            try
        //            {
        //                var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(_vm.DeviceJson);
        //                DeviceJsonTextBox.Text = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
        //            }
        //            catch (Exception jsonEx)
        //            {
        //                DeviceJsonTextBox.Text = $"Error parsing JSON: {jsonEx.Message}";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DeviceJsonTextBox.Text = $"Error fetching static data: {ex.Message}";
        //    }
        //    finally
        //    {
        //        DeviceGetStaticDataBtn.IsEnabled = true;
        //        DeviceGetDynamicDataBtn.IsEnabled = true;
        //        DeviceManageResourcesBtn.IsEnabled = true;
        //        LoadingSpinner.Visibility = Visibility.Collapsed;
        //    }
        //}

        //private void GetDynamicInfo_Click(object sender, RoutedEventArgs e)
        //{
        //    var firstTime = true;
        //    if (_vm.SelectedDevice == null)
        //    {
        //        DeviceJsonTextBox.Text = "No device selected.";
        //        return;
        //    }

        //    try
        //    {
        //        _vm.StopDynamicUpdates();

        //        _vm.StartDynamicUpdates(async content =>
        //        {
        //            try
        //            {
        //                if (firstTime)
        //                {
        //                    Dispatcher.Invoke(() =>
        //                    {
        //                        DeviceGetStaticDataBtn.IsEnabled = false;
        //                        DeviceGetDynamicDataBtn.IsEnabled = false;
        //                        DeviceManageResourcesBtn.IsEnabled = false;
        //                        LoadingSpinner.Visibility = Visibility.Visible;
        //                        _vm.DeviceJson = null;
        //                    });
        //                }

        //                await Dispatcher.Invoke(async () =>
        //                {
        //                    if (string.IsNullOrWhiteSpace(content))
        //                    {
        //                        DeviceJsonTextBox.Text = "Dynamic data is empty.";
        //                        return;
        //                    }

        //                    try
        //                    {
        //                        if (firstTime)
        //                        {
        //                            await Task.Delay(2000); // simulate loader delay
        //                            firstTime = false; // now mark it done
        //                        }
        //                        var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
        //                        _vm.DeviceJson = Newtonsoft.Json.JsonConvert.SerializeObject(
        //                            parsedJson, Newtonsoft.Json.Formatting.Indented);
        //                    }
        //                    catch (Exception jsonEx)
        //                    {
        //                        DeviceJsonTextBox.Text = $"Error parsing dynamic JSON: {jsonEx.Message}";
        //                    }
        //                });
        //            }
        //            catch (Exception dispatchEx)
        //            {
        //                DeviceJsonTextBox.Text = $"Error updating UI: {dispatchEx.Message}";
        //            }
        //            finally
        //            {
        //                if (!firstTime)
        //                {
        //                    Dispatcher.Invoke(() =>
        //                    {
        //                        DeviceGetStaticDataBtn.IsEnabled = true;
        //                        DeviceGetDynamicDataBtn.IsEnabled = true;
        //                        DeviceManageResourcesBtn.IsEnabled = true;
        //                        LoadingSpinner.Visibility = Visibility.Collapsed;
        //                    });
        //                }
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        DeviceJsonTextBox.Text = $"Error starting dynamic updates: {ex.Message}";
        //    }
        //}
    }
}
