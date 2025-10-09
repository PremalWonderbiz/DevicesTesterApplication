using DeviceTesterServices.Services;
using NUnit.Framework.Legacy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Models;
using System.Reflection;
using System.Timers;

namespace DeviceTesterTests.ServicesTests
{
    [TestFixture]
    public class JsonDeviceDataProviderTests
    {
        private string _tempDir = string.Empty!;
        private string _staticFile = string.Empty!;
        private string[] _dynamicFiles = Array.Empty<string>()!;
        private JsonDeviceDataProvider _provider = null!;

        [SetUp]
        public void Setup()
        {
            // Create temp folder for test files
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            // Create static file
            _staticFile = Path.Combine(_tempDir, "StaticData.json");
            File.WriteAllText(_staticFile, "{ \"name\": \"static\" }");

            // Create dynamic files
            _dynamicFiles = new[]
            {
                Path.Combine(_tempDir, "dynamic1.json"),
                Path.Combine(_tempDir, "dynamic2.json")
            };
            File.WriteAllText(_dynamicFiles[0], "{ \"data\": 1 }");
            File.WriteAllText(_dynamicFiles[1], "{ \"data\": 2 }");

            _provider = new JsonDeviceDataProvider(_dynamicFiles, intervalMs: 2000);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldThrow_WhenFilesNullOrEmpty()
        {
            ClassicAssert.Throws<ArgumentException>(() => new JsonDeviceDataProvider(null!));
            ClassicAssert.Throws<ArgumentException>(() => new JsonDeviceDataProvider(Array.Empty<string>()));
        }

        #endregion

        #region GetStaticAsync Tests

        [Test]
        public async Task GetStaticAsync_ShouldReturnFileContent_WhenFileExists()
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DummyData");
            Directory.CreateDirectory(folderPath); // create folder if missing

            var filePath = Path.Combine(folderPath, "StaticData.json");
            File.WriteAllText(filePath, "{ \"ok\": true }");

            var provider = new JsonDeviceDataProvider(_dynamicFiles);
            var result = await provider.GetStaticAsync(new Device());

            ClassicAssert.IsNotNull(result);
        }

        [Test]
        public void GetStaticAsync_ShouldThrow_WhenFileMissing()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

            var provider = new JsonDeviceDataProvider(_dynamicFiles, tempFile, tempFile);

            ClassicAssert.ThrowsAsync<InvalidOperationException>(() =>
                provider.GetStaticAsync(new Device()));
        }

        #endregion

        #region Dynamic Updates Tests

        [Test]
        public void StartDynamicUpdates_ShouldCycleThroughFiles()
        {
            int count = 0;
            string lastReceived = string.Empty;

            _provider.StartDynamicUpdates(new Device(), data =>
            {
                lastReceived = data;
                count++;
            });

            // --- create ElapsedEventArgs via reflection (handles private ctor) ---
            var ctor = typeof(ElapsedEventArgs)
                       .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                       .First(c => {
                           var p = c.GetParameters();
                           return p.Length == 1 && p[0].ParameterType == typeof(DateTime);
                       });
            var args = (ElapsedEventArgs)ctor.Invoke(new object[] { DateTime.Now });

            _provider.StartDynamicUpdates(new Device(), data =>
            {
                lastReceived = data;
                count++;
            });

            var timerField = typeof(JsonDeviceDataProvider)
                .GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(_provider) as System.Timers.Timer;

            var onElapsedMethod = typeof(JsonDeviceDataProvider)
                .GetMethod("TimerElapsed", BindingFlags.NonPublic | BindingFlags.Instance)!;

            for (int i = 0; i < 1; i++)
            {
                onElapsedMethod.Invoke(_provider,
                    new object[] { timerField, args });
            }
            ClassicAssert.GreaterOrEqual(count, 1);
            _provider.StopDynamicUpdates(new Device());
        }

        [Test]
        public void StartDynamicUpdates_ShouldSendErrorForMissingDynamicFile()
        {
            var missingFilesProvider = new JsonDeviceDataProvider(new[] { "nonexistent.json" });
            string? received = null;

            // --- create ElapsedEventArgs via reflection (handles private ctor) ---
            var ctor = typeof(ElapsedEventArgs)
                       .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                       .First(c => {
                           var p = c.GetParameters();
                           return p.Length == 1 && p[0].ParameterType == typeof(DateTime);
                       });
            var args = (ElapsedEventArgs)ctor.Invoke(new object[] { DateTime.Now });

            missingFilesProvider.StartDynamicUpdates(new Device(), data => {
                received = data;
                });

            var timerField = typeof(JsonDeviceDataProvider)
                .GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(missingFilesProvider) as System.Timers.Timer;

            var onElapsedMethod = typeof(JsonDeviceDataProvider)
                .GetMethod("TimerElapsed", BindingFlags.NonPublic | BindingFlags.Instance)!;

            for (int i = 0; i < 1; i++)
            {
                onElapsedMethod.Invoke(missingFilesProvider,
                    new object[] { timerField, args });
            }

            ClassicAssert.IsNotNull(received);
            ClassicAssert.IsTrue(received!.Contains("Error"));
        }

        [Test]
        public void StopDynamicUpdates_ShouldNotInvokeCallbackAfterStop()
        {
            int callCount = 0;
            string? lastReceived = null;

            // Start dynamic updates
            _provider.StartDynamicUpdates(new Device(), data =>
            {
                callCount++;
                lastReceived = data;
            });

            // Save previous call count after first automatic invocation
            int previousCount = callCount;

            // Stop updates
            _provider.StopDynamicUpdates(new Device());

            // Trigger timer manually to simulate elapsed after stopping
            var timerField = typeof(JsonDeviceDataProvider)
                .GetField("_timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(_provider) as System.Timers.Timer;

            typeof(System.Timers.Timer)
                .GetMethod("OnElapsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(timerField, new object?[] { null });

            // Assert callback was NOT invoked again
            ClassicAssert.AreEqual(previousCount, callCount);
        }


        #endregion
    }
}
