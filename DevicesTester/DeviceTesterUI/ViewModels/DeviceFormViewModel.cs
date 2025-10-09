using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Models;

namespace DeviceTesterUI.ViewModels
{
    public class DeviceFormViewModel : BaseViewModel
    {
        private Device _editingDevice;
        public Device EditingDevice
        {
            get => _editingDevice;
            set
            {
                _editingDevice = value;
                OnPropertyChanged(nameof(EditingDevice));
            }
        }

        public ObservableCollection<string> AvailableAgents { get; } =
        [
            "Redfish", "EcoRT", "SoftdPACManager"
        ];

        public ObservableCollection<string> AvailablePorts { get; } = new();

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }
    }
}
