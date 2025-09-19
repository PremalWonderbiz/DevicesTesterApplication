using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Models;

namespace DeviceTesterCore.Interfaces
{
    public interface IDeviceRepository
    {
        Task<List<Device>> LoadDevicesAsync();
        Task SaveDevicesAsync(IEnumerable<Device> devices);
    }
}
