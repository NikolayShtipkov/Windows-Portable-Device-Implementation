using NowCandid.WPD.Common;
using NowCandid.WPD.Implementation;
using PortableDeviceApiLib;

namespace NowCandid.WPD
{
    public class PortableDeviceEventCallback : IPortableDeviceEventCallback
    {
        private PortableDevice _device;
        private PortableCamera _camera;
        private string _cookie;

        public PortableDeviceEventCallback(PortableDevice device, PortableCamera camera)
        {
            _device = device;
            _camera = camera;

            Register();
        }

        public PortableCamera camera
        {
            get { return _camera; }
        }

        private Guid _eventId;
        public Guid EventId
        {
            get { return _eventId; }
        }

        private string _eventDescription;
        public string EventDescription
        {
            get { return _eventDescription; }
        }

        private string _objectId;
        public string ObjectId
        {
            get { return _objectId; }
        }

        private string _objectName;
        public string ObjectName
        {
            get { return _objectName; }
        }

        private string _contentType;
        public string ContentType
        {
            get { return _contentType; }
        }

        // IPortableDeviceEventCallback Members
        public void OnEvent(IPortableDeviceValues eventParameters)
        {
            eventParameters.GetGuidValue(PortableDevicePKeys.WPD_EVENT_PARAMETER_EVENT_ID, out _eventId);

            // Determine what caused the event.
            if (_eventId.Equals(PortableDeviceGuids.WPD_EVENT_DEVICE_REMOVED))
            {
                // This event was caused by the device being removed, so indicate it is removed
                _camera.Disconnect();
                Unregister();
            }
            else if (_eventId.Equals(PortableDeviceGuids.WPD_EVENT_OBJECT_ADDED))
            {
                // This event was caused by an object (or file) being added to the card.
                eventParameters.GetStringValue(PortableDevicePKeys.WPD_OBJECT_ID, out _objectId);
                eventParameters.GetStringValue(PortableDevicePKeys.WPD_OBJECT_NAME, out _objectName);
                eventParameters.GetStringValue(PortableDevicePKeys.WPD_OBJECT_CONTENT_TYPE, out _contentType);

                // If this is a file, execute the specified delegate on the thread that owns
                _contentType = _contentType.Substring(1, _contentType.Length - 2);
                if (_contentType.Equals(PortableDeviceGuids.WPD_CONTENT_TYPE_IMAGE.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    // Import image when photo is taken
                    var file = new PortableDeviceFile(ObjectId, ObjectName);
                    camera.DownloadFile(file, @"c:\Projects Aditional Files\Images");
                }
            }
        }

        /// <summary>
        /// IPortableDevice.Advise is used to register for event notifications
        /// This returns a cookie string that is needed while unregistering.
        /// </summary>
        public void Register()
        {
            PortableDeviceGuids eventParameters = new PortableDeviceGuids();

            _device.Advise(0, this, null, out _cookie);
        }

        public void Unregister()
        {
            String errorMessage;

            try
            {
                if (string.IsNullOrEmpty(_cookie))
                {
                    errorMessage = String.Format("Invalid event cookie - '{0}'", _cookie);
                    throw new NullReferenceException();
                }

                _device.Unadvise(_cookie);
                _cookie = "";

                // Marshal.ReleaseComObject(this);
                _camera = null;
                _eventId = Guid.Empty;
                _eventDescription = "";
                _objectId = "";
            }
            catch (Exception e)
            {
                errorMessage = String.Format("Error in PortableDeviceEventCallback::Unregister - {0}", e.Message);
            }
        }
    }
}
