using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DeviceTesterCore.Interfaces;
using DeviceTesterUI.Commands;

namespace DeviceTesterCore.Models
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private readonly IDeviceRepository _repo;
        private readonly IDeviceDataProvider _dataProvider;

        public ObservableCollection<Device> Devices { get; set; } = new();

        private Device _selectedDevice;
        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged(nameof(SelectedDevice));
                    OnSelectedDeviceChanged();
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
                {
                    _editingDevice.ErrorsChanged -= EditingDevice_ErrorsChanged;
                    _editingDevice.PropertyChanged -= EditingDevice_PropertyChanged;
                }

                _editingDevice = value;

                if (_editingDevice != null)
                {
                    _editingDevice.ErrorsChanged += EditingDevice_ErrorsChanged;
                    _editingDevice.PropertyChanged += EditingDevice_PropertyChanged;

                    // Ensure ports are loaded for the current agent
                    LoadPorts(_editingDevice.Agent, string.IsNullOrEmpty(_editingDevice.DeviceId));
                }

                OnPropertyChanged(nameof(EditingDevice));
                SaveCommand.RaiseCanExecuteChanged();
                ClearCommand.RaiseCanExecuteChanged();
            }
        }

        private void EditingDevice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Device.Agent))
            {
                LoadPorts(EditingDevice.Agent, string.IsNullOrEmpty(EditingDevice.DeviceId));
            }
        }

        private void EditingDevice_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnSelectedDeviceChanged()
        {
            StopDynamicUpdates();

            if (SelectedDevice != null)
                EditingDevice = new Device(SelectedDevice); // copy constructor
            else
                EditingDevice = CreateDefaultDevice();

            DeviceJson = string.Empty;
            ErrorMessage = string.Empty;
        }

        public ObservableCollection<string> AvailableAgents { get; } = new()
        {
            "Redfish", "EcoRT", "SoftdPACManager"
        };

        public ObservableCollection<string> AvailablePorts { get; } = new();

        private string _deviceJson;
        public string DeviceJson
        {
            get => _deviceJson;
            set { _deviceJson = value; OnPropertyChanged(nameof(DeviceJson)); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        // ================= Commands =================
        public ActionCommand SaveCommand { get; }
        public ActionCommand ClearCommand { get; }
        public ActionCommand AuthenticateCommand { get; }
        public ActionCommand DeleteCommand { get; }

        public DeviceViewModel(IDeviceRepository repo, IDeviceDataProvider dataProvider)
        {
            _repo = repo;
            _dataProvider = dataProvider;

            SaveCommand = new ActionCommand(async _ => await SaveDeviceAsync(), CanSave);
            ClearCommand = new ActionCommand(Clear);
            AuthenticateCommand = new ActionCommand(async param => await AuthenticateDeviceAsync(param as Device),
                                            param => param is Device);
            DeleteCommand = new ActionCommand(async param => await DeleteDeviceAsync(param as Device),
                                      param => param is Device);

            _ = LoadDevicesAsync(true);
            EditingDevice = CreateDefaultDevice();
        }

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
                    if(!AvailablePorts.Contains(EditingDevice.Port))
                    {
                        AvailablePorts.Add(EditingDevice.Port);
                        SortAvailablePorts();
                    }   
                }
                else
                {
                    EditingDevice.Port = AvailablePorts.FirstOrDefault() ?? "0000";
                }
            }
        }

        private void SortAvailablePorts()
        {
            var sorted = AvailablePorts
                .OrderBy(p => p == "Other" ? int.MaxValue : int.Parse(p))
                .ToList();

            AvailablePorts.Clear();
            foreach (var p in sorted)
                AvailablePorts.Add(p);
        }

        private async Task SaveDeviceAsync()
        {
            if (EditingDevice == null) return;

            // Duplicate IP + Port check
            bool duplicateIpPort = Devices.Any(d =>
                d.IpAddress == EditingDevice.IpAddress &&
                d.Port == EditingDevice.Port &&
                d.DeviceId != EditingDevice.DeviceId);

            if (duplicateIpPort)
            {
                ErrorMessage = "A device with the same IP and Port already exists!";
                return;
            }

            ErrorMessage = string.Empty;

            // Generate IDs if empty
            if (string.IsNullOrEmpty(EditingDevice.DeviceId))
                EditingDevice.DeviceId = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(EditingDevice.SolutionId))
                EditingDevice.SolutionId = Guid.NewGuid().ToString();

            // Add or update using copy constructor
            var existing = Devices.FirstOrDefault(d => d.DeviceId == EditingDevice.DeviceId);
            if (existing != null)
            {
                var index = Devices.IndexOf(existing);
                Devices[index] = new Device(EditingDevice);
                await _repo.SaveDevicesAsync(Devices);
                MessageBox.Show("Device updated successfully!");
            }
            else
            {
                EditingDevice.IsAuthenticated = false;
                Devices.Insert(0, new Device(EditingDevice));
                await _repo.SaveDevicesAsync(Devices);
                MessageBox.Show("Device saved successfully!");
            }

            Clear(new object());
        }

        private bool CanSave(object obj)
        {
            if (EditingDevice == null) return false;
            return Validator.TryValidateObject(EditingDevice, new ValidationContext(EditingDevice), null, true);
        }

        private void Clear(object obj)
        {
            ErrorMessage = string.Empty;
            SelectedDevice = null;                
            EditingDevice = CreateDefaultDevice();
            EditingDevice.Agent = AvailableAgents.First();
            EditingDevice.Port = AvailablePorts.FirstOrDefault();
        }

        private async Task AuthenticateDeviceAsync(Device device)
        {
            if (device == null) return;

            bool result = new Random().Next(0, 2) == 1;
            device.IsAuthenticated = result;

            await _repo.SaveDevicesAsync(Devices);
            OnPropertyChanged(nameof(Devices));

            MessageBox.Show(result ? "Authentication succeeded" : "Authentication failed");
        }

        private async Task DeleteDeviceAsync(Device device)
        {
            if (device == null) return;

            var confirm = MessageBox.Show(
                "Are you sure you want to delete?",
                "Delete Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                Devices.Remove(device);
                await _repo.SaveDevicesAsync(Devices);

                if (SelectedDevice == device)
                    EditingDevice = CreateDefaultDevice();

                MessageBox.Show("Device deleted successfully");
            }
        }

        public Device CreateDefaultDevice()
        {
            return new Device();
        }
            

        public async Task LoadDevicesAsync(bool initial = false)
        {
            Devices.Clear();
            var devicesFromFile = await _repo.LoadDevicesAsync();
            foreach (var device in devicesFromFile)
            {
                if (initial)
                    device.IsAuthenticated = null;
                Devices.Add(device);
            }

            if (initial)
            {
                await Task.Delay(2000);
                AuthenticateAllDevices();
                await _repo.SaveDevicesAsync(Devices);
                OnPropertyChanged(nameof(Devices));
            }
        }

        private void AuthenticateAllDevices()
        {
            if(Devices is not null && Devices.Count > 0)
            {
                foreach (var device in Devices)
                {
                    bool result = new Random().Next(0, 2) == 1;
                    device.IsAuthenticated = result;
                }
            }
        }

        // ================= Dynamic Data =================
        public async Task GetStaticDataAsync()
        {
            StopDynamicUpdates();
            if (SelectedDevice != null)
            {
                DeviceJson = null; //loading spinner
                await Task.Delay(1000);
                DeviceJson = await _dataProvider.GetStaticAsync(SelectedDevice);
            }
                
        }

        public void StartDynamicUpdates(Action<string> onDataReceived)
        {
            if (SelectedDevice == null) return;
            _dataProvider.StartDynamicUpdates(SelectedDevice, onDataReceived);
        }

        public void StopDynamicUpdates()
        {
            if (SelectedDevice == null) return;
            _dataProvider.StopDynamicUpdates(SelectedDevice);
            DeviceJson = string.Empty;
        }

        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
