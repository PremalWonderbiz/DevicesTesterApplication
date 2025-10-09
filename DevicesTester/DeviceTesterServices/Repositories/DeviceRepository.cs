using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DeviceTesterCore.Interfaces;
using DeviceTesterCore.Models;

namespace DeviceTesterServices.Repositories
{
    /// <summary>
    /// Repository implementation for device persistence.
    /// Handles loading and saving devices to a JSON file.
    /// </summary>
    public class DeviceRepository : IDeviceRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions jsonSerializerOptions = new () { WriteIndented = true };

        /// <summary>
        /// Initializes the repository with a default file path inside the executable directory.
        /// </summary>
        public DeviceRepository(string? filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                _filePath = Path.Combine(exeDir, "DummyData", "devices.json");
            }
            else
            {
                _filePath = filePath;
            }
        }

        /// <summary>
        /// Loads all devices asynchronously from the JSON file.
        /// </summary>
        /// <returns>List of <see cref="Device"/> objects, or empty if no file/data exists.</returns>
        public async Task<List<Device>> LoadDevicesAsync()
        {
            if (!File.Exists(_filePath))
                return new List<Device>();

            var json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(json))
                return new List<Device>();

            return JsonSerializer.Deserialize<List<Device>>(json) ?? new List<Device>();
        }

        /// <summary>
        /// Saves all devices asynchronously into the JSON file.
        /// </summary>
        /// <param name="devices">Collection of devices to persist.</param>
        public async Task SaveDevicesAsync(IEnumerable<Device> devices)
        {
            var json = JsonSerializer.Serialize(devices, jsonSerializerOptions);
            await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
        }
    }
}
