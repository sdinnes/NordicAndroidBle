
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Android.Bluetooth;
using Android.OS;
using Android.Util;
using NordicAndroidBle.Droid;
using NO.Nordicsemi.Android.Ble;
using NO.Nordicsemi.Android.Support.V18.Scanner;
using Xamarin.Forms;
using Timer = System.Timers.Timer;

[assembly: Dependency(typeof(NordicBleAdaptor))]
namespace NordicAndroidBle.Droid
{
    #region Scan call back and event handlers

    /// <summary>
    /// Event arguments for batch scan results
    /// </summary>
    public class NordicBatchScanResultsEventArgs : EventArgs
    {
        public NordicBatchScanResultsEventArgs(IList<ScanResult> iResults)
        {
            Results = iResults;
        }

        public IList<ScanResult> Results { get; set; }
    }
    /// <summary>
    /// Event arguments for scan failed results
    /// </summary>
    public class NordicScanFailureEventArgs : EventArgs
    {
        public NordicScanFailureEventArgs(int iErrorCode)
        {
            ErrorCode = iErrorCode;
        }

        public int ErrorCode { get; set; }
    }
    /// <summary>
    /// Event arguments for a scan result
    /// </summary>
    public class NordicScanResultEventArgs : EventArgs
    {
        public NordicScanResultEventArgs(int iCallBackType, ScanResult iResults)
        {
            CallBackType = iCallBackType;
            Results = iResults;
        }

        public int CallBackType { get; private set; }

        public ScanResult Results { get; set; }
    }

    /// <summary>
    /// Overridden class to handle BLE scan results callbacks
    /// </summary>
    public class NordicScanCallBack : ScanCallback
    {
        /// <summary>
        /// Event handler for when batch scan results return
        /// </summary>
        public event EventHandler<NordicBatchScanResultsEventArgs> BatchScanResults;

        /// <summary>
        /// Event handler for when the scan fails
        /// </summary>
        public event EventHandler<NordicScanFailureEventArgs> ScanFailure;

        /// <summary>
        /// Event handler for when a device is found
        /// </summary>
        public event EventHandler<NordicScanResultEventArgs> ScanResult;

        /// <summary>
        /// Batch scan results call back
        /// </summary>
        /// <param name="results">Batch scan results</param>
        public override void OnBatchScanResults(IList<ScanResult> results)
        {
            base.OnBatchScanResults(results);
            DoBatchScanResults(results);
        }

        /// <summary>
        /// Event handler for batch scan results
        /// </summary>
        /// <param name="results">Batch scan results</param>
        private void DoBatchScanResults(IList<ScanResult> results)
        {
            BatchScanResults?.Invoke(this, new NordicBatchScanResultsEventArgs(results));
        }

        /// <summary>
        /// Scan failed call back
        /// </summary>
        /// <param name="errorCode">Scan failure error code</param>
        public override void OnScanFailed(int errorCode)
        {
            base.OnScanFailed(errorCode);
            DoScanFailed(errorCode);
        }

        /// <summary>
        /// Event handler for scan failure
        /// </summary>
        /// <param name="errorCode">Scan failure error code</param>
        private void DoScanFailed(int errorCode)
        {
            ScanFailure?.Invoke(this, new NordicScanFailureEventArgs(errorCode));
        }

        /// <summary>
        /// Scan result call back
        /// </summary>
        /// <param name="callbackType">Scan call back type</param>
        /// <param name="result">Scan result (a device)</param>
        public override void OnScanResult(int callbackType, ScanResult result)
        {
            base.OnScanResult(callbackType, result);
            DoBatchScanResults(callbackType, result);
        }

        /// <summary>
        /// Event handler for a scan results (a device found)
        /// </summary>
        /// <param name="callbackType">Scan call back type</param>
        /// <param name="result">Scan result (a device)</param>
        private void DoBatchScanResults(int callbackType, ScanResult result)
        {
            ScanResult?.Invoke(this, new NordicScanResultEventArgs(callbackType, result));
        }

        /// <summary>
        /// Clear call backs for scanning
        /// </summary>
        /// <returns>Number of events removed from call backs</returns>
        public int ClearCallers()
        {
            int? c = 0;
            if (ScanResult != null)
            {
                c += ScanResult?.GetInvocationList().Count();
                foreach (Delegate d in ScanResult?.GetInvocationList())
                {
                    ScanResult -= (EventHandler<NordicScanResultEventArgs>)d;
                }
            }
            if (ScanFailure != null)
            {
                c += ScanFailure?.GetInvocationList().Count();
                foreach (Delegate d in ScanFailure?.GetInvocationList())
                {
                    ScanFailure -= (EventHandler<NordicScanFailureEventArgs>)d;
                }
            }
            return c ?? 0;
        }
    }

    #endregion

    /// <summary>
    /// Android implementation of a BLE adaptor that uses Nordic Semiconductor Java code imported via bindings
    /// </summary>
    class NordicBleAdaptor : INordicBleAdaptor
    {
        #region Constants

        /// <summary>
        /// Number of times to attempt a connection
        /// </summary>
        private const int CONNECT_RETRIES = 10;

        /// <summary>
        /// Delay between connection tries
        /// </summary>
        private const int CONNECT_RETRY_DELAY_MS = 1000;

        /// <summary>
        /// Default MTU - the nominal lowest value
        /// </summary>
        private const int DEFAULT_MTU = 20;

        /// <summary>
        /// Period to wait until timing out after sending data
        /// </summary>
        private const int DATA_SEND_WAIT_TIME_MS = 10000;

        #endregion

        #region Private members

        /// <summary>
        /// Instance of the class that descends from <see cref="BleManager"/> which we can create and dispose at will
        /// (in contrast to this class <see cref="NordicBleAdaptor"/> which is a singleton)
        /// </summary>
        private static MyBleManager _bleManager = null;

        // Logging
        private LogDelegate _logDelegate;
        private bool _deepLogging;

        private NordicScanCallBack _skfScanCallback;
        private NordicScanCallBack _previousScanCallback;

        private bool _scanning;

        /// <summary>
        /// Timer used to stop the scan if using fixed scan interval
        /// </summary>
        private Timer _scanTimer;

        /// <summary>
        /// List of discovered Bluetooth devices picked up during scan
        /// </summary>
        private List<BluetoothDevice> FoundDevices = new List<BluetoothDevice>();

        /// <summary>
        /// List of devices discovered
        /// </summary>
        private List<DeviceInfo> Devices = new List<DeviceInfo>();

        /// <summary>
        /// Initial MTU, updated if changed successfully to the actual value
        /// </summary>
        private int _mtu = DEFAULT_MTU;

        /// <summary>
        /// Indicates if device services have been discovered
        /// </summary>
        private bool _servicesDiscovered;

        /// <summary>
        /// Buffer for receiving data after a characteristic read request
        /// </summary>
        private byte[] _readData;

        /// <summary>
        /// Event handler for connection MTU change
        /// </summary>
        private EventHandler<MtuEventArgs> _connectionMtuChangeEvent;

        /// <summary>
        /// Event handler for connection success
        /// </summary>
        private EventHandler<SuccessEventArgs> _connectionSuccessEventArgs;

        /// <summary>
        /// Event handler for disconnection that is called with DisconnectGatt()
        /// </summary>
        private EventHandler<DeviceInfoEventArgs> _localDisconnectionSuccess;

        #endregion

        #region Event handlers

        /// <summary>
        /// Event to be raised when scan finishes
        /// </summary>
        public EventHandler DiscoveryFinished { get; private set; }

        /// <summary>
        /// Event to be raised when a device is discovered
        /// </summary>
        public event EventHandler<DeviceInfoEventArgs> DeviceDiscoveredEvent;

        /// <summary>
        /// Event to be raised when a device is connected
        /// </summary>
        public event EventHandler<DeviceInfoEventArgs> DeviceConnectedEvent;

        /// <summary>
        /// Event to be raised when a device is ready
        /// </summary>
        public event EventHandler<DeviceInfoEventArgs> DeviceReadyEvent;

        /// <summary>
        /// Event to be raised when a device disconnects
        /// </summary>
        public event EventHandler<DeviceInfoEventArgs> DeviceDisconnectEvent;

        /// <summary>
        /// Event handler for when the MTU is changed
        /// </summary>
        public event EventHandler<MtuEventArgs> ConnectionMtuChangeEvent;

        /// <summary>
        /// Event to be raised when a device is bonded
        /// </summary>
        public event EventHandler<DeviceInfoEventArgs> DeviceBondedEvent;

        /// <summary>
        /// Event handler for issuing log messages
        /// </summary>
        public event EventHandler<LogEventArgs> LogEvent;

        #endregion

        #region Constructor

        /// <summary>
        /// Create the internal BLE adaptor based on Nordic Semiconductor BleManager
        /// </summary>
        public void CreateAdaptor()
        {
            if (_bleManager == null)
            {
                Log("Creating BLE adaptor");

                // Create the adaptor
                _bleManager = new MyBleManager();

                // Subscribe to event handlers related to device connections
                _bleManager.DeviceConnectedEvent += _bleManager_DeviceConnectedEvent;
                _bleManager.DeviceReadyEvent += _bleManager_DeviceReadyEvent;
                _bleManager.DeviceDisconnectedEvent += _bleManager_DeviceDisconnectedEvent;
                _bleManager.MtuChangeEvent += _bleManager_MtuChangeEvent;
                _bleManager.ServicesDiscoveredEvent += _bleManager_ServicesDiscoveredEvent;

                // Deep logging
                _bleManager.LogEvent += _bleManager_LogEvent;
            }
        }

        /// <summary>
        /// Event handler for deep logging from the Nordic base class
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including a log message</param>
        private void _bleManager_LogEvent(object sender, LogEventArgs e)
        {
            if (_deepLogging)
            {
                Log(e.LogMessage, true);
            }
        }

        /// <summary>
        /// Dispose of the internal BLE adaptor based on Nordic Semiconductor BleManager
        /// </summary>
        public void RemoveAdaptor()
        {
            if (ManagerPresent())
            {
                Log("Removing BLE adaptor");

                // Unsubscribe from event handlers
                _bleManager.ClearEventHandlers();
                
                // Deep logging
                _bleManager.LogEvent -= _bleManager_LogEvent;

                // Nullify our reference to the adaptor
                // Note - not calling Dispose as it may be responsible for the "use of deleted global reference" error that can crash the app
                // _bleManager.Dispose();
                _bleManager = null;
            }
        }

        #endregion

        #region Public members - log delegate

        /// <summary>
        /// Specify a logging delegate
        /// </summary>
        public void AddLogDelegate(LogDelegate iLogDelegate, bool iDeepLogging = false)
        {
            _logDelegate = iLogDelegate;
            _deepLogging = iDeepLogging;
        }

        #endregion

        #region Scanning

        /// <summary>
        /// Initiate a BLE scan
        /// </summary>
        /// <param name="iDiscoveryFinished">Event handler for caller to be notified when the scan has completed</param>
        /// <param name="iScanSeconds">Number of seconds to scan; set to zero for indefinite</param>
        /// <param name="iServiceId">Optional service ID (GUID string) to filter scan results; set to null to skip</param>
        /// <param name="iManufacturerId">Optional filter on manufacturer ID; set to zero or omit to skip</param>
        /// <returns>True if apparently successul</returns>
        public bool Scan(EventHandler iDiscoveryFinished, int iScanSeconds, string iServiceId = null, int iManufacturerId = 0)
        {
            // Hold the event handler
            DiscoveryFinished = iDiscoveryFinished ?? throw new ArgumentNullException("Disovery finished event handler must be specified");

            // Ensure the adaptor is present
            CreateAdaptor();

            try
            {
                // Clear list of received devices
                FoundDevices.Clear();

                // Ensure it has stopped
                StopScan();

                BluetoothLeScannerCompat scanner = BluetoothLeScannerCompat.Scanner;

                // If there was a previous scan, flush its results before we start another
                //if (_previousScanCallback != null)
                //{
                //    scanner.FlushPendingScanResults(_previousScanCallback);
                //    _previousScanCallback = null;
                //}

                ScanSettings settings = new ScanSettings.Builder()
                            .SetLegacy(false)
                            .SetScanMode(ScanSettings.ScanModeLowLatency)
                            //.SetReportDelay(1000)
                            .SetUseHardwareBatchingIfSupported(false).Build();

                List<ScanFilter> filters = new List<ScanFilter>();

                if (!string.IsNullOrEmpty(iServiceId))
                {
                    ParcelUuid mUuid = ParcelUuid.FromString(iServiceId);

                    filters.Add(new ScanFilter.Builder().SetServiceUuid(mUuid).Build());
                }

                if (iManufacturerId != 0)
                {
                    filters.Add(new ScanFilter.Builder().SetManufacturerData(iManufacturerId, new byte[1] { 0 }, new byte[1] { 0 }).Build());
                }

                _skfScanCallback = new NordicScanCallBack();

                // Add in the event handlers
                _skfScanCallback.ScanResult += Receiver_ScanResult;
                _skfScanCallback.ScanFailure += Receiver_ScanFailure;

                // Scan
                scanner.StartScan(filters, settings, _skfScanCallback);
                _scanning = true;

                if (iScanSeconds != 0)
                {
                    // Use a timer to end the scan
                    _scanTimer = new Timer(iScanSeconds * 1000);
                    _scanTimer.Elapsed += _scanTimer_Elapsed;
                    _scanTimer.AutoReset = false;
                    _scanTimer.Start();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log(string.Format("Exception thrown in Scan: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Scan for a single device using its address
        /// </summary>
        /// <param name="iDiscoveryFinished">Event handler for caller to be notified when the scan has completed</param>
        /// <param name="iDeviceAddress">Device address in the form of a GUID string</param>
        /// <param name="iScanSeconds">Number of seconds to scan; set to zero for indefinite</param>
        /// <returns></returns>
        public bool ScanSingleDevice(EventHandler iDiscoveryFinished, string iDeviceAddress, int iScanSeconds = 0)
        {
            // Hold the event handler
            DiscoveryFinished = iDiscoveryFinished ?? throw new ArgumentNullException("Disovery finished event handler must be specified");

            try
            {
                // Clear list of received devices
                FoundDevices.Clear();

                // Ensure it has stopped
                StopScan();

                BluetoothLeScannerCompat scanner = BluetoothLeScannerCompat.Scanner;

                ScanSettings settings = new ScanSettings.Builder()
                            .SetLegacy(false)
                            .SetScanMode(ScanSettings.ScanModeLowLatency)
                            .SetUseHardwareBatchingIfSupported(false).Build();

                List<ScanFilter> filters = new List<ScanFilter>();

                // Add filter to return only the specified device
                filters.Add(new ScanFilter.Builder().SetDeviceAddress(iDeviceAddress).Build());

                _skfScanCallback = new NordicScanCallBack();

                // Add in the event handlers
                _skfScanCallback.ScanResult += Receiver_ScanResult;
                _skfScanCallback.ScanFailure += Receiver_ScanFailure;

                // Scan
                scanner.StartScan(filters, settings, _skfScanCallback);
                _scanning = true;

                if (iScanSeconds != 0)
                {
                    // Use a timer to end the scan
                    _scanTimer = new Timer(iScanSeconds * 1000);
                    _scanTimer.Elapsed += _scanTimer_Elapsed;
                    _scanTimer.AutoReset = false;
                    _scanTimer.Start();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log(string.Format("Exception thrown in ScanSingleDevice: {0}", ex.Message));
                return false;
            }
        }

        private void _scanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log(string.Format("Scan stopped on timer: {0} devices", FoundDevices.Count));
            _scanTimer.Elapsed -= _scanTimer_Elapsed;
            StopScan();
        }

        /// <summary>
        /// Stop any existing Bluetooth device discovery
        /// </summary>
        public void CancelScan()
        {
            StopScan();
        }

        #endregion

        #region Device connection

        /// <summary>
        /// Find and connect to a device using its MAC address
        /// </summary>
        /// <param name="iDevice">Device info providing a MAC address</param>
        /// <param name="iMTU">Required MTU for connection (specify 0 if not required)</param>
        /// <param name="iServices">List of services and characteristic we are going to use</param>
        /// <param name="iBond">Optional parameter you set to true if you wish to pair (bond) with the device</param>
        /// <param name="iConnectionEvent">Optional callback event for connection success or failure</param>
        public void ConnectToDevice(DeviceInfo iDevice, 
            int iMTU, 
            List<ServiceAndCharacteristicsParcel> iServices, 
            bool iBond = false,
            EventHandler<SuccessEventArgs> iConnectionEvent = null)
        {
            // Ensure the adaptor is present
            CreateAdaptor();

            // Find the BluetoothDevice already scanned
            string mac = MacAddressUtils.MacAddressAsString(iDevice.DeviceId, true);

            // Clear some fields
            _mtu = DEFAULT_MTU;
            _servicesDiscovered = false;

            // Specify the services and characteristics we are going to use 
            _bleManager.ServiceAndCharacteristics(iServices);

            // Locate the device object - must have already been scanned
            BluetoothDevice bluetoothDevice = FoundDevices.FirstOrDefault(d => d.Address == mac);

            // Create the connection request
            ConnectRequest request = _bleManager.Connect(bluetoothDevice);

            // Add in handlers if specified
            if (iConnectionEvent != null)
            {
                _connectionSuccessEventArgs = iConnectionEvent;
                request
                    .Done(new SuccessCallBack(ConnectionRequestEvent))
                    .Fail(new SuccessCallBack(ConnectionRequestEvent));
            }
            else
                _connectionSuccessEventArgs = null;

            // Enqueue the connection request and process it
            request.Retry(CONNECT_RETRIES, CONNECT_RETRY_DELAY_MS)
                .UseAutoConnect(false)
                .Enqueue();

            // Bond to the device if required
            if (iBond)
            {
                // Request bonding with device
                _bleManager.BondToDevice(new SuccessCallBack(_bleManager_DeviceBondedEvent));
            }

            // Enqueue an MTU request if required
            if (iMTU != 0)
            {
                Thread.Sleep(50);
                _bleManager.MakeMtuRequest(iMTU);
            }
        }

        /// <summary>
        /// Event handler for connection request result
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success</param>
        private void ConnectionRequestEvent(object sender, DeviceEventArgs e)
        {
            _connectionSuccessEventArgs?.Invoke(this, new SuccessEventArgs(FindDeviceInfo(e.Device), e.Status == 0, (int)e.Status));
        }

        /// <summary>
        /// Make an external MTU change request
        /// </summary>
        /// <param name="iMTU">Required MTU</param>
        /// <param name="iConnectionMtuChangeEvent">Event handler to fire when completed</param>
        public void RequestMtu(int iMTU, EventHandler<MtuEventArgs> iConnectionMtuChangeEvent)
        {
            _connectionMtuChangeEvent = iConnectionMtuChangeEvent;

            _bleManager.MakeMtuRequest(iMTU, CreateMtuRequestCallBack());
        }

        /// <summary>
        /// Create the callback class for receiving notification of change to MTU
        /// </summary>
        /// <returns>Instance of a class implementing IMtuCallback</returns>
        private MtuRequestCallBack CreateMtuRequestCallBack()
        {
            MtuRequestCallBack mtuRequest = new MtuRequestCallBack();

            mtuRequest.DeviceMtuEvent += MtuRequest_DeviceMtuEvent;

            return mtuRequest;
        }

        /// <summary>
        /// Event handler to tell caller that the external MTU request has been handled
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including the new MTU value</param>
        /// <remarks>
        /// This handler is only invoked when requesting MTU manually outside of Connect() method
        /// </remarks>
        private void MtuRequest_DeviceMtuEvent(object sender, DeviceMtuEventArgs e)
        {
            // Must update local setting if successful
            if (e.Success)
            {
                _mtu = e.MTU;
                _connectionMtuChangeEvent?.Invoke(this, new MtuEventArgs(e.MTU));
            }
            else
            {
                _connectionMtuChangeEvent?.Invoke(this, new MtuEventArgs(0, e.Success, e.MTU));
            }
        }

        public Guid ConnectedDevice()
        {
            return ManagerPresent() ? MacAddressUtils.GuidFromMacAddress(_bleManager.BluetoothDevice?.Address) : Guid.Empty;
        }

        private bool ManagerPresent()
        {
            return _bleManager != null;
        }

        public string ConnectedDeviceName()
        {
            return ManagerPresent() ? _bleManager.BluetoothDevice.Name : string.Empty;
        }

        public bool IsConnected()
        {
            return ManagerPresent() ? _bleManager.IsConnected : false;
        }

        /// <summary>
        /// Disconnects from the current Gatt server
        /// </summary>
        /// <param name="iDisconnectionSuccess">Optional disconnection event handler</param>
        /// <remarks>
        /// Note that <see cref="DeviceDisconnectEvent"/> will also fire, if set
        /// </remarks>
        public void DisconnectGatt(EventHandler<DeviceInfoEventArgs> iDisconnectionSuccess = null)
        {
            // Disconnect the device
            if (ManagerPresent())
            {
                DisconnectRequest request = _bleManager.Disconnect();
                request.Done(new SuccessCallBack(_bleManager_DeviceDisconnectedEvent));
                _localDisconnectionSuccess = iDisconnectionSuccess;
                request.Enqueue();
            }
        }

        #endregion

        #region Services and MTU

        /// <summary>
        /// Indicates if we have successfully read the device services
        /// </summary>
        public bool ServicesAvailable()
        {
            // If the Manager is ready, the required services have been discovered and validated
            return _servicesDiscovered;
        }

        /// <summary>
        /// Return a list of services provided by the connected device
        /// </summary>
        /// <returns>List of service <see cref="Guid"/> values</returns>
        public List<Guid> DeviceServices()
        {
            // Not possible with this adaptor?
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the current MTU
        /// </summary>
        /// <returns>MTU</returns>
        public int MTU()
        {
            return _mtu;
        }

        #endregion

        #region Characteristics


        /// <summary>
        /// Clear the previously set up notification for when a read characteristic changes
        /// </summary>
        /// <param name="iService">Service providing the characteristic</param>
        /// <param name="iCharacteristic">Characteristic we no longer want to get data from</param>
        /// <param name="iDataWrittenEvent">Event handler on which to return the clear results</param>
        /// <returns>True if we successfully unsubscribed from the notification</returns>
        public bool ClearCharacteristic(string iService, string iCharacteristic, EventHandler<CharacteristicWriteEventArgs> iDataWrittenEvent)
        {
            // Check current status
            if (IsConnected())
            {
                Log(string.Format("Clearing notifications for service [{0}] characteristic [{1}]", iService, iCharacteristic));

                // Get the characteristic as a Guid
                Guid characteristic = new Guid(iCharacteristic);

                return _bleManager.ClearCharacteristicNotification(characteristic, new DeviceDataSentCallBack(characteristic, null, iDataWrittenEvent));
            }
            return false;
        }

        /// <summary>
        /// Set up a notification for when a read characteristic changes
        /// </summary>
        /// <param name="iService">Service providing the characteristic</param>
        /// <param name="iCharacteristic">Characteristic we want to get data from</param>
        /// <param name="iDataReceivedEvent">Event handler which will receive the pushed data from the characteristic</param>
        /// <param name="iDataWrittenEvent">Event handler on which to return the notification results</param>
        /// <returns>True if we successfully subscribed to a notification</returns>
        public bool NotifyCharacteristic(string iService, string iCharacteristic, 
            EventHandler<CharacteristicReadEventArgs> iDataReceivedEvent, 
            EventHandler<CharacteristicWriteEventArgs> iDataWrittenEvent)
        {
            // Check current status
            if (IsConnected())
            {
                Log(string.Format("Subscribing to notifications for service [{0}] characteristic [{1}]", iService, iCharacteristic));

                // Get the characteristic as a Guid
                Guid characteristic = new Guid(iCharacteristic);

                return _bleManager.EnableCharacteristicNotification(
                    characteristic, 
                    new DeviceDataReceivedCallback(characteristic, iDataReceivedEvent), 
                    new DeviceDataSentCallBack(characteristic, null, iDataWrittenEvent));
            }
            return false;
        }

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
        public bool ReadCharacteristic(string iService, string iCharacteristic, EventHandler<CharacteristicReadEventArgs> iDataReadEvent)
        {
            // Check current status
            if (IsConnected())
            {
                _readData = null;

                Log(string.Format("Requesting to read from service [{0}] characteristic [{1}]", iService, iCharacteristic), true);

                // Get the characteristic as a Guid
                Guid characteristic = new Guid(iCharacteristic);

                // Write the data - there is no result from this function, so it returns immediately; iDataReadEvent is used to catch
                // the read response from BleManager and return the data to the caller, via DeviceDataReceivedCallback
                _bleManager.ReadCharacteristicData(characteristic, new DeviceDataReceivedCallback(characteristic, iDataReadEvent));

                return true;
            }

            // No connection if we end up here
            Log("Failed to read data: no connection to device");
            return false;
        }

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
        public bool WriteCharacteristic(string iService, string iCharacteristic, byte[] iData, EventHandler<CharacteristicWriteEventArgs> iDataWrittenEvent)
        {
            // Check current status
            if (IsConnected())
            {
                Log(string.Format("Writing to service [{0}] characteristic [{1}]: {2} bytes", iService, iCharacteristic, iData.Length), true);

                // Get the characteristic as a Guid
                Guid characteristic = new Guid(iCharacteristic);

                // Write the data - the result from this function indicates only we tried to send it; the data sent notification (iDataWrittenEvent) will
                // tell the caller the result, once it is run
                return _bleManager.WriteCharacteristicData(characteristic, iData, new DeviceDataSentCallBack(characteristic, iData, iDataWrittenEvent));
            }

            // No connection if we end up here
            Log("Failed to send data: no connection to device");
            return false;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Event handler to receive notice that the device has disconnected
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including the device info</param>
        private void _bleManager_DeviceDisconnectedEvent(object sender, DeviceEventArgs e)
        {
            DeviceInfo device = FindDeviceInfo(e.Device);

            Log(string.Format("Device {0} [{1}] disconnected", device != null ? device.Name : e.Device.Name, e.Device.Address));

            if (device != null)
            {
                DeviceDisconnectEvent?.Invoke(this, new DeviceInfoEventArgs(device));

                _localDisconnectionSuccess?.Invoke(this, new DeviceInfoEventArgs(device));
                _localDisconnectionSuccess = null;
            }
        }

        /// <summary>
        /// Event handler to receive notice that the device has connected
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including the device info</param>
        private void _bleManager_DeviceConnectedEvent(object sender, DeviceEventArgs e)
        {
            Log(string.Format("Device {0} [{1}] connected", e.Device.Name, e.Device.Address));

            DeviceInfo device = FindDeviceInfo(e.Device);
            if (device != null)
            {
                DeviceConnectedEvent?.Invoke(this, new DeviceInfoEventArgs(FindDeviceInfo(e.Device)));
            }
        }

        /// <summary>
        /// Event handler to receive notice that the device is ready
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including the device info</param>
        private void _bleManager_DeviceReadyEvent(object sender, DeviceEventArgs e)
        {
            Log(string.Format("Device {0} [{1}] ready!", e.Device.Name, e.Device.Address));

            DeviceInfo device = FindDeviceInfo(e.Device);
            if (device != null)
            {
                DeviceReadyEvent?.Invoke(this, new DeviceInfoEventArgs(FindDeviceInfo(e.Device)));
            }
        }

        /// <summary>
        /// Event handler to receive notice that the MTU has changed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including new MTU</param>
        private void _bleManager_MtuChangeEvent(object sender, DeviceMtuEventArgs e)
        {
            if (e.Success)
            {
                _mtu = e.MTU;
                ConnectionMtuChangeEvent?.Invoke(this, new MtuEventArgs(e.MTU));
            }
            else
            {
                ConnectionMtuChangeEvent?.Invoke(this, new MtuEventArgs(0, e.Success, e.MTU));
            }
        }

        /// <summary>
        /// Event handler to receive when services have been discovered
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void _bleManager_ServicesDiscoveredEvent(object sender, EventArgs e)
        {
            _servicesDiscovered = true;
        }

        /// <summary>
        /// Event handler for when the device is bonded
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments including a Bluetooth device</param>
        private void _bleManager_DeviceBondedEvent(object sender, DeviceEventArgs e)
        {
            DeviceBondedEvent?.Invoke(this, new DeviceInfoEventArgs(FindDeviceInfo(e.Device)));
        }

        /// <summary>
        /// Event handler to receive when requested data has been read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including the data</param>
        private void _bleManager_DataReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            _readData = e.Data;
        }

        /// <summary>
        /// Find a DeviceInfo instance matching the BluetoothDevice
        /// </summary>
        /// <param name="iDevice">Bluetooth device</param>
        /// <returns>Found device info or null if not found</returns>
        private DeviceInfo FindDeviceInfo(BluetoothDevice iDevice)
        {
            // Find the BluetoothDevice already scanned
            return Devices.FirstOrDefault(d => d.DeviceId.ToString() == MacAddressUtils.GuidFromMacAddress(iDevice.Address).ToString());
        }

        /// <summary>
        /// Log handler to pass messages back to the owner
        /// </summary>
        /// <param name="iMessage"></param>
        private void Log(string iMessage, bool iDebugOutputOnly = false)
        {
            // _logDelegate?.Invoke(iMessage, iDebugOutputOnly);

            LogEvent?.Invoke(this, new LogEventArgs(iMessage));
        }

        #endregion

        #region Private scanning methods

        /// <summary>
        /// Handler to receive scan failure
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Scan failure event args</param>
        private void Receiver_ScanFailure(object sender, NordicScanFailureEventArgs e)
        {
            Log(string.Format("SCAN FAILURE: error code {0}", e.ErrorCode));
            DiscoveryFinished?.Invoke(this, new EventArgs());
        }

        private void Receiver_ScanResult(object sender, NordicScanResultEventArgs e)
        {
            ScanResult scanResult = e.Results;

            FoundDevices.Add(e.Results.Device);

            DeviceInfo device = InterpretScanResult(scanResult);

            if (Devices.FirstOrDefault(d => d.DeviceId == device.DeviceId) == null)
            {
                Log(string.Format("Device {0}: {1} [{2}]", FoundDevices.Count, e.Results.Device.Name, e.Results.Device.Address), true);
                Devices.Add(device);
            }

            OnRaiseDeviceDiscoveredEvent(new DeviceInfoEventArgs(device));
        }

        /// <summary>
        /// Analyse the scan result to find device information
        /// </summary>
        /// <param name="iScanResult">Scan result</param>
        /// <returns>Device information class</returns>
        private static DeviceInfo InterpretScanResult(ScanResult iScanResult)
        {
            Guid guid = MacAddressUtils.GuidFromMacAddress(iScanResult.Device.Address);

            DeviceInfo device = new DeviceInfo(iScanResult.ScanRecord.DeviceName, guid) { Rssi = iScanResult.Rssi };

            // Add in manufacturer specific data
            Dictionary<int, byte[]> manf = new Dictionary<int, byte[]>();
            SparseArray manufacturerSpecificData = iScanResult.ScanRecord.ManufacturerSpecificData;

            if (manufacturerSpecificData != null)
            {
                for (int i = 0; i < manufacturerSpecificData.Size(); i++)
                {
                    int companyId = iScanResult.ScanRecord.ManufacturerSpecificData.KeyAt(i);
                    byte[] data = (byte[])iScanResult.ScanRecord.ManufacturerSpecificData.ValueAt(i);
                    manf.Add(companyId, data);
                }
            }
            device.ManufacturerSpecificData = manf;

            // Add in services
            List<Guid> services = new List<Guid>();
            foreach (ParcelUuid uuid in iScanResult.ScanRecord.ServiceUuids ?? new List<ParcelUuid>())
            {
                services.Add(new Guid(uuid.Uuid.ToString()));
            }
            device.Services = services;
            return device;
        }

        /// <summary>
        /// Event invocation method for handling discovered devices 
        /// </summary>
        /// <param name="e">Event arguments containing a <see cref="DeviceInfo"/> instance</param>
        private void OnRaiseDeviceDiscoveredEvent(DeviceInfoEventArgs e)
        {
            DeviceDiscoveredEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Stop the scan and notify the caller that it has ended
        /// </summary>
        private void StopScan()
        {
            Log("Stop BLE scan");

            BluetoothLeScannerCompat scanner = BluetoothLeScannerCompat.Scanner;

            if (_scanning && scanner != null && _skfScanCallback != null)
            {
                // Hold onto this scan callback so we can flush it before any following scan
                _previousScanCallback = _skfScanCallback;

                scanner.StopScan(_skfScanCallback);
                _scanTimer?.Stop();
                _scanning = false;

                // Invoke the caller's callback so they know the scan finished
                DiscoveryFinished?.Invoke(this, new EventArgs());
            }

            // Clear the event handlers
            if (_skfScanCallback != null)
            {
                Log(string.Format("StopScan: {0} callbacks cleared", _skfScanCallback.ClearCallers()));

                // _skfScanCallback.Dispose();
                _skfScanCallback = null;
            }

            //_scanTimer?.Dispose();
            _scanTimer = null;
        }

        #endregion
    }
}