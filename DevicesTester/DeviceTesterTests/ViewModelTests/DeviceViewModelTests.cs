using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterCore.Interfaces;
using Moq;
using NUnit.Framework.Legacy;
using NUnit.Framework;
using DeviceTesterCore.Models;
using System.Collections.ObjectModel;

namespace DeviceTesterTests.ViewModelTests
{
    [TestFixture]
    public class DeviceViewModelTests
    {
        private Mock<IDeviceRepository> _mockRepo;
        private DeviceViewModel _vm;
        private List<Device> _devices;

        [SetUp]
        public void Setup()
        {
            _devices = new List<Device>
            {
                new Device { DeviceId = "D1", Agent = "Redfish", Port = "9000" },
                new Device { DeviceId = "D2", Agent = "EcoRT", Port = "51443" }
            };

            _mockRepo = new Mock<IDeviceRepository>();
            _mockRepo.Setup(r => r.LoadDevicesAsync()).ReturnsAsync(_devices);
            _mockRepo.Setup(r => r.SaveDevicesAsync(It.IsAny<ObservableCollection<Device>>()))
                     .Returns(Task.CompletedTask);

            _vm = new DeviceViewModel(_mockRepo.Object);
        }

        [Test]
        public async Task LoadDevicesAsync_ShouldPopulateDevices()
        {
            await _vm.LoadDevicesAsync();

            ClassicAssert.AreEqual(2, _vm.Devices.Count);
            ClassicAssert.AreEqual("D1", _vm.Devices[0].DeviceId);
        }

        [Test]
        public void SelectedDevice_Set_ShouldUpdateEditingDeviceAndPorts()
        {
            _vm.SelectedDevice = _devices[0];

            ClassicAssert.AreEqual(_devices[0].DeviceId, _vm.EditingDevice.DeviceId);
            ClassicAssert.AreEqual("Redfish", _vm.SelectedAgent);
            ClassicAssert.AreEqual("9000", _vm.SelectedPort);
        }

        [Test]
        public void SelectedDevice_Null_ShouldCreateDefaultDevice()
        {
            _vm.SelectedDevice = null;

            ClassicAssert.IsNotNull(_vm.EditingDevice);
            ClassicAssert.AreEqual("Redfish", _vm.SelectedAgent);
            ClassicAssert.AreEqual("9000", _vm.SelectedPort);
        }

        [Test]
        public void SelectedAgent_Change_ShouldUpdateEditingDeviceAndPorts()
        {
            _vm.SelectedDevice = _devices[0];
            _vm.SelectedAgent = "EcoRT";

            ClassicAssert.AreEqual("EcoRT", _vm.EditingDevice.Agent);
            ClassicAssert.Contains("51443", _vm.AvailablePorts);
            ClassicAssert.Contains("51499", _vm.AvailablePorts);
        }

        [Test]
        public void SelectedPort_Change_ShouldUpdateEditingDevice()
        {
            _vm.SelectedDevice = _devices[0];
            _vm.SelectedPort = "Other";

            ClassicAssert.AreEqual("Other", _vm.EditingDevice.Port);
        }

        [Test]
        public async Task AddDeviceAsync_ShouldAddDeviceToTop()
        {
            var newDevice = new Device { DeviceId = "D3" };
            await _vm.AddDeviceAsync(newDevice);

            ClassicAssert.AreEqual(newDevice, _vm.Devices[0]);
            _mockRepo.Verify(r => r.SaveDevicesAsync(It.IsAny<ObservableCollection<Device>>()), Times.Once);
        }

        [Test]
        public async Task UpdateDeviceAsync_ShouldReplaceSelectedDevice()
        {
            _vm.SelectedDevice = _devices[0];
            var updatedDevice = new Device { DeviceId = "D1", Port = "9001" };
            await _vm.UpdateDeviceAsync(updatedDevice);

            ClassicAssert.AreEqual("9001", _vm.Devices[0].Port);
            _mockRepo.Verify(r => r.SaveDevicesAsync(It.IsAny<ObservableCollection<Device>>()), Times.Once);
        }

        [Test]
        public async Task DeleteDeviceAsync_ShouldRemoveDevice()
        {
            var deviceToDelete = _devices[0];
            await _vm.DeleteDeviceAsync(deviceToDelete);

            ClassicAssert.IsFalse(_vm.Devices.Contains(deviceToDelete));
            _mockRepo.Verify(r => r.SaveDevicesAsync(It.IsAny<ObservableCollection<Device>>()), Times.Once);
        }

        [Test]
        public async Task AuthenticateDeviceAsync_ShouldSetIsAuthenticated()
        {
            var device = new Device();
            bool result = await _vm.AuthenticateDeviceAsync(device);

            ClassicAssert.AreEqual(result, device.IsAuthenticated);
            _mockRepo.Verify(r => r.SaveDevicesAsync(It.IsAny<ObservableCollection<Device>>()), Times.Once);
        }

        [Test]
        public void CreateDefaultDevice_ShouldHaveExpectedValues()
        {
            var defaultDevice = _vm.CreateDefaultDevice();

            ClassicAssert.AreEqual("Redfish", defaultDevice.Agent);
            ClassicAssert.AreEqual("9000", defaultDevice.Port);
            ClassicAssert.AreEqual("127.0.0.1", defaultDevice.IpAddress);
            ClassicAssert.AreEqual(true, defaultDevice.UseSecureConnection);
        }
    }
}
