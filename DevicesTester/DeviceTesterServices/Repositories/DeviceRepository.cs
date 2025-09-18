using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DeviceTesterCore.Models;

namespace DeviceTesterServices.Repositories
{
    public class DeviceRepository
    {
        private readonly string _filePath;
        
        public DeviceRepository()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _filePath = Path.Combine(exeDir, "devices.json");
        }

        public List<Device> LoadDevices()
        {
            if (!File.Exists(_filePath)) return new List<Device>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Device>>(json) ?? new List<Device>();
        }

        public void SaveDevices(IEnumerable<Device> devices)
        {
            var json = JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }

}
