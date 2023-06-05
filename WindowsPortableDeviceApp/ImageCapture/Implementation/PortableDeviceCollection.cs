using System.Collections.ObjectModel;
using PortableDeviceApiLib;

namespace NowCandid.WPD.Implementation
{
    public class PortableDeviceCollection : Collection<PortableCamera>
    {
        private readonly PortableDeviceManager _deviceManager;
        public bool cameraIsConnected = false;

        public PortableDeviceCollection()
        {
            _deviceManager = new PortableDeviceManager();
        }

        public void Refresh()
        {
            _deviceManager.RefreshDeviceList();

            // Determine how many WPD devices are connected
            var deviceIds = new string[1];
            uint count = 1;
            _deviceManager.GetDevices(ref deviceIds[0], ref count);

            if (count == 0)
            {
                Console.WriteLine("No devices connected.");
                cameraIsConnected = false;
                return;
            }

            cameraIsConnected = true;

            // Retrieve the device id for each connected device
            deviceIds = new string[count];
            _deviceManager.GetDevices(ref deviceIds[0], ref count);
            foreach (var deviceId in deviceIds)
            {
                Add(new PortableCamera(deviceId));
            }
        }
    }
}
