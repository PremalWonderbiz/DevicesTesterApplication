using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DeviceTesterCore.Interfaces;
using DeviceTesterCore.Models;
using DeviceTesterUI.Commands;
using DeviceTesterUI.Helpers;
using DeviceTesterUI.ViewModels;
using DeviceTesterUI.Windows;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using SchneiderElectric.Automation.Sodb.Client;
using SchneiderElectric.Automation.Sodb.Common;
using SchneiderElectric.Automation.Sodb.Messages;


namespace DeviceTesterUI.ViewModels
{
    public class DeviceViewModel : BaseViewModel
    {
        
        #region DeviceListViewModel refactoring
        // new sub-VM instance (field initializer ensures availability before ctor runs)
        private readonly DeviceListViewModel _list = new ();
        
        public DeviceListViewModel List => _list;

        #endregion

        #region DeviceFormViewModel refactoring
        private readonly DeviceFormViewModel _form = new ();

        public DeviceFormViewModel Form => _form;

        // this is to store the old subscribed editing device
        private DeviceTesterCore.Models.Device _previousDevice; 
        
        #endregion

        #region DeviceDetailsViewModel refactoring
        //private string _deviceJson;
        //public string DeviceJson
        //{
        //    get => _deviceJson;
        //    set { _deviceJson = value; OnPropertyChanged(nameof(DeviceJson)); }
        //}

        private readonly DeviceDetailsViewModel _details = new ();
        public DeviceDetailsViewModel Details => _details;

        private string _staticResourceInput;

        private string _dynamicResourceInput;

        #endregion

        private readonly IDeviceRepository _repo;
        private readonly IDeviceDataProvider _dataProvider;

        //sodbClient for future implementation
        private readonly ISodbClient? client;

        private void EditingDevice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Device.Agent))
            {
                LoadPorts(Form.EditingDevice.Agent, string.IsNullOrEmpty(Form.EditingDevice.DeviceId));
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
                Form.EditingDevice = new Device(List.SelectedDevice); // copy constructor
            else
                Form.EditingDevice = CreateDefaultDevice();

            Details.DeviceJson = string.Empty;
            Form.ErrorMessage = string.Empty;
        }  

        // ================= Commands =================
        public ActionCommand SaveCommand { get; }
        public ActionCommand ClearCommand { get; }
        public ActionCommand AuthenticateCommand { get; }
        public ActionCommand DeleteCommand { get; }
        public ActionCommand GetStaticDataCommand { get; }
        public ActionCommand GetDynamicDataCommand { get; }
        public ActionCommand ManageResourcesCommand { get; }

        public DeviceViewModel(IDeviceRepository repo, IDeviceDataProvider dataProvider, ISodbClient? _client = null)
        {
            //sodb client future implementation
            if (_client is not null)
            {
                client = _client;
            }
            else
            {
                try
                {
                    client = SodbClientSingleton.Instance;

                    if (SodbClientSingleton.InitializationError != null)
                    {
                        MessageBox.Show($"Error initializing SodbClient: {SodbClientSingleton.InitializationError}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error initializing SodbClient: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
                

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

            Form.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DeviceFormViewModel.EditingDevice))
                {
                    // unsubscribe from old device
                    if (_previousDevice != null)
                    {
                        _previousDevice.ErrorsChanged -= EditingDevice_ErrorsChanged;
                        _previousDevice.PropertyChanged -= EditingDevice_PropertyChanged;
                    }

                    // subscribe to new device
                    _previousDevice = Form.EditingDevice;
                    if (_previousDevice != null)
                    {
                        _previousDevice.ErrorsChanged += EditingDevice_ErrorsChanged;
                        _previousDevice.PropertyChanged += EditingDevice_PropertyChanged;

                        LoadPorts(_previousDevice.Agent, string.IsNullOrEmpty(_previousDevice.DeviceId));
                    }

                    SaveCommand?.RaiseCanExecuteChanged();
                    ClearCommand?.RaiseCanExecuteChanged();
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
            Form.EditingDevice = CreateDefaultDevice();
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
                Details.DeviceJson = "No device selected.";
                return;
            }

            StopDynamicUpdates();

            await RunWithLoader("DynamicData", async () =>
            {
                await Task.Delay(1000);
                var initialData = await _dataProvider.GetDynamicDataAsync(List.SelectedDevice);

                if (string.IsNullOrWhiteSpace(initialData))
                {
                    Details.DeviceJson = "Dynamic data is empty.";
                }
                else
                {
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(initialData);
                    Details.DeviceJson = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                }
            });

            StartDynamicUpdates(LiveUpdateHandler);
        }

        private void LiveUpdateHandler(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Details.DeviceJson = "Dynamic data is empty.";
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
                        Details.DeviceJson = formatted;
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Details.DeviceJson = $"Error parsing dynamic JSON: {ex.Message}";
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
            Form.AvailablePorts.Clear();

            switch (agent)
            {
                case "Redfish":
                    Form.AvailablePorts.Add("9000");
                    Form.AvailablePorts.Add("Other");
                    break;
                case "EcoRT":
                    Form.AvailablePorts.Add("51443");
                    Form.AvailablePorts.Add("51499");
                    Form.AvailablePorts.Add("Other");
                    break;
                case "SoftdPACManager":
                    Form.AvailablePorts.Add("443");
                    Form.AvailablePorts.Add("Other");
                    break;
            }

            if (Form.EditingDevice != null)
            {
                if (!isNewDevice)
                {
                    if (!Form.AvailablePorts.Contains(Form.EditingDevice.Port))
                    {
                        Form.AvailablePorts.Add(Form.EditingDevice.Port);
                        SortAvailablePorts();
                    }
                }
                else
                {
                    Form.EditingDevice.Port = Form.AvailablePorts.FirstOrDefault() ?? "0000";
                }
            }
        }

        private void SortAvailablePorts()
        {
            var sorted = Form.AvailablePorts
                .OrderBy(p => p == "Other" ? int.MaxValue : int.Parse(p))
                .ToList();

            Form.AvailablePorts.Clear();
            foreach (var p in sorted)
                Form.AvailablePorts.Add(p);
        }

        private async Task SaveDeviceAsync()
        {
            if (Form.EditingDevice == null) return;

            // Duplicate IP + Port check
            bool duplicateIpPort = List.Devices.Any(d =>
                d.IpAddress == Form.EditingDevice.IpAddress &&
                d.Port == Form.EditingDevice.Port &&
                d.DeviceId != Form.EditingDevice.DeviceId);

            if (duplicateIpPort)
            {
                Form.ErrorMessage = "A device with the same IP and Port already exists!";
                return;
            }

            Form.ErrorMessage = string.Empty;

            // Generate IDs if empty
            if (string.IsNullOrEmpty(Form.EditingDevice.DeviceId))
                Form.EditingDevice.DeviceId = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(Form.EditingDevice.SolutionId))
                Form.EditingDevice.SolutionId = Guid.NewGuid().ToString();

            //set device name hard coded for now
            Form.EditingDevice.DeviceName = $"Device {Form.EditingDevice.Agent}";

            // Add or update using copy constructor
            var existing = List.Devices.FirstOrDefault(d => d.DeviceId == Form.EditingDevice.DeviceId);
            if (existing != null)
            {
                var index = List.Devices.IndexOf(existing);
                List.Devices[index] = new Device(Form.EditingDevice);
                await _repo.SaveDevicesAsync(List.Devices);
                MessageBox.Show("Device updated successfully!");
            }
            else
            {
                Form.EditingDevice.IsAuthenticated = false;
                List.Devices.Insert(0, new Device(Form.EditingDevice));
                await _repo.SaveDevicesAsync(List.Devices);
                MessageBox.Show("Device saved successfully!");
            }


            #region Future Implementation
            //if (Form.EditingDevice.Agent == "EcoRT")
            //{
            //    var res = await EcoRt_Device_Add(Form.EditingDevice);
            //}
            //if (Form.EditingDevice.Agent == "Redfish")
            //{
            //    var res = await Redfish_Device_Add(Form.EditingDevice);
            //}
            #endregion

            Clear(new object());
        }

        //#region EcoRtAddFunctionality
        //public async Task<bool> EcoRt_Device_Add(Device editingDevice)
        //{
        //    var deviceId = editingDevice.DeviceId;

        //    var deviceDesc = new DeviceDescription { Id = deviceId, Name = "EcoRT_Device" };

        //    var ecoRTConnectionDetails = new EcoRTConnectionDetails
        //    {
        //        AuthenticationPluginGuid = "D3BCDFDE-0476-4A15-B5F9-41E57E3D3DAA",
        //        Endpoint = new Endpoint { Address = editingDevice.IpAddress, Port = int.Parse(editingDevice.Port) },
        //        DeviceDesc = deviceDesc,
        //        UseEncryption = editingDevice.UseSecureConnection,
        //    };
            
        //    var deviceIdentifier = new DeviceIdentifier { DeviceId = deviceDesc.Id, OwnerId = "solutionId1" };

        //    var connectionInfo = new ConnectionInfo
        //    {
        //        Protocol = EcoRTConstants.EcoRt.OriginalName(),
        //        Credential = new Credential { Username = editingDevice.Username, Password = editingDevice.Password },
        //        Details = Any.Pack(ecoRTConnectionDetails),
        //    };

        //    var authResult = await AddOrReplaceConnectionInfo(deviceIdentifier, connectionInfo).ConfigureAwait(false);

        //    return authResult;
        //}

        //#endregion

        //#region RedfishAddFunctionality
        //public async Task<bool> Redfish_Device_Add(Device editingDevice)
        //{
        //    var redfishConnectionDetails = new RedfishConnectionDetails
        //    {
        //        Endpoint = new Endpoint { Address = editingDevice.IpAddress, Port = int.Parse(editingDevice.Port) }
        //    };

        //    var deviceIdentifier = new DeviceIdentifier { DeviceId = editingDevice.DeviceId, OwnerId = "solutionId1" };

        //    var connectionInfo = new ConnectionInfo
        //    {
        //        Protocol = "Redfish",
        //        Credential = new Credential { Username = editingDevice.Username, Password = editingDevice.Password },
        //        Details = Any.Pack(redfishConnectionDetails),
        //    };

        //    var authResult = await AddOrReplaceConnectionInfo(deviceIdentifier, connectionInfo).ConfigureAwait(false);
        //    //this.Dispatcher.Invoke(() =>
        //    //{
        //    //    this.txtLog.Text = authResult ? "ConnectionInfo added to store successfully" : "ConnectionInfo not added to store";
        //    //});

        //    return authResult;
        //}

        //#endregion

        //#region HelperMethodForConnectionAddOrReplace
        //public virtual async Task<bool> AddOrReplaceConnectionInfo(DeviceIdentifier id, ConnectionInfo connectionInfo)
        //{
        //    var connectionInfoAddRequest = new ConnectionInfoAddRequest
        //    {
        //        Id = id,
        //        Info = connectionInfo,
        //    };

        //    var (result, _) = await client.ExecuteAsync<ConnectionInfoAddRequest, Empty>(SodbConnectionInfoFunction.V1AddOrReplace, connectionInfoAddRequest).ConfigureAwait(false);

        //    return result;
        //}

        //#endregion

        //#region AuthenticateFunctionality
        //public async Task<bool> AuthenticateDeviceInSODB(Device device)
        //{
        //    var deviceId = device.DeviceId;

        //    var deviceIdentifier = new DeviceIdentifier { DeviceId = deviceId, OwnerId = "solutionId1" };

        //    var (IsSuccess, Data) = await client.ExecuteAsync<DeviceIdentifier, BoolValue>(SodbConnectionInfoFunction.V1ValidateExisting<EcoRTConstants>(EcoRTConstants.EcoRt), deviceIdentifier).ConfigureAwait(false);

        //    if (Data == null || !IsSuccess)
        //    {
        //        return false;
        //    }

        //    //Dispatcher.Invoke(() =>
        //    //{
        //    //    var mosnitorWindow = new MonitorWindow($"Authentication State: {deviceId}");
        //    //    monitorWindow.UpdateMonitorInfo(res.Data.Value ? "Valid credentials" : "Invalid credentials");
        //    //    monitorWindow.Show();
        //    //});

        //    return IsSuccess;
        //}

        //#endregion

        private bool CanSave(object obj)
        {
            if (Form.EditingDevice == null) return false;
            return Validator.TryValidateObject(Form.EditingDevice, new ValidationContext(Form.EditingDevice), null, true);
        }

        private void Clear(object obj)
        {
            Form.ErrorMessage = string.Empty;
            List.SelectedDevice = null;
            Form.EditingDevice = CreateDefaultDevice();
            Form.EditingDevice.Agent = Form.AvailableAgents.First();
            Form.EditingDevice.Port = Form.AvailablePorts.FirstOrDefault();
        }

        private async Task AuthenticateDeviceAsync(Device device)
        {
            if (device == null) return;

            device.IsAuthenticated = null;
            await Task.Delay(500);
            bool result = new Random().Next(0, 2) == 1;
            device.IsAuthenticated = result;
            Form.EditingDevice.IsAuthenticated = result;

            if (!result)
            {
                Details.DeviceJson = string.Empty;
                StopDynamicUpdates();
            }
                
            await _repo.SaveDevicesAsync(List.Devices);
            OnPropertyChanged(nameof(List.Devices));
            UpdateCommandStates();

            MessageBox.Show(result ? "Authentication succeeded" : "Authentication failed");

            #region Future Implementation
            //AuthenticateDeviceInSODB(device);
            #endregion
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
                    Form.EditingDevice = CreateDefaultDevice();

                MessageBox.Show("Device deleted successfully");
            }
        }

        public static Device CreateDefaultDevice()
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
                Details.DeviceJson = "No device selected.";
                return;
            }
            StopDynamicUpdates();
            if (List.SelectedDevice != null)
            {
                Details.DeviceJson = null; //loading spinner
                await Task.Delay(2000);
                Details.DeviceJson = await _dataProvider.GetStaticAsync(List.SelectedDevice);
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
            Details.DeviceJson = string.Empty;
        }

        // Dictionary to hold multiple loading states
        public Dictionary<string, LoadingState> LoadingStates { get; } = new();

    }

    public class SodbClientSingleton
    {
        private static SodbClient instance;
        private static readonly object lockObj = new ();
        private static bool isInitialized = false;
        private static string initializationError = null;

        private SodbClientSingleton() { }

        public static SodbClient Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (instance == null && !isInitialized)
                    {
                        try
                        {
                            var logger = new ConsoleLogger();
                            int mqqtPort = 22590;
                            instance = new SodbClient(logger, new MqttClientCreator(IPAddress.Loopback.ToString(), mqqtPort, logger, false));
                            instance.InitializeAsync().Wait();
                            isInitialized = true;
                        }
                        catch (Exception ex)
                        {
                            initializationError = ex.Message;
                            throw;
                        }
                    }
                    return instance;
                }
            }
        }

        public static string InitializationError => initializationError;
    }
}


