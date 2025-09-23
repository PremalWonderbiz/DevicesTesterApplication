using System;
using System.ComponentModel;
using System.Net;

namespace DeviceTesterCore.Models
{
    public class Device : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _agent;
        private string _deviceId;
        private string _solutionId;
        private string _ipAddress;
        private string _port;
        private string _username;
        private string _password;
        private bool _isAuthenticated;
        private bool _useSecureConnection;

        public Device() { }  // default constructor (needed for XAML binding etc.)

        // Copy constructor
        public Device(Device other)
        {
            if (other == null) return;

            Agent = other.Agent;
            DeviceId = other.DeviceId;
            SolutionId = other.SolutionId;
            IpAddress = other.IpAddress;
            Port = other.Port;
            Username = other.Username;
            Password = other.Password;
            IsAuthenticated = other.IsAuthenticated;
            UseSecureConnection = other.UseSecureConnection;
        }

        public string Agent
        {
            get => _agent;
            set { _agent = value; OnPropertyChanged(nameof(Agent)); }
        }

        public string DeviceId
        {
            get => _deviceId;
            set { _deviceId = value; OnPropertyChanged(nameof(DeviceId)); }
        }

        public string SolutionId
        {
            get => _solutionId;
            set { _solutionId = value; OnPropertyChanged(nameof(SolutionId)); }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(nameof(IpAddress)); }
        }

        public string Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(nameof(Port)); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set { _isAuthenticated = value; OnPropertyChanged(nameof(IsAuthenticated)); }
        }

        public bool UseSecureConnection
        {
            get => _useSecureConnection;
            set { _useSecureConnection = value; OnPropertyChanged(nameof(UseSecureConnection)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        //Validation (IDataErrorInfo)
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Agent):
                        if (string.IsNullOrWhiteSpace(Agent))
                            return "Agent is required";
                        break;

                    case nameof(IpAddress):
                        if (string.IsNullOrWhiteSpace(IpAddress) || !IPAddress.TryParse(IpAddress, out _))
                            return "Invalid IP address";
                        break;

                    case nameof(Port):
                        if (!int.TryParse(Port, out int port) || port <= 0 || port > 65535)
                            return "Port must be between 1 and 65535";
                        break;

                    case nameof(Username):
                        if (string.IsNullOrWhiteSpace(Username))
                            return "Username is required";
                        break;

                    case nameof(Password):
                        if (string.IsNullOrWhiteSpace(Password))
                            return "Password is required";
                        break;

                    case nameof(DeviceId):
                        if (!string.IsNullOrEmpty(DeviceId) && !Guid.TryParse(DeviceId, out _))
                            return "DeviceId must be a valid GUID";
                        break;

                    case nameof(SolutionId):
                        if (!string.IsNullOrEmpty(SolutionId) && !Guid.TryParse(SolutionId, out _))
                            return "SolutionId must be a valid GUID";
                        break;
                }
                return null;
            }
        }
        public bool HasErrors => GetType().GetProperties()
       .Any(p => !string.IsNullOrEmpty(this[p.Name]));
    }
}
