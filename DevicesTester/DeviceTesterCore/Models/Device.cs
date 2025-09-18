using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTesterCore.Models
{
    public class Device : INotifyPropertyChanged
    {
        private string _agent;
        private string _deviceId;
        private string _solutionId;
        private string _ipAddress;
        private int _port;
        private string _username;
        private string _password;
        private bool _isAuthenticated;
        private bool _useSecureConnection;

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

        public int Port
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
    }
}
