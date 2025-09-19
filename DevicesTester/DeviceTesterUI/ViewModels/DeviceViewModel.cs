using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DeviceTesterCore.Interfaces;
using DeviceTesterServices.Repositories;

namespace DeviceTesterCore.Models
{
    /// <summary>
    /// ViewModel that manages devices, selection state, and device editing logic.
    /// It also handles agent/port options and provides CRUD operations via repository.
    /// </summary>
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private readonly IDeviceRepository _repo;

        /// <summary>
        /// List of all devices loaded from repository.
        /// </summary>
        public ObservableCollection<Device> Devices { get; set; } = new();

        private string _deviceJson;

        /// <summary>
        /// Current JSON (static or dynamic) shown for selected device.
        /// </summary>
        public string DeviceJson
        {
            get => _deviceJson;
            set
            {
                _deviceJson = value;
                OnPropertyChanged(nameof(DeviceJson));
            }
        }

        private string _selectedAgent;

        /// <summary>
        /// Currently selected agent type (e.g., Redfish, EcoRT, etc.).
        /// Updating this will also refresh available ports.
        /// </summary>
        public string SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                _selectedAgent = value;
                OnPropertyChanged(nameof(SelectedAgent));

                if (EditingDevice != null)
                {
                    EditingDevice.Agent = _selectedAgent;

                    if (!string.IsNullOrEmpty(EditingDevice.DeviceId))
                    {
                        LoadPorts(_selectedAgent, isNewDevice: false);
                        return;
                    }
                }

                LoadPorts(_selectedAgent);
            }
        }

        private string _selectedPort;

        /// <summary>
        /// Currently selected port for the active device.
        /// </summary>
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

        /// <summary>
        /// Device currently selected in the list.
        /// When updated, creates a copy for editing or loads a default device if null.
        /// </summary>
        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));

                if (_selectedDevice != null)
                {
                    EditingDevice = new Device(_selectedDevice);
                    SelectedAgent = _selectedDevice.Agent;
                    SelectedPort = _selectedDevice.Port;
                    DeviceJson = string.Empty;
                }
                else
                {
                    EditingDevice = CreateDefaultDevice();
                    SelectedAgent = AvailableAgents.First();
                    DeviceJson = string.Empty;
                }
            }
        }

        private Device _editingDevice;

        /// <summary>
        /// A temporary device used for editing (copy of selected device).
        /// </summary>
        public Device EditingDevice
        {
            get => _editingDevice;
            set
            {
                _editingDevice = value;
                OnPropertyChanged(nameof(EditingDevice));
            }
        }

        private ObservableCollection<string> _availableAgents;

        /// <summary>
        /// List of supported agent types.
        /// </summary>
        public ObservableCollection<string> AvailableAgents
        {
            get => _availableAgents;
            set
            {
                _availableAgents = value;
                OnPropertyChanged(nameof(AvailableAgents));
            }
        }

        /// <summary>
        /// List of ports available for the selected agent.
        /// </summary>
        public ObservableCollection<string> AvailablePorts { get; } = new();

        /// <summary>
        /// Loads available ports for the given agent type.
        /// If device exists, tries to keep its assigned port.
        /// Otherwise, assigns the first default port.
        /// </summary>
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
                    // Existing device: keep actual port if available, else fallback to "Other"
                    SelectedPort = AvailablePorts.Contains(EditingDevice.Port)
                        ? EditingDevice.Port
                        : (AvailablePorts.Contains("Other") ? "Other" : AvailablePorts.FirstOrDefault());
                }
                else
                {
                    // New device: select first port
                    SelectedPort = AvailablePorts.FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// Creates a default device with preconfigured values.
        /// </summary>
        public Device CreateDefaultDevice()
        {
            return new Device
            {
                Agent = "Redfish",
                DeviceId = string.Empty,
                SolutionId = string.Empty,
                IpAddress = "127.0.0.1",
                Port = "9000",
                Username = "account",
                Password = "Pass@123",
                UseSecureConnection = true,
                IsAuthenticated = false
            };
        }

        /// <summary>
        /// Initializes the ViewModel with default agents and loads devices from repository.
        /// </summary>
        public DeviceViewModel(IDeviceRepository repo)
        {
            _repo = repo;

            AvailableAgents = new ObservableCollection<string>
            {
                "Redfish",
                "EcoRT",
                "SoftdPACManager"
            };

            SelectedAgent = AvailableAgents.First();
            _ = LoadDevicesAsync();
            EditingDevice = CreateDefaultDevice();
        }

        /// <summary>
        /// Reloads devices from repository asynchronously.
        /// </summary>
        public async Task LoadDevicesAsync()
        {
            Devices.Clear();
            var devicesFromFile = await _repo.LoadDevicesAsync();

            foreach (var device in devicesFromFile)
                Devices.Add(device);
        }

        /// <summary>
        /// Adds a new device to the list (on top) and persists it asynchronously.
        /// </summary>
        public async Task AddDeviceAsync(Device device)
        {
            Devices.Insert(0, device);
            await _repo.SaveDevicesAsync(Devices);
        }

        /// <summary>
        /// Updates the selected device in the list and persists changes asynchronously.
        /// </summary>
        public async Task UpdateDeviceAsync(Device device)
        {
            var index = Devices.IndexOf(SelectedDevice);
            if (index >= 0) Devices[index] = device;

            await _repo.SaveDevicesAsync(Devices);
        }

        /// <summary>
        /// Deletes the given device and saves repository state asynchronously.
        /// </summary>
        public async Task DeleteDeviceAsync(Device device)
        {
            Devices.Remove(device);
            await _repo.SaveDevicesAsync(Devices);
        }

        /// <summary>
        /// Dummy authentication method that randomly succeeds or fails.
        /// Updates the device authentication state and persists it.
        /// </summary>
        public async Task<bool> AuthenticateDeviceAsync(Device device)
        {
            bool result = new Random().Next(0, 2) == 1;
            device.IsAuthenticated = result;

            await _repo.SaveDevicesAsync(Devices);

            // Notify UI of changes
            OnPropertyChanged(nameof(Devices));
            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper to raise property changed events.
        /// </summary>
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
