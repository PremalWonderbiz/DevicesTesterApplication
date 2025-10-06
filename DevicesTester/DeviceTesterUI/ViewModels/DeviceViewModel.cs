using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DeviceTesterCore.Interfaces;
using DeviceTesterUI.Commands;
using DeviceTesterUI.ViewModels;
using DeviceTesterUI.Windows;
using Newtonsoft.Json;

namespace DeviceTesterCore.Models
{
    public class DeviceViewModel : BaseViewModel
    {
        #region DeviceListViewModel refactoring
        // new sub-VM instance (field initializer ensures availability before ctor runs)
        private readonly DeviceListViewModel _list = new DeviceListViewModel();
        
        public DeviceListViewModel List => _list;

        #endregion

        #region DeviceFormViewModel refactoring
        //private Device _editingDevice;
        //public Device EditingDevice
        //{
        //    get => _editingDevice;
        //    set
        //    {
        //        if (_editingDevice != null)
        //        {
        //            _editingDevice.ErrorsChanged -= EditingDevice_ErrorsChanged;
        //            _editingDevice.PropertyChanged -= EditingDevice_PropertyChanged;
        //        }

        //        _editingDevice = value;

        //        if (_editingDevice != null)
        //        {
        //            _editingDevice.ErrorsChanged += EditingDevice_ErrorsChanged;
        //            _editingDevice.PropertyChanged += EditingDevice_PropertyChanged;

        //            // Ensure ports are loaded for the current agent
        //            LoadPorts(_editingDevice.Agent, string.IsNullOrEmpty(_editingDevice.DeviceId));
        //        }

        //        OnPropertyChanged(nameof(EditingDevice));
        //        SaveCommand.RaiseCanExecuteChanged();
        //        ClearCommand.RaiseCanExecuteChanged();
        //    }
        //}

        //public ObservableCollection<string> AvailableAgents { get; } = new()
        //{
        //    "Redfish", "EcoRT", "SoftdPACManager"
        //};

        //public ObservableCollection<string> AvailablePorts { get; } = new();

        //private string _errorMessage;
        //public string ErrorMessage
        //{
        //    get => _errorMessage;
        //    set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        //}

        private readonly DeviceFormViewModel _form = new DeviceFormViewModel();
        public DeviceFormViewModel Form => _form;

        // wrapper for EditingDevice that preserves the original side-effects (no method bodies changed)
        public Device EditingDevice
        {
            get => Form.EditingDevice;
            set
            {
                if (Form.EditingDevice != null)
                {
                    Form.EditingDevice.ErrorsChanged -= EditingDevice_ErrorsChanged;
                    Form.EditingDevice.PropertyChanged -= EditingDevice_PropertyChanged;
                }

                Form.EditingDevice = value;

                if (Form.EditingDevice != null)
                {
                    Form.EditingDevice.ErrorsChanged += EditingDevice_ErrorsChanged;
                    Form.EditingDevice.PropertyChanged += EditingDevice_PropertyChanged;

                    // Ensure ports are loaded for the current agent
                    LoadPorts(Form.EditingDevice.Agent, string.IsNullOrEmpty(Form.EditingDevice.DeviceId));
                }

                OnPropertyChanged(nameof(EditingDevice));
                SaveCommand.RaiseCanExecuteChanged();
                ClearCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> AvailableAgents { get => Form.AvailableAgents; }

        public ObservableCollection<string> AvailablePorts { get => Form.AvailablePorts; }

        public string ErrorMessage
        {
            get => Form.ErrorMessage;
            set { Form.ErrorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        #endregion

        #region DeviceDetailsViewModel refactoring
        //private string _deviceJson;
        //public string DeviceJson
        //{
        //    get => _deviceJson;
        //    set { _deviceJson = value; OnPropertyChanged(nameof(DeviceJson)); }
        //}

        private readonly DeviceDetailsViewModel _details = new DeviceDetailsViewModel();
        public DeviceDetailsViewModel Details => _details;

        public string DeviceJson
        {
            get => Details.DeviceJson;
            set { Details.DeviceJson = value; OnPropertyChanged(nameof(DeviceJson)); }
        }

        private string _staticResourceInput;

        private string _dynamicResourceInput;

        #endregion

        private readonly IDeviceRepository _repo;
        private readonly IDeviceDataProvider _dataProvider;

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

            if (List.SelectedDevice != null)
                EditingDevice = new Device(List.SelectedDevice); // copy constructor
            else
                EditingDevice = CreateDefaultDevice();

            DeviceJson = string.Empty;
            ErrorMessage = string.Empty;
        }  

        // ================= Commands =================
        public ActionCommand SaveCommand { get; }
        public ActionCommand ClearCommand { get; }
        public ActionCommand AuthenticateCommand { get; }
        public ActionCommand DeleteCommand { get; }
        public ActionCommand GetStaticDataCommand { get; }
        public ActionCommand GetDynamicDataCommand { get; }
        public ActionCommand ManageResourcesCommand { get; }

        public DeviceViewModel(IDeviceRepository repo, IDeviceDataProvider dataProvider)
        {
            _repo = repo;
            _dataProvider = dataProvider;

            List.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DeviceListViewModel.SelectedDevice))
                {
                    OnSelectedDeviceChanged();
                    UpdateCommandStates();
                }
            };

            SaveCommand = new ActionCommand(async _ => await SaveDeviceAsync(), CanSave);
            ClearCommand = new ActionCommand(Clear);
            AuthenticateCommand = new ActionCommand(
                async param =>
                {
                    if (param is Device device)
                    {
                        await RunWithLoader("Authenticate", async () =>
                        {
                            await AuthenticateDeviceAsync(device);
                        });
                    }
                },
                param => param is Device && !IsDeviceDetailsBusy // optionally disable if busy
            );

            DeleteCommand = new ActionCommand(async param => await DeleteDeviceAsync(param as Device),
                                      param => param is Device);
            GetStaticDataCommand = new ActionCommand(
            async _ => await RunWithLoader("StaticData", GetStaticDataAsync),
            _ => CanExecuteCommand()
            );

            GetDynamicDataCommand = new ActionCommand(
                async _ => await RunWithLoader("DynamicData", GetDynamicDataAsync),
                _ => CanExecuteCommand()
            );

            ManageResourcesCommand = new ActionCommand(
                _ => OpenResourceWindow(),
                _ => CanExecuteCommand()
            );

            _ = LoadDevicesAsync(true);
            EditingDevice = CreateDefaultDevice();
        }

        public bool IsFetchingStatic => LoadingStates.ContainsKey("StaticData") && LoadingStates["StaticData"].IsLoading;
        public bool IsFetchingDynamic => LoadingStates.ContainsKey("DynamicData") && LoadingStates["DynamicData"].IsLoading;

        /// <summary>
        /// Global busy flag derived from LoadingStates
        /// </summary>
        public bool IsDeviceDetailsBusy => LoadingStates.Values.Any(x => x.IsLoading);
        public bool IsDeviceDetailsOnlyBusy => IsFetchingDynamic || IsFetchingStatic;

        private bool CanExecuteCommand()
        {
            return List.SelectedDevice?.IsAuthenticated == true && !IsDeviceDetailsBusy;
        }

        private void UpdateCommandStates()
        {
            GetStaticDataCommand.RaiseCanExecuteChanged();
            GetDynamicDataCommand.RaiseCanExecuteChanged();
            ManageResourcesCommand.RaiseCanExecuteChanged();
        }

        protected async Task RunWithLoader(string key, Func<Task> operation)
        {
            if (!LoadingStates.ContainsKey(key))
                LoadingStates[key] = new LoadingState();

            var state = LoadingStates[key];

            state.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoadingState.IsLoading))
                {
                    OnPropertyChanged(nameof(IsFetchingStatic));
                    OnPropertyChanged(nameof(IsFetchingDynamic));
                    OnPropertyChanged(nameof(IsDeviceDetailsBusy));
                    OnPropertyChanged(nameof(IsDeviceDetailsOnlyBusy)); 
                    UpdateCommandStates();
                }
            };

            try
            {
                state.IsLoading = true;
                await Task.Yield(); // allow UI to refresh
                await operation();
            }
            finally
            {
                state.IsLoading = false;
            }
        }

        private async Task GetDynamicDataAsync()
        {
            if (List.SelectedDevice == null)
            {
                DeviceJson = "No device selected.";
                return;
            }

            StopDynamicUpdates();

            await RunWithLoader("DynamicData", async () =>
            {
                await Task.Delay(1000);
                var initialData = await _dataProvider.GetDynamicDataAsync(List.SelectedDevice);

                if (string.IsNullOrWhiteSpace(initialData))
                {
                    DeviceJson = "Dynamic data is empty.";
                }
                else
                {
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(initialData);
                    DeviceJson = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                }
            });

            StartDynamicUpdates(LiveUpdateHandler);
        }

        private void LiveUpdateHandler(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                DeviceJson = "Dynamic data is empty.";
                return;
            }

            // Fire-and-forget async task to avoid blocking Action<string>
            _ = Task.Run(async () =>
            {
                try
                {
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                    var formatted = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);

                    // Update UI on main thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        DeviceJson = formatted;
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        DeviceJson = $"Error parsing dynamic JSON: {ex.Message}";
                    });
                }
            });
        }

        private void OpenResourceWindow()
        {
            var popup = new ResourceInputWindow
            {
                Owner = Window.GetWindow(Application.Current.MainWindow),
                StaticData = _staticResourceInput,
                DynamicData = _dynamicResourceInput
            };

            if (popup.ShowDialog() == true)
            {
                _staticResourceInput = popup.StaticData;
                _dynamicResourceInput = popup.DynamicData;

                MessageBox.Show("Configurations saved");
            }
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
                    if (!AvailablePorts.Contains(EditingDevice.Port))
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
            bool duplicateIpPort = List.Devices.Any(d =>
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
            var existing = List.Devices.FirstOrDefault(d => d.DeviceId == EditingDevice.DeviceId);
            if (existing != null)
            {
                var index = List.Devices.IndexOf(existing);
                List.Devices[index] = new Device(EditingDevice);
                await _repo.SaveDevicesAsync(List.Devices);
                MessageBox.Show("Device updated successfully!");
            }
            else
            {
                EditingDevice.IsAuthenticated = false;
                List.Devices.Insert(0, new Device(EditingDevice));
                await _repo.SaveDevicesAsync(List.Devices);
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
            List.SelectedDevice = null;
            EditingDevice = CreateDefaultDevice();
            EditingDevice.Agent = AvailableAgents.First();
            EditingDevice.Port = AvailablePorts.FirstOrDefault();
        }

        private async Task AuthenticateDeviceAsync(Device device)
        {
            if (device == null) return;

            device.IsAuthenticated = null;
            await Task.Delay(500);
            bool result = new Random().Next(0, 2) == 1;
            device.IsAuthenticated = result;
            EditingDevice.IsAuthenticated = result;

            if (!result)
            {
                DeviceJson = string.Empty;
                StopDynamicUpdates();
            }
                
            await _repo.SaveDevicesAsync(List.Devices);
            OnPropertyChanged(nameof(List.Devices));
            UpdateCommandStates();

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
                List.Devices.Remove(device);
                await _repo.SaveDevicesAsync(List.Devices);

                if (List.SelectedDevice == device)
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
            try
            {
                // Clear old list
                List.Devices.Clear();

                // Load from repository
                var devicesFromFile = await _repo.LoadDevicesAsync();

                // Populate the observable collection
                foreach (var device in devicesFromFile)
                {
                    if (initial)
                        device.IsAuthenticated = null;
                    List.Devices.Add(device);
                }

                // Additional actions only on first load
                if (initial)
                {
                    await RunWithLoader("AuthenticateAllDevices", async () =>
                    {
                        await AuthenticateAllDevices();
                    });

                    await _repo.SaveDevicesAsync(List.Devices);
                    OnPropertyChanged(nameof(List.Devices));
                }
            }
            catch (Exception ex)
            {
                foreach (var device in List.Devices)
                {
                    if (initial)
                        device.IsAuthenticated = false;
                }
                Console.WriteLine($"Error in LoadDevicesAsync: {ex.Message}");
            }
            finally
            {
                AuthenticateCommand.RaiseCanExecuteChanged();
                UpdateCommandStates();
            }
        }


        private async Task AuthenticateAllDevices()
        {
            if (List.Devices is not null && List.Devices.Count > 0)
            {
                await Task.Delay(2000);
                foreach (var device in List.Devices)
                {
                    bool result = new Random().Next(0, 2) == 1;
                    device.IsAuthenticated = result;
                }
                UpdateCommandStates();
            }
        }

        // ================= Dynamic Data =================
        public async Task GetStaticDataAsync()
        {
            if (List.SelectedDevice == null)
            {
                DeviceJson = "No device selected.";
                return;
            }
            StopDynamicUpdates();
            if (List.SelectedDevice != null)
            {
                DeviceJson = null; //loading spinner
                await Task.Delay(2000);
                DeviceJson = await _dataProvider.GetStaticAsync(List.SelectedDevice);
            }

        }

        public void StartDynamicUpdates(Action<string> onDataReceived)
        {
            if (List.SelectedDevice == null) return;
            _dataProvider.StartDynamicUpdates(List.SelectedDevice, onDataReceived);
        }

        public void StopDynamicUpdates()
        {
            if (List.SelectedDevice == null) return;
            _dataProvider.StopDynamicUpdates(List.SelectedDevice);
            DeviceJson = string.Empty;
        }

        // Dictionary to hold multiple loading states
        public Dictionary<string, LoadingState> LoadingStates { get; } = new();

    }
}
