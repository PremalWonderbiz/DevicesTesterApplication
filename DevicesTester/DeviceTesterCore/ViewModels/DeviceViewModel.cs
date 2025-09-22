using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        /// <summary>
        /// List of all devices loaded from the repository.
        /// </summary>
        public ObservableCollection<Device> Devices { get; set; } = new();

        private string _deviceJson;

        /// <summary>
        /// Current JSON (static or dynamic) shown for selected device.
        /// Used to preview device details.
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

        private Device _selectedDevice;

        /// <summary>
        /// Device currently selected in the list view.
        /// Creates an editable copy (EditingDevice) or 
        /// resets to default if selection is cleared.
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

        /// <summary>
        /// Temporary device object used for editing.
        /// This is either a copy of the selected device or a default new one.
        /// </summary>
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

                    // Refresh ports whenever Agent changes
                    LoadPorts(_editingDevice.Agent, isNewDevice: string.IsNullOrEmpty(_editingDevice.DeviceId));
                }

                OnPropertyChanged(nameof(EditingDevice));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        /// <summary>
        /// Handles property changes in EditingDevice to trigger error checks
        /// and update available ports when Agent changes.
        /// </summary>
        private void EditingDevice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Device.Agent))
            {
                LoadPorts(EditingDevice.Agent, isNewDevice: string.IsNullOrEmpty(EditingDevice.DeviceId));
            }

            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// True if the EditingDevice has validation errors.
        /// Used to enable/disable the Save button.
        /// </summary>
        public bool HasErrors => EditingDevice != null && EditingDevice.HasErrors;

        /// <summary>
        /// List of supported agent types.
        /// </summary>
        public ObservableCollection<string> AvailableAgents { get; } = new()
        {
            "Redfish",
            "EcoRT",
            "SoftdPACManager"
        };

        /// <summary>
        /// List of ports available for the selected agent.
        /// Bound to the Port ComboBox in the form.
        /// </summary>
        public ObservableCollection<string> AvailablePorts { get; } = new();

        /// <summary>
        /// Loads available ports for the given agent type.
        /// If editing an existing device, tries to keep its assigned port.
        /// Otherwise, assigns a default port.
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

        /// <summary>
        /// Creates a new default device with preconfigured values.
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
                Username = "account1",
                Password = "Pass@123",
                UseSecureConnection = true,
                IsAuthenticated = false
            };
        }

        /// <summary>
        /// Initializes the ViewModel with default agents and loads devices.
        /// </summary>
        public DeviceViewModel(IDeviceRepository repo)
        {
            _repo = repo;
            _ = LoadDevicesAsync();
            EditingDevice = CreateDefaultDevice();
        }

        /// <summary>
        /// Reloads devices from the repository asynchronously.
        /// </summary>
        public async Task LoadDevicesAsync()
        {
            Devices.Clear();
            var devicesFromFile = await _repo.LoadDevicesAsync();

            foreach (var device in devicesFromFile)
                Devices.Add(device);
        }

        /// <summary>
        /// Adds a new device and persists the list to the repository.
        /// </summary>
        public async Task AddDeviceAsync(Device device)
        {
            Devices.Insert(0, device);
            await _repo.SaveDevicesAsync(Devices);
        }

        /// <summary>
        /// Updates the currently selected device and saves the changes.
        /// </summary>
        public async Task UpdateDeviceAsync(Device device)
        {
            var index = Devices.IndexOf(SelectedDevice);
            if (index >= 0) Devices[index] = device;

            await _repo.SaveDevicesAsync(Devices);
        }

        /// <summary>
        /// Deletes a device and persists the updated list.
        /// </summary>
        public async Task DeleteDeviceAsync(Device device)
        {
            Devices.Remove(device);
            await _repo.SaveDevicesAsync(Devices);
        }

        /// <summary>
        /// Dummy authentication method that randomly succeeds or fails.
        /// Updates device authentication state and persists it.
        /// </summary>
        public async Task<bool> AuthenticateDeviceAsync(Device device)
        {
            bool result = new Random().Next(0, 2) == 1;
            device.IsAuthenticated = result;

            await _repo.SaveDevicesAsync(Devices);
            OnPropertyChanged(nameof(Devices));
            return result;
        }

        /// <summary>
        /// Event triggered when a property changes (INotifyPropertyChanged).
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises property changed notification for a given property.
        /// </summary>
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
