using System;
using System.IO;
using System.Timers;
using DeviceTesterCore.Interfaces;
using DeviceTesterCore.Models;

namespace DeviceTesterServices.Services
{
    public class JsonDeviceDataProvider : IDeviceDataProvider
    {
        private readonly string[] _dynamicFiles;
        private readonly string _staticFilePath;
        private readonly string _dynamicFilePath;
        private readonly System.Timers.Timer _timer;
        private int _fileIndex = 0;
        private Action<string>? _onDataReceived;

        public JsonDeviceDataProvider(string[] dynamicFiles,string? dynamicFilePath=null, string? staticFilePath = null, double intervalMs = 5000)
        {
            if (dynamicFiles == null || dynamicFiles.Length == 0)
                throw new ArgumentException("dynamicFiles cannot be null or empty.");

            _dynamicFiles = dynamicFiles;
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += TimerElapsed;
            _timer.AutoReset = true;
            _staticFilePath = staticFilePath ?? Path.Combine("DummyData", "StaticData.json");
            _dynamicFilePath = dynamicFilePath ?? Path.Combine("DummyData", "DynamicData1.json");
        }

        public async Task<string> GetDynamicDataAsync(Device device)
        {
            try
            {
                if (!File.Exists(_staticFilePath))
                    throw new FileNotFoundException("Dynamic data file not found.", _dynamicFilePath);

                return await File.ReadAllTextAsync(_dynamicFilePath);
            }
            catch (Exception ex)
            {
                // Optionally log error
                throw new InvalidOperationException("Failed to read dynamic data.", ex);
            }
        }
        
        public async Task<string> GetStaticAsync(Device device)
        {
            try
            {
                if (!File.Exists(_staticFilePath))
                    throw new FileNotFoundException("Static data file not found.", _staticFilePath);

                return await File.ReadAllTextAsync(_staticFilePath);
            }
            catch (Exception ex)
            {
                // Optionally log error
                throw new InvalidOperationException("Failed to read static data.", ex);
            }
        }

        public void StartDynamicUpdates(Device device, Action<string> onDataReceived)
        {
            ArgumentNullException.ThrowIfNull(onDataReceived);
            _onDataReceived = onDataReceived;
            _fileIndex = 0;

            try
            {
                _timer.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start dynamic updates.", ex);
            }
        }

        /// <summary>
        /// Reads the next dynamic file and invokes the callback.
        /// </summary>
        private void SendNextDynamicFile()
        {
            if (_onDataReceived == null || _dynamicFiles.Length == 0) return;

            try
            {
                var file = _dynamicFiles[_fileIndex % _dynamicFiles.Length];

                if (!File.Exists(file))
                {
                    _onDataReceived($"Error: Dynamic file not found: {file}");
                    return;
                }

                string content = File.ReadAllText(file);
                _onDataReceived(content);

                _fileIndex++;
            }
            catch (Exception ex)
            {
                _onDataReceived($"Error reading dynamic file: {ex.Message}");
            }
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            SendNextDynamicFile();
        }


        public void StopDynamicUpdates(Device device)
        {
            try
            {
                _timer.Stop();
            }
            catch
            {
                // optionally log
            }
            finally
            {
                _onDataReceived = null;
            }
        }
    }
}
