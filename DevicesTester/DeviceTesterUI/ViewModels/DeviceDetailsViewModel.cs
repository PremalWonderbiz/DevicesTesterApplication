using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Models;

namespace DeviceTesterUI.ViewModels
{
    // Editing + validation
    public class DeviceDetailsViewModel : BaseViewModel
    {
        private string _deviceJson;
        public string DeviceJson
        {
            get => _deviceJson;
            set { _deviceJson = value; OnPropertyChanged(nameof(DeviceJson)); }
        }
    }
}
