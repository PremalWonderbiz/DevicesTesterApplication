using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using DeviceTesterCore.CustomAttributes;

namespace DeviceTesterCore.Models
{
    public class Device : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #pragma warning disable IDE0028
        private readonly Dictionary<string, List<string>> _errors = new() { };
        #pragma warning restore IDE0028

        private readonly bool _suppressValidation = true;

        public Device()
        {
            _suppressValidation = true;

            Agent = "Redfish";
            DeviceId = string.Empty;
            SolutionId = string.Empty;
            IpAddress = "127.0.0.0";
            Port = "9000";
            Username = "";
            Password = "";
            UseSecureConnection = true;
            IsAuthenticated = false;

            _suppressValidation = false;
        }

        public Device(Device other)
        {
            ArgumentNullException.ThrowIfNull(other);

            _suppressValidation = true;

            Agent = other.Agent;
            DeviceId = other.DeviceId;
            SolutionId = other.SolutionId;
            IpAddress = other.IpAddress;
            Port = other.Port;
            Username = other.Username;
            Password = other.Password;
            IsAuthenticated = other.IsAuthenticated;
            UseSecureConnection = other.UseSecureConnection;

            _suppressValidation = false;  
        }

        // ==== Properties with Validation Attributes ====
        private string _agent;
        [Required(ErrorMessage = "Agent is required")]
        public string Agent
        {
            get => _agent;
            set
            {
                if (_agent != value)
                {
                    _agent = value;
                    OnPropertyChanged(nameof(Agent));
                    ValidateProperty(nameof(Agent), value);
                }
            }
        }

        private string _deviceId;
        [RegularExpression(@"^$|^[0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12}$",
            ErrorMessage = "DeviceId must be a valid GUID")]
        public string DeviceId
        {
            get => _deviceId;
            set
            {
                if (_deviceId != value)
                {
                    _deviceId = value;
                    OnPropertyChanged(nameof(DeviceId));
                    ValidateProperty(nameof(DeviceId), value);
                }
            }
        }

        private string _solutionId;
        [RegularExpression(@"^$|^[0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12}$",
            ErrorMessage = "SolutionId must be a valid GUID")]
        public string SolutionId
        {
            get => _solutionId;
            set
            {
                if (_solutionId != value)
                {
                    _solutionId = value;
                    OnPropertyChanged(nameof(SolutionId));
                    ValidateProperty(nameof(SolutionId), value);
                }
            }
        }

        private string _deviceName;
        public string DeviceName
        {
            get => _deviceName;
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    OnPropertyChanged(nameof(DeviceName));
                    ValidateProperty(nameof(DeviceName), value);
                }
            }
        }

        private string _ipAddress;
        [Required(ErrorMessage = "IP Address is required")]
        [IPAddress(ErrorMessage = "Invalid IP address")]
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged(nameof(IpAddress));
                    ValidateProperty(nameof(IpAddress), value);
                }
            }
        }

        private string _port;
        [Required(ErrorMessage = "Port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public string Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged(nameof(Port));
                    ValidateProperty(nameof(Port), value);
                }
            }
        }

        private string _username;
        [Required(ErrorMessage = "Username is required")]
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged(nameof(Username));
                    ValidateProperty(nameof(Username), value);
                }
            }
        }

        private string _password;
        [Required(ErrorMessage = "Password is required")]
        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                    ValidateProperty(nameof(Password), value);
                }
            }
        }

        private bool? _isAuthenticated;
        public bool? IsAuthenticated
        {
            get => _isAuthenticated;
            set { _isAuthenticated = value; OnPropertyChanged(nameof(IsAuthenticated)); }
        }

        private bool _useSecureConnection;
        public bool UseSecureConnection
        {
            get => _useSecureConnection;
            set { _useSecureConnection = value; OnPropertyChanged(nameof(UseSecureConnection)); }
        }

        // ==== Validations ====
        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName) && _errors.TryGetValue(propertyName, out var list))
                return list;
            return Enumerable.Empty<string>();
        }

        private void ValidateProperty(string propertyName, object value)
        {
            if (_suppressValidation) return; 
            var results = new List<ValidationResult>();

            Validator.TryValidateProperty(
                value,
                new ValidationContext(this) { MemberName = propertyName },
                results);

            if (results.Any())
                _errors[propertyName] = results.Select(r => r.ErrorMessage!).ToList();
            else
                _errors.Remove(propertyName);

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }



        // ==== PropertyChanged plumbing ====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
