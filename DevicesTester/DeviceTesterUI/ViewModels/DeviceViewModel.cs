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
        private string _selectedAgent;
        public string SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                    _selectedAgent = value;
                    OnPropertyChanged(nameof(SelectedAgent));
                    if (EditingDevice != null)
                        EditingDevice.Agent = _selectedAgent;
                    LoadPorts(_selectedAgent);
            }
        }

        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (_selectedPort != value)
                {
                    _selectedPort = value;
                    OnPropertyChanged(nameof(SelectedPort));
                    if (EditingDevice != null)
                        EditingDevice.Port = _selectedPort;
                }
            }
        }
        private Device _selectedDevice;
        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));

                // If a row is selected → copy it
                // If no row selected → load defaults
                if (_selectedDevice != null)
                {
                    EditingDevice = new Device(_selectedDevice);
                    SelectedAgent = _selectedDevice.Agent;
                    SelectedPort = _selectedDevice.Port;
                }
                else
                {
                    EditingDevice = CreateDefaultDevice();
                    SelectedAgent = AvailableAgents.First();
                }
                    
            }
        }


        private Device _editingDevice;
        public Device EditingDevice
        {
            get => _editingDevice;
            set { _editingDevice = value; OnPropertyChanged(nameof(EditingDevice)); }
        }

        private ObservableCollection<string> _availableAgents;
        public ObservableCollection<string> AvailableAgents
        {
            get => _availableAgents;
            set { _availableAgents = value; OnPropertyChanged(nameof(AvailableAgents)); }
        }

        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>();

        private void LoadPorts(string agent)
        {
            AvailablePorts.Clear();

            switch (agent)
            {
                case "Redfish":
                    AvailablePorts.Add("9000");
                    AvailablePorts.Add("Other");
                    break;

                case "EcoRT":
                    AvailablePorts.Add("51443");
                    AvailablePorts.Add("51499");
                    AvailablePorts.Add("Other");
                    break;

                case "SoftdPACManager":
                    AvailablePorts.Add("443");
                    AvailablePorts.Add("Other");
                    break;
            }
            // Select first port by default
            if (AvailablePorts.Count > 0)
                SelectedPort = AvailablePorts[0];
        }

        public Device CreateDefaultDevice()
        {
            return new Device
            {
                Agent = "Redfish",
                DeviceId = String.Empty,
                SolutionId = String.Empty,
                IpAddress = "127.0.0.1",
                Port = "9000",
                Username = "account",
                Password = "Pass@123",
                UseSecureConnection = true,
                IsAuthenticated = false
            };
        }

        public DeviceViewModel()
        {
            AvailableAgents = new ObservableCollection<string>
            {
                "Redfish",
                "EcoRT",
                "SoftdPACManager"
            };
            SelectedAgent = AvailableAgents.First();
            LoadDevices();
            EditingDevice = CreateDefaultDevice();
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
