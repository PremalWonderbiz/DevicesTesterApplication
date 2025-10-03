using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Models;

namespace DeviceTesterCore.Interfaces
{
    public interface IDeviceDataProvider
    {
        Task<string> GetStaticAsync(Device device);
        Task<string> GetDynamicDataAsync(Device device);
        void StartDynamicUpdates(Device device, Action<string> onDataReceived);
        void StopDynamicUpdates(Device device);
    }
}
