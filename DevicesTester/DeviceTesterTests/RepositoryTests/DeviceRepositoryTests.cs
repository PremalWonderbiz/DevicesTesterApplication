using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTesterTests.RepositoryTests
{
    using NUnit.Framework;
    using DeviceTesterServices.Repositories;
    using DeviceTesterCore.Models;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework.Legacy;

    namespace DeviceTesterTests.RepositoryTests
    {
        [TestFixture]
        public class DeviceRepositoryTests
        {
            private string _testFilePath;
            private DeviceRepository _repo;

            [SetUp]
            public void Setup()
            {
                _testFilePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test_devices.json");
                if (File.Exists(_testFilePath))
                    File.Delete(_testFilePath);

                _repo = new DeviceRepository(_testFilePath);
            }

            [Test]
            public async Task LoadDevicesAsync_ShouldReturnEmptyList_WhenFileNotExists()
            {
                var devices = await _repo.LoadDevicesAsync();
                ClassicAssert.IsEmpty(devices);
            }

            [Test]
            public async Task SaveDevicesAsync_And_LoadDevicesAsync_ShouldWorkCorrectly()
            {
                var devices = new List<Device>
            {
                new Device { DeviceId = "1", Agent = "Redfish", IpAddress = "127.0.0.1" }
            };

                await _repo.SaveDevicesAsync(devices);
                var loaded = await _repo.LoadDevicesAsync();

                ClassicAssert.AreEqual(1, loaded.Count);
                ClassicAssert.AreEqual("Redfish", loaded[0].Agent);
            }
        }
    }

}
