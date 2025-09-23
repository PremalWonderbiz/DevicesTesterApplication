using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeviceTesterCore.Interfaces;

namespace DeviceTesterCore.Models
{
    /// <summary>
    /// ViewModel that manages device data, editing state, 
    /// available agent/port options, and persistence via repository.
    /// Implements INotifyPropertyChanged for UI binding.
    /// </summary>
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private readonly IDeviceRepository _repo;
        private readonly IDeviceDataProvider _dataProvider;

        public ObservableCollection<Device> Devices { get; set; } = new();

        private string _deviceJson;
        public string DeviceJson
        {
            get => _deviceJson;
            set
            {
                _deviceJson = value;
                OnPropertyChanged(nameof(DeviceJson));
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

                StopDynamicUpdates(); // stop dynamic on selection change

                if (_selectedDevice != null)
                {
                    EditingDevice = new Device(_selectedDevice);
                    DeviceJson = string.Empty;
                }
                else
                {
                    EditingDevice = CreateDefaultDevice();
                    DeviceJson = string.Empty;
                }
            }
        }

        private Device _editingDevice;
        public Device EditingDevice
        {
            get => _editingDevice;
            set
            {
                if (_editingDevice != null)
                    _editingDevice.PropertyChanged -= EditingDevice_PropertyChanged;

                _editingDevice = value;

                if (_editingDevice != null)
                {
                    _editingDevice.PropertyChanged += EditingDevice_PropertyChanged;
                    LoadPorts(_editingDevice.Agent, string.IsNullOrEmpty(_editingDevice.DeviceId));
                }

                OnPropertyChanged(nameof(EditingDevice));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void EditingDevice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Device.Agent))
            {
                LoadPorts(EditingDevice.Agent, string.IsNullOrEmpty(EditingDevice.DeviceId));
            }

            OnPropertyChanged(nameof(HasErrors));
        }

        public bool HasErrors => EditingDevice != null && EditingDevice.HasErrors;

        public ObservableCollection<string> AvailableAgents { get; } = new()
        {
            "Redfish",
            "EcoRT",
            "SoftdPACManager"
        };

        public ObservableCollection<string> AvailablePorts { get; } = new();

        private void LoadPorts(string agent, bool isNewDevice = true)
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

            if (EditingDevice != null)
            {
                if (!isNewDevice)
                {
                    EditingDevice.Port = AvailablePorts.Contains(EditingDevice.Port)
                        ? EditingDevice.Port
                        : (AvailablePorts.Contains("Other") ? "Other" : AvailablePorts.FirstOrDefault());
                }
                else
                {
                    EditingDevice.Port = AvailablePorts.FirstOrDefault();
                }
            }
        }

        public Device CreateDefaultDevice()
        {
            return new Device
            {
                Agent = "Redfish",
                DeviceId = string.Empty,
                SolutionId = string.Empty,
                IpAddress = "127.0.0.1",
                Port = "9000",
                Username = "",
                Password = "",
                UseSecureConnection = true,
                IsAuthenticated = false
            };
        }

        public DeviceViewModel(IDeviceRepository repo, IDeviceDataProvider dataProvider)
        {
            _repo = repo;
            _dataProvider = dataProvider;

            _ = LoadDevicesAsync();
            EditingDevice = CreateDefaultDevice();
        }

        public async Task LoadDevicesAsync()
        {
            Devices.Clear();
            var devicesFromFile = await _repo.LoadDevicesAsync();

            foreach (var device in devicesFromFile)
                Devices.Add(device);
        }

        public async Task AddDeviceAsync(Device device)
        {
            Devices.Insert(0, device);
            await _repo.SaveDevicesAsync(Devices);
        }

        public async Task UpdateDeviceAsync(Device device)
        {
            if (device == null) return;

            var existingDevice = Devices.FirstOrDefault(d => d.DeviceId == device.DeviceId);
            if (existingDevice != null)
            {
                var index = Devices.IndexOf(existingDevice);
                Devices[index] = device;

                await _repo.SaveDevicesAsync(Devices);
            }
            else
            {
                throw new InvalidOperationException($"Device with ID {device.DeviceId} not found.");
            }
        }

        public async Task DeleteDeviceAsync(Device device)
        {
            Devices.Remove(device);
            await _repo.SaveDevicesAsync(Devices);
        }

        public async Task<bool> AuthenticateDeviceAsync(Device device)
        {
            bool result = new Random().Next(0, 2) == 1;
            device.IsAuthenticated = result;

            await _repo.SaveDevicesAsync(Devices);
            OnPropertyChanged(nameof(Devices));
            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        // ================= Dynamic Data Integration ==================

        /// <summary>
        /// Fetch static data for currently selected device
        /// </summary>
        public async Task GetStaticDataAsync()
        {
            StopDynamicUpdates();
            if (SelectedDevice != null)
                DeviceJson = await _dataProvider.GetStaticAsync(SelectedDevice);
        }

        /// <summary>
        /// Start dynamic updates for the selected device
        /// </summary>
        public void StartDynamicUpdates(Action<string> onDataReceived)
        {
            if (SelectedDevice == null) return;

            _dataProvider.StartDynamicUpdates(SelectedDevice, onDataReceived);
        }

        /// <summary>
        /// Stop dynamic updates
        /// </summary>
        public void StopDynamicUpdates()
        {
            if (SelectedDevice == null) return;

            _dataProvider.StopDynamicUpdates(SelectedDevice);
            DeviceJson = string.Empty;
        }
    }
}
