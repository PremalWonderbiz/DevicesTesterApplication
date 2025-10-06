using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Interfaces;
using DeviceTesterCore.Models;

namespace DeviceTesterUI.ViewModels
{
    // Selected Device and Devices Observable
    public class DeviceListViewModel : BaseViewModel
    {
        private ObservableCollection<Device> _devices = new();
        public ObservableCollection<Device> Devices
        {
            get => _devices;
            set
            {
                if (_devices == value) return;
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }

        private Device _selectedDevice;
        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice == value) return;
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }
    }
}
