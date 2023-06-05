using NowCandid.WPD.Implementation;

namespace NowCandid.WPD
{
    class Program
    {
        static void Main()
        {
            var collection = new PortableDeviceCollection();
            collection.Refresh();
            if (!collection.cameraIsConnected)
            {
                Console.WriteLine("Connect a camera and try again.");
                return;
            }

            var device = collection[0];
            device.Connect();

            Console.WriteLine("Device name: {0}", device.FriendlyName);

            var portableDeviceCallback = new PortableDeviceEventCallback(device.device, device);

            while (true)
            {
            }
        }
    }
}
