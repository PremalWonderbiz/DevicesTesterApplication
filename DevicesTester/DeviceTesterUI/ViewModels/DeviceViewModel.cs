using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterServices.Repositories;

namespace DeviceTesterCore.Models
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private readonly DeviceRepository _repo = new();
        public ObservableCollection<Device> Devices { get; set; } = new();
        private Device _selectedDevice;

        public Device SelectedDevice
        {
            get => _selectedDevice;
            set { _selectedDevice = value; OnPropertyChanged(nameof(SelectedDevice)); }
        }

        public DeviceViewModel()
        {
            LoadDevices();
        }

        public void LoadDevices()
        {
            Devices.Clear();
            var devicesFromFile = _repo.LoadDevices();
            foreach (var device in devicesFromFile) Devices.Add(device);
        }

        public void AddDevice(Device device)
        {
            Devices.Add(device);          
            _repo.SaveDevices(Devices);   
        }

        public void UpdateDevice(Device device)
        {
            var index = Devices.IndexOf(SelectedDevice);
            if (index >= 0) Devices[index] = device;
            _repo.SaveDevices(Devices);
        }

        public void DeleteDevice(Device device)
        {
            Devices.Remove(device);
            _repo.SaveDevices(Devices);
        }

        public bool AuthenticateDevice(Device device)
        {
            bool result = new Random().Next(0, 2) == 1;

            device.IsAuthenticated = result;

            _repo.SaveDevices(Devices);

            // Notify UI (refresh binding)
            OnPropertyChanged(nameof(Devices));

            return result;
            }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

}
