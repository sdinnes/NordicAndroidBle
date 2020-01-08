using System;
using System.Collections.Generic;
using System.Linq;

namespace NordicAndroidBle
{
    #region Helper classes

    /// <summary>
    /// Class to handle device discovery events
    /// </summary>    
    public class DeviceInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="d">User object</param>
        public DeviceInfoEventArgs(DeviceInfo d)
        {
            Device = d;
        }

        /// <summary>
        /// Basic device information
        /// </summary>
        public DeviceInfo Device { get; set; }
    }

    /// <summary>
    /// Simple event arguments class for log messages
    /// </summary>
    public class LogEventArgs: EventArgs
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iMsg">Log message</param>
        public LogEventArgs(string iMsg)
        {
            LogMessage = iMsg;
        }

        /// <summary>
        /// Log message
        /// </summary>
        public string LogMessage { get; private set; }
    }

    /// <summary>
    /// Simple event arguments class for progress messages
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iMsg">Progress message</param>
        /// <param name="iProgressValue">Progress value (0 - 1)</param>
        public ProgressEventArgs(string iMsg, double iProgressValue)
        {
            ProgressMessage = iMsg;
            ProgressValue = iProgressValue;
        }

        /// <summary>
        /// Progress message
        /// </summary>
        public string ProgressMessage { get; private set; }

        /// <summary>
        /// Progress value (0 - 1)
        /// </summary>
        public double ProgressValue { get; private set; }
    }

    /// <summary>
    /// Class to handle BLE operation success indication
    /// </summary>
    public class SuccessEventArgs : EventArgs
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iDevice">Bluetooth device involved in the call</param>
        /// <param name="iSuccess">Success indicator</param>
        /// <param name="iStatus">GATT status code (optional - default success)</param>
        /// <param name="iReason">Optional message, like a failure or error reason</param>
        public SuccessEventArgs(DeviceInfo iDevice, bool iSuccess, int iStatus = 0, string iReason = "")
        {
            Device = iDevice;
            Success = iSuccess;
            Status = iStatus;
            Reason = iReason;
        }

        /// <summary>
        /// Bluetooth device involved in the call
        /// </summary>
        public DeviceInfo Device { get; set; }

        /// <summary>
        /// Success indicator
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// GATT status code
        /// </summary>
        public int Status { get; private set; }

        /// <summary>
        /// Additional message, like a failure or error reason
        /// </summary>
        public string Reason { get; private set; }
    }

    /// <summary>
    /// Class to handle characteristic data read events
    /// </summary>    
    public class CharacteristicReadEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="iDeviceId">Device MAC address as a GUID</param>
        /// <param name="iCharacteristic">UUID of the characteristic that sent the data</param>
        /// <param name="iData">Data values read from the characteristic</param>
        /// <param name="iStatus">GATT status (optional - Success inferred)</param>
        /// <param name="iMessage">Optional message</param>
        public CharacteristicReadEventArgs(Guid iDeviceId, Guid iCharacteristic, byte[] iData, int iStatus = 0, string iMessage = "")
        {
            DeviceId = iDeviceId;
            Characteristic = iCharacteristic;
            Data = iData;
            Message = iMessage;
        }

        /// <summary>
        /// Device MAC address as a GUID
        /// </summary>
        public Guid DeviceId { get; private set; }

        /// <summary>
        /// UUID of the characteristic that sent the data
        /// </summary>
        public Guid Characteristic { get; private set; }

        /// <summary>
        /// Data values read from the characteristic
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// GATT status
        /// </summary>
        public int Status { get; private set; }

        /// <summary>
        /// Success indicator
        /// </summary>
        public bool Success {  get { return Status == 0; } }

        /// <summary>
        /// Optional message in case of problems
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Class to handle characteristic data write events
    /// </summary>    
    public class CharacteristicWriteEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="iDeviceId">Device MAC address as a GUID</param>
        /// <param name="iCharacteristic">UUID of the characteristic that sent the data</param>
        /// <param name="iSuccess">Indicates if the write was successful</param>
        /// <param name="iNumberOfBytesWritten">Number of bytes written</param>
        /// <param name="iStatus">GATT status (optional - Success inferred)</param>
        /// <param name="iMessage">Optional message</param>
        public CharacteristicWriteEventArgs(Guid iDeviceId, Guid iCharacteristic, bool iSuccess, int iNumberOfBytesWritten, int iStatus = 0, string iMessage = "")
        {
            DeviceId = iDeviceId;
            Characteristic = iCharacteristic;
            Success = iSuccess;
            NumberOfBytesWritten = iNumberOfBytesWritten;
            Status = iStatus;
            Message = iMessage;
        }

        /// <summary>
        /// Device MAC address as a GUID
        /// </summary>
        public Guid DeviceId { get; private set; }

        /// <summary>
        /// UUID of the characteristic that sent the data
        /// </summary>
        public Guid Characteristic { get; private set; }

        /// <summary>
        /// Indicates if the write was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Number of bytes that were written to the characteristic
        /// </summary>
        public int NumberOfBytesWritten { get; private set; }

        /// <summary>
        /// GATT status (optional - Success inferred)
        /// </summary>
        public int Status { get; private set; }

        /// <summary>
        /// Optional message in case of problems
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Event arguments for new connection state
    /// </summary>
    public class MtuEventArgs : EventArgs
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="mtu">MTU value</param>
        /// <param name="iSuccess">Success indicator</param>
        /// <param name="iError">Error code/GATT status value</param>
        public MtuEventArgs(int mtu, bool iSuccess = true, int iError = 0)
        {
            MTU = mtu;
            Success = iSuccess;
            Error = iError;
        }

        /// <summary>
        /// MTU value
        /// </summary>
        public int MTU { get; set; }

        /// <summary>
        /// Success indicator
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Error code/GATT status value
        /// </summary>
        public int Error { get; private set; }
    }

    /// <summary>
    /// Simple class to return agnostic device scan results
    /// </summary>
    public class DeviceInfo
    {
        private Guid _hardwareID;

        /// <summary>
        /// Device name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Device MAC address
        /// </summary>
        public byte[] MacAddress { get; set; }

        /// <summary>
        /// Device identity (BLE adaptor MAC address in Guid form)
        /// </summary>
        public Guid DeviceId { get; set; }

        /// <summary>
        /// The actual hardware device identity (MAC address in Guid form)
        /// </summary>
        /// <remarks>
        /// On some devices, the actual hardware ID is different from the MAC address of the BLE adaptor.  Use this property 
        /// if you want to extract a hardare ID from, for example, the advertising data. The purpose of this field is therefore 
        /// for cases where we want to present the actual ID.
        /// </remarks>
        public Guid HardwareId
        {
            get
            {
                // Validate it
                if (_hardwareID == null | _hardwareID == Guid.Empty)
                {
                    _hardwareID = DeviceId;
                }
                return _hardwareID;
            }
            set
            {
                _hardwareID = value;
            }
        }

        /// <summary>
        /// Manufacturer specific data
        /// </summary>
        public Dictionary<int, byte[]> ManufacturerSpecificData { get; set; }

        /// <summary>
        /// Advertised services for this device
        /// </summary>
        public List<Guid> Services { get; set; }

        /// <summary>
        /// Radio Signal Strength Indication (dBm)
        /// </summary>
        public int Rssi { get; set; }

        /// <summary>
        /// Device discovered in the scan
        /// </summary>
        public object ScannedDevice { get; set; }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iName">Device name</param>
        /// <param name="iDeviceId">Device MAC address</param>
        public DeviceInfo(string iName, Guid iDeviceId)
        {
            Name = iName;
            DeviceId = iDeviceId;

            // Clear the HardwareId field, in order to validate that it is replaced before use with
            // the actual hardware ID read from advertising data.  [Added build 49]
            HardwareId = Guid.Empty;
        }

        /// <summary>
        /// Show the actual device ID (MAC address) as a string in the form "AA:BB:CC:DD:EE:FF"
        /// </summary>
        public string ShortId { get { return MacAddressUtils.MacAddressAsString(HardwareId, true).ToUpper(); } }

        /// <summary>
        /// Show the actual device ID (MAC address) as a string in the form "AABBCCDDEEFF"
        /// </summary>
        public string SuperShortId { get { return MacAddressUtils.MacAddressAsString(HardwareId).ToUpper(); } }

        /// <summary>
        /// Return the manufacturer ID
        /// </summary>
        /// <returns>The manufacturer ID or 0 if not found</returns>
        /// <remarks>
        /// The manufacturer ID is returned only from the first entry in ManufacturerSpecificData. It's probably
        /// unlikely that if there is more than one that they others would differ!!
        /// </remarks>
        public int ManufacturerId()
        {
            return ManufacturerSpecificData.Count != 0 ? ManufacturerSpecificData.Keys.First() : 0;
        }
    }

    /// <summary>
    /// Delegate for passing back logging information to the caller 
    /// </summary>
    /// <param name="iMessage">Log string</param>
    public delegate void LogDelegate(string iMessage, bool iDebugOutputOnly = false);

    /// <summary>
    /// Characteristic property flags
    /// </summary>
    /// <remarks>
    /// See this very handy guide on how flags operate in C#: https://www.codeproject.com/articles/396851/ending-the-great-debate-on-enum-flags
    /// 
    /// Remember to use this:
    /// 
    /// if(myEnumValue.HasFlag(CharacteristicProperties.Read))
    /// {
    ///     // Contains Read!
    /// }
    /// 
    /// </remarks>
    [Flags]
    public enum CharacteristicProperties
    {
        /// <summary>
        /// Characteristic is broadcastable
        /// </summary>
        Broadcast = 1,

        /// <summary>
        /// Characteristic can be read
        /// </summary>
        Read = 1 << 1,

        /// <summary>
        /// Characteristic can be written without response
        /// </summary>
        WriteNoResponse = 1 << 2,

        /// <summary>
        /// Characteristic can be written
        /// </summary>
        Write = 1 << 3,

        /// <summary>
        /// Characteristic supports notification
        /// </summary>
        Notify = 1 << 4,

        /// <summary>
        /// Characteristic supports indication
        /// </summary>
        Indicate = 1 << 5,

        /// <summary>
        /// Characteristic supports write with signature
        /// </summary>
        SignedWrite = 1 << 6,

        /// <summary>
        /// Characteristic has extended properties
        /// </summary>
        ExtendedProps = 1 << 7
    }

    /// <summary>
    /// Characteristic properties to be set up in the adaptor
    /// </summary>
    public class CharacteristicsParcel
    {
        /// <summary>
        /// Service owning this characteristic
        /// </summary>
        public Guid Service { get; set; }

        /// <summary>
        /// The characteristic
        /// </summary>
        public Guid Characteristic { get; set; }

        /// <summary>
        /// Required characteristic properties
        /// </summary>
        public CharacteristicProperties Properties { get; set; }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="service">Service owning this characteristic</param>
        /// <param name="characteristic">The characteristic</param>
        /// <param name="properties">Required characteristic properties</param>
        public CharacteristicsParcel(Guid service, Guid characteristic, CharacteristicProperties properties)
        {
            Service = service;
            Characteristic = characteristic;
            Properties = properties;
        }
    }

    /// <summary>
    /// Service and characteristic properties to be set up in the adaptor
    /// </summary>
    public class ServiceAndCharacteristicsParcel
    {
        /// <summary>
        /// The service
        /// </summary>
        public Guid Service { get; internal set; }

        /// <summary>
        /// Disctionary of characteristics belonging to this service
        /// </summary>
        public Dictionary<Guid, CharacteristicsParcel> Characteristics { get; } = new Dictionary<Guid, CharacteristicsParcel>();

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iService">The service</param>
        public ServiceAndCharacteristicsParcel(Guid iService)
        {
            Service = iService;
        }

        /// <summary>
        /// Add a characteristic to the parcel
        /// </summary>
        /// <param name="iCharacteristic">A characteristic</param>
        /// <param name="iProperties">Properties for the characteristic</param>
        /// <returns>True if the characteristic is not already in the parcel</returns>
        public bool AddCharacteristic(Guid iCharacteristic, CharacteristicProperties iProperties)
        {
            if (!Characteristics.ContainsKey(iCharacteristic))
            {
                Characteristics.Add(iCharacteristic, new CharacteristicsParcel(Service, iCharacteristic, iProperties));
                return true;
            }
            return false;
        }
    }

    #endregion

    #region Base adaptor interface

    /// <summary>
    /// BLE adaptor base interface declaration
    /// </summary>
    public interface IBaseBleAdaptor
    {
        /// <summary>
        /// Specify a logging delegate
        /// </summary>
        void AddLogDelegate(LogDelegate iLogDelegate, bool iDeepLogging = false);

        /// <summary>
        /// Event to be raised when a device is discovered
        /// </summary>
        event EventHandler<DeviceInfoEventArgs> DeviceDiscoveredEvent;
        
        /// <summary>
        /// Initiate a BLE scan
        /// </summary>
        /// <param name="iDiscoveryFinished">Event handler for caller to be notified when the scan has completed</param>
        /// <param name="iScanSeconds">Number of seconds to scan; set to zero for indefinite</param>
        /// <param name="iServiceId">Optional service ID (GUID string) to filter scan results; set to null to skip</param>
        /// <param name="iManufacturerId">Optional filter on manufacturer ID; set to zero or omit to skip</param>
        /// <returns>True if apparently successul</returns>
        bool Scan(EventHandler iDiscoveryFinished, int iScanSeconds, string iServiceId = null, int iManufacturerId = 0);

        /// <summary>
        /// Scan for a single device using its address
        /// </summary>
        /// <param name="iDiscoveryFinished">Event handler for caller to be notified when the scan has completed</param>
        /// <param name="iDeviceAddress">Device address in the form of a GUID string</param>
        /// <param name="iScanSeconds">Number of seconds to scan; set to zero for indefinite</param>
        /// <returns></returns>
        bool ScanSingleDevice(EventHandler iDiscoveryFinished, string iDeviceAddress, int iScanSeconds = 0);

        /// <summary>
        /// Stop any existing Bluetooth device discovery
        /// </summary>
        void CancelScan();

        /// <summary>
        /// Event to be raised when a device is connected and ready
        /// </summary>
        event EventHandler<DeviceInfoEventArgs> DeviceReadyEvent;

        /// <summary>
        /// Event to be raised when a device disconnects
        /// </summary>
        event EventHandler<DeviceInfoEventArgs> DeviceDisconnectEvent;

        /// <summary>
        /// Event handler for when the MTU is changed
        /// </summary>
        event EventHandler<MtuEventArgs> ConnectionMtuChangeEvent;

        /// <summary>
        /// Event handler for issuing log messages
        /// </summary>
        event EventHandler<LogEventArgs> LogEvent;

        /// <summary>
        /// Indicates if we have a current connection to the device
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// Indicates if we have successfully read the device services
        /// </summary>
        bool ServicesAvailable();

        /// <summary>
        /// Returns the MAC address of the connected device in GUID format
        /// </summary>
        /// <returns>MAC address or empty Guid if not connected</returns>
        /// <remarks>
        /// If this function is successful, a Gatt connection will be opened; the caller MUST ensure that this
        /// connection is closed when no longer needed, by calling DisconnectGatt()
        /// </remarks>
        Guid ConnectedDevice();

        /// <summary>
        /// Disconnects from the current Gatt server
        /// </summary>
        /// <param name="iDisconnectionSuccess">Optional disconnection event handler</param>
        /// <remarks>
        /// Note that <see cref="DeviceDisconnectEvent"/> will also fire, if set
        /// </remarks>
        void DisconnectGatt(EventHandler<DeviceInfoEventArgs> iDisconnectionSuccess = null);

        /// <summary>
        /// Returns the friendly name of the connected device
        /// </summary>
        /// <returns>Friendly name or an empty string if not connected</returns>
        string ConnectedDeviceName();

        /// <summary>
        /// Return the MTU of the current connection
        /// </summary>
        /// <returns></returns>
        int MTU();

        /// <summary>
        /// Return a list of services provided by the connected device
        /// </summary>
        /// <returns>List of service <see cref="Guid"/> values</returns>
        List<Guid> DeviceServices();

        /// <summary>
        /// Set up a notification for when a read characteristic changes
        /// </summary>
        /// <param name="iService">Service providing the characteristic</param>
        /// <param name="iCharacteristic">Characteristic we want to get data from</param>
        /// <param name="iDataReceivedEvent">Event handler which will receive the pushed data from the characteristic</param>
        /// <param name="iDataWrittenEvent">Event handler on which to return the notification results</param>
        /// <returns>True if we successfully subscribed to a notification</returns>
        bool NotifyCharacteristic(string iService, string iCharacteristic, EventHandler<CharacteristicReadEventArgs> iDataReceivedEvent, EventHandler<CharacteristicWriteEventArgs> iDataWrittenEvent);

        /// <summary>
        /// Clear the previously set up notification for when a read characteristic changes
        /// </summary>
        /// <param name="iService">Service providing the characteristic</param>
        /// <param name="iCharacteristic">Characteristic we no longer want to get data from</param>
        /// <param name="iDataWrittenEvent">Event handler on which to return the clear results</param>
        /// <returns>True if we successfully unsubscribed from the notification</returns>
        bool ClearCharacteristic(string iService, string iCharacteristic, EventHandler<CharacteristicWriteEventArgs> iDataWrittenEvent);

        /// <summary>
        /// Dispose of the internal BLE adaptor based on Nordic Semiconductor BleManager
        /// </summary>
        void RemoveAdaptor();
    }

    #endregion

    #region Nordic Semiconductor BLE library

    /// <summary>
    /// BLE adaptor that uses Nordic Semiconductor Android code
    /// </summary>
    public interface INordicBleAdaptor : IBaseBleAdaptor
    {
        /// <summary>
        /// Find and connect to a device using its MAC address
        /// </summary>
        /// <param name="iDevice">Device info providing a MAC address</param>
        /// <param name="iMTU">Required MTU for connection (set to 0 if not required)</param>
        /// <param name="iServices">List of services and characteristic we are going to use</param>
        /// <param name="iBond">Optional parameter you set to true if you wish to pair (bond) with the device</param>
        /// <param name="iConnectionEvent">Optional callback event for connection success or failure</param>
        void ConnectToDevice(DeviceInfo iDevice, 
            int iMTU, 
            List<ServiceAndCharacteristicsParcel> iServices, 
            bool iBond = false,
            EventHandler<SuccessEventArgs> iConnectionEvent = null);

        /// <summary>
        /// Make an external MTU change request
        /// </summary>
        /// <param name="iMTU">Required MTU</param>
        /// <param name="iConnectionMtuChangeEvent">Event handler to fire when completed</param>
        void RequestMtu(int iMTU, EventHandler<MtuEventArgs> iConnectionMtuChangeEvent);

        /// <summary>
        /// Read data from a service characteristic
        /// </summary>
        /// <param name="iService">Service providing the characteristic</param>
        /// <param name="iCharacteristic">Characteristic to get data from</param>
        /// <param name="iDataReadEvent">Event handler on which to return the read results</param>
        /// <returns>True if device connected</returns>
        /// <remarks>
        /// A True result from this call does not indicate if the read was actually successful! The caller must wait for a result via
        /// iDataReadEvent and inspect the values returned.
        /// </remarks>
        bool ReadCharacteristic(string iService, string iCharacteristic, EventHandler<CharacteristicReadEventArgs> iDataReadEvent);

        /// <summary>
        /// Write data to a service characteristic
        /// </summary>
        /// <param name="iService">Service providing the characteristic</param>
        /// <param name="iCharacteristic">Characteristic to write data to</param>
        /// <param name="iData">Data to write</param>
        /// <param name="iDataWrittenEvent">Event handler on which to return the write results</param>
        /// <returns>True if device connected</returns>
        /// <remarks>
        /// A True result from this call does not indicate if the send was actually successful! The caller must wait for a result via
        /// iDataWrittenEvent and inspect the Success value returned.
        /// </remarks>
        bool WriteCharacteristic(string iService, string iCharacteristic, byte[] iData, EventHandler<CharacteristicWriteEventArgs> iDataWrittenEvent);
    }

    #endregion

    #region Various Guid and MAC Address helper functions

    public class MacAddressUtils
    {
        private const string GUID_TEMPLATE = "00000000-0000-0000-0000-{0}";
        private const int NUMBER_OF_BYTES_IN_ADDRESS = 6;

        /// <summary>
        /// Create a Guid using a MAC address string in the form "00:11:22:AA:BB:CC"
        /// </summary>
        /// <param name="iAddress">MAC address string in the form "00:11:22:AA:BB:CC" (separators optional)</param>
        /// <returns>A MAC address in the form of a Guid</returns>
        public static Guid GuidFromMacAddress(string iAddress)
        {
            // Remove colons
            iAddress = iAddress.Replace(":", string.Empty);

            // Build up into a full GUID string and create
            return new Guid(string.Format(GUID_TEMPLATE, iAddress));
        }

        /// <summary>
        /// Create a Guid using a MAC address in the form of a 6 byte array
        /// </summary>
        /// <param name="iAddress">MAC address in the form of an array of 6 bytes</param>
        /// <returns>A MAC address in the form of a Guid</returns>
        public static Guid GuidFromMacAddress(byte[] iAddress)
        {
            // Convert into string
            string address = string.Empty;
            for (int i = 0; i < NUMBER_OF_BYTES_IN_ADDRESS; i++)
            {
                address += iAddress[i].ToString("X2");
            }

            // Build up into a full GUID string and create
            return new Guid(string.Format(GUID_TEMPLATE, address));
        }

        /// <summary>
        /// Return a MAC address as a string like "AABBCCDDEEFF"
        /// </summary>
        /// <param name="iAddress">MAC address as byte array</param>
        /// <param name="includePeriod">Optional parameter to include separator (colon)</param>
        /// <returns>A MAC address as a string like "AA:BB:CC:DD:EE:FF" or "AABBCCDDEEFF"</returns>
        public static string MacAddressAsString(byte[] iAddress, bool includePeriod = false)
        {
            // Convert into string
            string address = string.Empty;
            for (int i = 0; i < NUMBER_OF_BYTES_IN_ADDRESS; i++)
            {
                address += iAddress[i].ToString("X2");
                if (includePeriod & i < NUMBER_OF_BYTES_IN_ADDRESS - 1) address += ":";
            }

            // Build up into a full GUID string and create
            return address;
        }

        /// <summary>
        /// Return a MAC address as a string like "AABBCCDDEEFF"
        /// </summary>
        /// <param name="iAddress">MAC address as a GUID</param>
        /// <param name="includePeriod">Optional parameter to include separator (colon)</param>
        /// <returns>A MAC address as a string like "AA:BB:CC:DD:EE:FF" or "AABBCCDDEEFF"</returns>
        public static string MacAddressAsString(Guid iAddress, bool includePeriod = false)
        {
            byte[] address = MacAddressBytesFromGiud(iAddress);
            return MacAddressAsString(address, includePeriod);
        }

        /// <summary>
        /// Extract a MAC address from the last six bytes of a GUID
        /// </summary>
        /// <param name="iGuid">GUID containing a MAC address in its last six bytes</param>
        /// <returns>Byte array containing a MAC address</returns>
        public static byte[] MacAddressBytesFromGiud(Guid iGuid)
        {
            if (iGuid == null) return null;

            // Get all bytes of the Guid and copy them into an array of the required length (6)
            byte[] guidbytes = iGuid.ToByteArray();
            byte[] result = new byte[NUMBER_OF_BYTES_IN_ADDRESS];
            Array.Copy(guidbytes, guidbytes.Length - NUMBER_OF_BYTES_IN_ADDRESS, result, 0, NUMBER_OF_BYTES_IN_ADDRESS);
            return result;
        }

        /// <summary>
        /// Extract a MAC address from a string representation of a Mac address
        /// </summary>
        /// <param name="iGuid">MAC address in string form, with or without the separator</param>
        /// <returns>Byte array containing a MAC address</returns>
        public static byte[] MacAddressBytesFromMacAddress(string iGuid)
        {
            return MacAddressBytesFromGiud(GuidFromMacAddress(iGuid));
        }

        /// <summary>
        /// Convert a Guid to a MAC address
        /// </summary>
        /// <param name="iGuid">GUID containing a MAC address in its last six bytes</param>
        /// <returns>A string similar to "00:11:22:33:AA:BB"</returns>
        /// <remarks>
        /// Only the final six bytes of the Guid are used; normally, when received from a BLE scan, the 
        /// address will be like this: 00000000-0000-0000-0000-f86ba17f480a
        /// </remarks>
        public static string MacAddressFromGiud(Guid iGuid)
        {
            byte[] values = MacAddressBytesFromGiud(iGuid);
            string result = string.Empty;

            if (values != null)
            {
                for (int i = 0; i < NUMBER_OF_BYTES_IN_ADDRESS; i++)
                {
                    result += string.Format("{0:X2}:", values[i]);
                }
                return result.Remove(result.Length - 1);
            }
            return result;
        }

        /// <summary>
        /// Compares an address in string form to a Mac address in 6 byte form
        /// </summary>
        /// <param name="iUId">Address to match</param>
        /// <param name="iMacAddress">Mac address</param>
        /// <returns>True if the string matches the Mac address converted to string form, either with or without the colon separator</returns>
        public static bool MatchingUid(string iUId, byte[] iMacAddress)
        {
            string mac = MacAddressAsString(iMacAddress);
            if (mac.ToUpper() == iUId.ToUpper())
            {
                return true;
            }
            else
            {
                mac = MacAddressAsString(iMacAddress, true);
                return mac.ToUpper() == iUId.ToUpper();
            }
        }
    }

    #endregion

}
