using System.Runtime.InteropServices;
using NowCandid.WPD.Common;
using PortableDeviceApiLib;
using PortableDeviceTypesLib;
using _tagpropertykey = PortableDeviceApiLib._tagpropertykey;
using IPortableDeviceValues = PortableDeviceApiLib.IPortableDeviceValues;

namespace NowCandid.WPD.Implementation
{
    public class PortableCamera
    {
        private bool _isConnected;
        public readonly PortableDevice device;

        public PortableCamera(string deviceId)
        {
            device = new PortableDevice();
            DeviceId = deviceId;
        }

        public string DeviceId { get; set; }

        public void Connect()
        {
            if (_isConnected) { return; }

            var clientInfo = (IPortableDeviceValues)new PortableDeviceValues();
            device.Open(DeviceId, clientInfo);
            _isConnected = true;
        }

        public void Disconnect()
        {
            if (!_isConnected) { return; }
            device.Close();
            _isConnected = false;
        }

        public string FriendlyName
        {
            get
            {
                if (!_isConnected)
                {
                    throw new InvalidOperationException("Not connected to device.");
                }

                // Retrieve the properties of the device
                IPortableDeviceContent content;
                IPortableDeviceProperties properties;
                device.Content(out content);
                content.Properties(out properties);

                // Retrieve the values for the properties
                IPortableDeviceValues propertyValues;
                properties.GetValues("DEVICE", null, out propertyValues);

                // Identify the property to retrieve
                var property = new _tagpropertykey();
                property.fmtid = PortableDevicePKeys.WPD_DEVICE_FRIENDLY_NAME.fmtid;
                property.pid = 12;

                // Retrieve the friendly name
                string propertyValue;
                propertyValues.GetStringValue(ref property, out propertyValue);

                return propertyValue;
            }
        }

        public PortableDeviceFolder GetContents()
        {
            var root = new PortableDeviceFolder("DEVICE", "DEVICE");

            IPortableDeviceContent content;
            device.Content(out content);
            EnumerateContents(ref content, root);

            return root;
        }

        private static void EnumerateContents(ref IPortableDeviceContent content, PortableDeviceFolder parent)
        {
            // Get the properties of the object
            IPortableDeviceProperties properties;
            content.Properties(out properties);

            // Enumerate the items contained by the current object
            IEnumPortableDeviceObjectIDs objectIds;
            content.EnumObjects(0, parent.Id, null, out objectIds);

            uint fetched = 0;
            do
            {
                string objectId;

                objectIds.Next(1, out objectId, ref fetched);
                if (fetched > 0)
                {
                    var currentObject = WrapObject(properties, objectId);

                    parent.Files.Add(currentObject);

                    if (currentObject is PortableDeviceFolder)
                    {
                        EnumerateContents(ref content, (PortableDeviceFolder)currentObject);
                    }
                }
            } while (fetched > 0);
        }

        private static PortableDeviceObject WrapObject(IPortableDeviceProperties properties, string objectId)
        {
            PortableDeviceApiLib.IPortableDeviceKeyCollection keys;
            properties.GetSupportedProperties(objectId, out keys);

            IPortableDeviceValues values;
            properties.GetValues(objectId, keys, out values);

            // Get the name of the object
            string name;
            var property = new _tagpropertykey();
            property.fmtid = PortableDevicePKeys.WPD_OBJECT_ID.fmtid;
            property.pid = 4;
            values.GetStringValue(property, out name);

            // Get the type of the object
            Guid contentType;
            property = new _tagpropertykey();
            property.fmtid = PortableDevicePKeys.WPD_OBJECT_ID.fmtid;
            property.pid = 7;
            values.GetGuidValue(property, out contentType);

            var folderType = PortableDeviceGuids.WPD_CONTENT_TYPE_FOLDER;
            var functionalType = PortableDeviceGuids.WPD_CONTENT_TYPE_FUNCTIONAL_OBJECT;

            if (contentType == folderType || contentType == functionalType)
            {
                return new PortableDeviceFolder(objectId, name);
            }

            return new PortableDeviceFile(objectId, name);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false, EntryPoint = "SHCreateStreamOnFileW")]
        static extern void SHCreateStreamOnFile(string fileName, StgmConstants mode, ref PortableDeviceApiLib.IStream stream);
        public void DownloadFile(PortableDeviceFile file, string saveToPath)
        {
            IPortableDeviceContent content;
            device.Content(out content);

            IPortableDeviceResources resources;
            content.Transfer(out resources);

            uint optimalTransferSize = 0;
            PortableDeviceApiLib.IStream sourceStream;
            resources.GetStream(file.Id, ref PortableDevicePKeys.WPD_RESOURCE_DEFAULT, 0, ref optimalTransferSize, out sourceStream);

            var filename = Path.GetFileName(file.Name) + ".jpg";
            PortableDeviceApiLib.IStream targetStream = null;
            SHCreateStreamOnFile(Path.Combine(saveToPath, filename),
                StgmConstants.STGM_WRITE | StgmConstants.STGM_CREATE | StgmConstants.STGM_FAILIFTHERE, ref targetStream);

            unsafe
            {
                byte[] objectData = new byte[optimalTransferSize];
                uint bytesRead = 0;
                uint bytesWritten = 0;

                do
                {
                    sourceStream.RemoteRead(out objectData[0], optimalTransferSize, out bytesRead);
                    targetStream.RemoteWrite(ref objectData[0], bytesRead, out bytesWritten);
                } while (bytesRead > 0);

                targetStream.Commit((uint)StgcConstants.STGC_DEFAULT);
                Marshal.ReleaseComObject(targetStream);
                Marshal.ReleaseComObject(sourceStream);
            }
        }
    }
}
