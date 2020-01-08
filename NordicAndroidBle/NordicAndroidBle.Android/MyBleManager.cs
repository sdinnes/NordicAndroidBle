using System;
using System.Collections.Generic;
using Android.Bluetooth;
using Java.Util;
using NO.Nordicsemi.Android.Ble;
using NO.Nordicsemi.Android.Ble.Callback;

namespace NordicAndroidBle.Droid
{
    /// <summary>
    /// Implementation descendant of Nordic Semiconductor BleManager
    /// </summary>
    /// <remarks>
    /// Code based on version 2.1.1, uploaded 14 May 2019.
    /// 
    /// https://github.com/NordicSemiconductor/Android-BLE-Library
    /// 
    /// </remarks>
    public class MyBleManager : BleManager
    {
        #region Constants

        private const string MSG_CHARACTERISTIC_NOT_LOADED = "Characteristic {0} not loaded";

        #endregion

        #region Private members

        /// <summary>
        /// BleManagerGattCallBack instance
        /// </summary>
        private static SkfBleManagerGattCallback _gattCallBack;

        /// <summary>
        /// BleManagerCallbacks instance
        /// </summary>
        private static SkfBleManagerCallbacks _skfBleManagerCallbacks = new SkfBleManagerCallbacks();

        /// <summary>
        /// A list of services and characteristics to be used by the adaptor
        /// </summary>
        protected List<ServiceAndCharacteristicsParcel> _services;

        /// <summary>
        /// A dictionary of loaded characteristics
        /// </summary>
        protected Dictionary<Guid, BluetoothGattCharacteristic> _characteristics = new Dictionary<Guid, BluetoothGattCharacteristic>();

        /// <summary>
        /// Currently connected device
        /// </summary>
        private BluetoothDevice _currentDevice;

        #endregion

        #region Constructor

        public MyBleManager() : base(Android.App.Application.Context)
        {
            // Create nested class for Gatt callbacks
            _gattCallBack = new SkfBleManagerGattCallback(this);

            // Use some event handlers from SkfBleManagerGattCallback
            _gattCallBack.OnDeviceDisconnectedEvent += _gattCallBack_OnDeviceDisconnectedEvent;

            // Add event handlers for the BleManager events like device ready
            _skfBleManagerCallbacks.DeviceConnectedEvent += _skfBleManagerCallbacks_DeviceConnectedEvent;
            _skfBleManagerCallbacks.DeviceReadyEvent += _skfBleManagerCallbacks_DeviceReadyEvent;
            _skfBleManagerCallbacks.DeviceDisconnectedEvent += _skfBleManagerCallbacks_DeviceDisconnectedEvent;

            // Set the Gatt call back class
            SetGattCallbacks(_skfBleManagerCallbacks);
        }

        /// <summary>
        /// Event to be raised when a device is bonded
        /// </summary>
        public event EventHandler<LogEventArgs> LogEvent;

        /// <summary>
        /// Add a list of services and characteristics to be used by the adaptor
        /// </summary>
        /// <param name="iServices">A list of services and characteristics</param>
        /// <remarks>
        /// We use this list to prepare the adaptor - when it connects, SkfBleManagerGattCallback.IsRequiredServiceSupported
        /// is called to validate all services and characteristics that we plan to use.  It also creates all of the
        /// characteristics and stores them in a private dictionary to be called when needed.
        /// If this list is not set, or is empty, connection will fail.
        /// </remarks>
        internal void ServiceAndCharacteristics(List<ServiceAndCharacteristicsParcel> iServices)
        {
            _services = iServices;
        }

        /// <summary>
        /// Logging override
        /// </summary>
        /// <param name="priority">Priority</param>
        /// <param name="message">Log message</param>
        public override void Log(int priority, string message)
        {
            base.Log(priority, message);
            LogEvent?.Invoke(this, new LogEventArgs(message));
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Event handler for device connected, for calls from owner class
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceConnectedEvent;

        /// <summary>
        /// Event handler for device disconnected, for calls from owner class
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceDisconnectedEvent;

        /// <summary>
        /// Event handler for device ready, for calls from owner class
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceReadyEvent;

        /// <summary>
        /// Event handler for when the MTU is changed
        /// </summary>
        public event EventHandler<DeviceMtuEventArgs> MtuChangeEvent;

        /// <summary>
        /// Event handler for when services are discovered
        /// </summary>
        public event EventHandler<EventArgs> ServicesDiscoveredEvent;

        /// <summary>
        /// Event handler for when the device is disconnected
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments including a Bluetooth device</param>
        private void _skfBleManagerCallbacks_DeviceDisconnectedEvent(object sender, DeviceEventArgs e)
        {
            _currentDevice = null;
            DeviceDisconnectedEvent?.Invoke(this, new DeviceEventArgs(e.Device));
        }

        /// <summary>
        /// Event handler for when the device is connected
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments including a Bluetooth device</param>
        private void _skfBleManagerCallbacks_DeviceConnectedEvent(object sender, DeviceEventArgs e)
        {
            _currentDevice = e.Device;
            DeviceConnectedEvent?.Invoke(this, new DeviceEventArgs(e.Device));
        }

        /// <summary>
        /// Event handler for when the device is ready
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments including a Bluetooth device</param>
        private void _skfBleManagerCallbacks_DeviceReadyEvent(object sender, DeviceEventArgs e)
        {
            _currentDevice = e.Device;
            DeviceReadyEvent?.Invoke(this, new DeviceEventArgs(e.Device));
        }

        /// <summary>
        /// Event handler for when services are discovered
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void _gattCallBack_OnDeviceDisconnectedEvent(object sender, EventArgs e)
        {
            ServicesDiscoveredEvent?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Clear all event handlers
        /// </summary>
        public void ClearEventHandlers()
        {
            if (DeviceConnectedEvent != null)
            {
                foreach (Delegate d in DeviceConnectedEvent?.GetInvocationList())
                {
                    DeviceConnectedEvent -= (EventHandler<DeviceEventArgs>)d;
                }
            }
            if (DeviceReadyEvent != null)
            {
                foreach (Delegate d in DeviceReadyEvent?.GetInvocationList())
                {
                    DeviceReadyEvent -= (EventHandler<DeviceEventArgs>)d;
                }
            }
            if (DeviceDisconnectedEvent != null)
            {
                foreach (Delegate d in DeviceDisconnectedEvent?.GetInvocationList())
                {
                    DeviceDisconnectedEvent -= (EventHandler<DeviceEventArgs>)d;
                }
            }
            if (MtuChangeEvent != null)
            {
                foreach (Delegate d in MtuChangeEvent?.GetInvocationList())
                {
                    MtuChangeEvent -= (EventHandler<DeviceMtuEventArgs>)d;
                }
            }
            if (ServicesDiscoveredEvent != null)
            {
                foreach (Delegate d in ServicesDiscoveredEvent?.GetInvocationList())
                {
                    ServicesDiscoveredEvent -= (EventHandler<EventArgs>)d;
                }
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Overridden method to return the BleManagerGattCallback instance for this class
        /// </summary>
        protected override BleManagerGattCallback GattCallback => _gattCallBack;

        /// <summary>
        /// Request to bond with the device
        /// </summary>
        /// <param name="iISuccessCallback">Instance of a class implementing ISuccessCallback</param>
        public void BondToDevice(ISuccessCallback iISuccessCallback)
        {
            CreateBond().Done(iISuccessCallback).Enqueue();
        }

        #endregion

        #region Characteristic handlers

        /// <summary>
        /// Request and enqueue MTU for the current connection
        /// </summary>
        /// <param name="iMtu">Required MTU</param>
        public void MakeMtuRequest(int iMtu)
        {
            // Enqueue an MTU request
            RequestMtu(iMtu).With(CreateMtuRequestCallBack()).Fail(CreateMtuRequestCallBack()).Enqueue();
        }

        /// <summary>
        /// Request and enqueue MTU for the current connection
        /// </summary>
        /// <param name="iMtu">Required MTU</param>
        /// <param name="iMtuRequestCallBack">Request call back</param>
        public void MakeMtuRequest(int iMtu, MtuRequestCallBack iMtuRequestCallBack)
        {
            // Enqueue an MTU request
            RequestMtu(iMtu).With(iMtuRequestCallBack).Fail(iMtuRequestCallBack).Enqueue();
        }

        /// <summary>
        /// Enable notifications on a characteristic, enqueueing the request
        /// </summary>
        /// <param name="iCharacteristic"></param>
        /// <param name="iDataReceivedCallback">Callback class with which to return the notification read results</param>
        /// <param name="iDataSentCallback">Callback class with which to return the set notification results</param>
        /// <returns>True if requested characteristic has been loaded</returns>
        public bool EnableCharacteristicNotification(Guid iCharacteristic, IDataReceivedCallback iDataReceivedCallback, DeviceDataSentCallBack iDataSentCallback)
        {             
            if (_characteristics.TryGetValue(iCharacteristic, out BluetoothGattCharacteristic characteristic))
            {
                // Set up notification callback for receiving data from the gateway
                SetNotificationCallback(characteristic).With(iDataReceivedCallback);

                // Enable the notifications and enqueue the request
                EnableNotifications(characteristic).With(iDataSentCallback).Fail(iDataSentCallback).Enqueue();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear notifications on a characteristic, enqueueing the request
        /// </summary>
        /// <param name="iCharacteristic"></param>
        /// <param name="iDataSentCallback">Callback class with which to return the clear notification results</param>
        /// <returns>True if requested characteristic has been loaded</returns>
        public bool ClearCharacteristicNotification(Guid iCharacteristic, DeviceDataSentCallBack iDataSentCallback)
        {            
            if (_characteristics.TryGetValue(iCharacteristic, out BluetoothGattCharacteristic characteristic))
            {
                DisableNotifications(characteristic).With(iDataSentCallback).Fail(iDataSentCallback).Enqueue();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Request a read of data from the characteristic, enqueueing the request
        /// </summary>
        /// <param name="iCharacteristic">Guid representing the characteristic</param>
        /// <param name="iDataReceivedCallback">Callback class with which to return the read results</param>
        /// <returns>True if requested characteristic has been loaded</returns>
        public bool ReadCharacteristicData(Guid iCharacteristic, DeviceDataReceivedCallback iDataReceivedCallback)
        {            
            if (_characteristics.TryGetValue(iCharacteristic, out BluetoothGattCharacteristic characteristic))
            {
                ReadCharacteristic(characteristic).With(iDataReceivedCallback).Fail(iDataReceivedCallback).Enqueue();
                return true;
            }
            iDataReceivedCallback?.OnRequestFailed(_currentDevice, (int)GattStatus.Failure);
            return false;
        }

        /// <summary>
        /// Write data to the characteristic, enqueueing the request
        /// </summary>
        /// <param name="iCharacteristic">Guid representing the characteristic</param>
        /// <param name="iData">Data to be sent</param>
        /// <param name="iDataSentCallback">Callback class with which to return the write results</param>
        /// <returns>True if requested characteristic has been loaded</returns>
        public bool WriteCharacteristicData(Guid iCharacteristic, byte[] iData, DeviceDataSentCallBack iDataSentCallback)
        {
            if (_characteristics.TryGetValue(iCharacteristic, out BluetoothGattCharacteristic characteristic))
            {
                WriteCharacteristic(characteristic, iData).With(iDataSentCallback).Fail(iDataSentCallback).Enqueue();
                return true;
            }
            iDataSentCallback?.OnRequestFailed(_currentDevice, (int)GattStatus.Failure);
            return false;
        }

        #endregion

        #region Private methods

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
        /// Callback event raised when the MTU request is granted
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including the new MTU value</param>
        private void MtuRequest_DeviceMtuEvent(object sender, DeviceMtuEventArgs e)
        {
            MtuChangeEvent?.Invoke(this, e);
        }

        #endregion

        #region Nested class SkfBleManagerGattCallback : BleManagerGattCallback

        public class SkfBleManagerGattCallback : BleManagerGattCallback
        {
            // Parent manager instance - needed for Initialize() and IsRequiredServiceSupported() overrides
            private MyBleManager _manager;

            //public SkfBleManagerGattCallback() : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
            //{

            //}

            /// <summary>
            /// Class constructor
            /// </summary>
            /// <param name="__self">Enclosing <see cref="SkfBleManager"/> instance</param>
            public SkfBleManagerGattCallback(MyBleManager __self) : base(__self)
            {
                _manager = __self;
            }

            /// <summary>
            /// The Initialize() override allows the manager to set up any characteristics with notification and read/write operations (enqueued).
            /// These are primarily actions that you wish to be carried out on connection; once the device is
            /// ready, the user should be able to add his own list of characteristics. In fact, in this class implementation, that should all
            /// be done by calling SkfBleManager.ServiceAndCharacteristics() with a non-empty list of services and characteristics to be used.
            /// I have left this code here simply as a guide to how you _can_ do it if you wish.  However, this probably best applies to 
            /// application specific services and characteristics, which we are not supporting here.  (I guess you might want to do that for
            /// some of the common services and characteristics, like those which return device name and so on).
            /// </summary>
            protected override void Initialize()
            {
                base.Initialize();

                // How to set up a notification callback for receiving data from a characteristic
                // _manager.SetNotificationCallback(iClientCharacteristic).With(CreateGatewayDataReceivedCallback());

                // How to enable the notifications and enqueue the request
                //_manager.EnableNotifications(iClientCharacteristic).Enqueue();

                // How to set up a handler for reading data from a characteristic (on request)
                // _manager.ReadCharacteristic(iClientCharacteristic).With(mButtonCallback).Enqueue();
            }

            /// <summary>
            /// Method that loads service and characteristics and checks they are suitable
            /// </summary>
            /// <param name="p0">Gatt server</param>
            /// <returns>True if successful</returns>
            protected override bool IsRequiredServiceSupported(BluetoothGatt p0)
            {                
                if (_manager._services != null)
                {
                    foreach (ServiceAndCharacteristicsParcel parcel in _manager._services)
                    {
                        // Get the service
                        BluetoothGattService service = p0.GetService(UUID.FromString(parcel.Service.ToString()));
                        if (service != null)
                        {
                            // Check each characteristic
                            foreach (KeyValuePair<Guid,CharacteristicsParcel> characteristicsParcel in parcel.Characteristics)
                            {
                                if (!_manager._characteristics.ContainsKey(characteristicsParcel.Key))
                                {
                                    // Get the characteristic
                                    BluetoothGattCharacteristic characteristic = service.GetCharacteristic(UUID.FromString(characteristicsParcel.Key.ToString()));

                                    if (characteristic != null)
                                    {
                                        // Add the service to the local dictionary
                                        _manager._characteristics.Add(characteristicsParcel.Key, characteristic);

                                        // Find the required properties of this characteristic
                                        CharacteristicProperties properties = characteristicsParcel.Value.Properties;

                                        // Now check that the characteristic supports the required properties
                                        GattProperty rxProperties = characteristic.Properties;

                                        // Read request
                                        if (properties.HasFlag(CharacteristicProperties.Read))
                                        {
                                            if (!rxProperties.HasFlag(GattProperty.Read))
                                            {
                                                return false;
                                            }
                                        }

                                        // Write request
                                        if (properties.HasFlag(CharacteristicProperties.Write))
                                        {
                                            if (!rxProperties.HasFlag(GattProperty.Write))
                                            {
                                                return false;
                                            }
                                        }

                                        // Notifications
                                        if (properties.HasFlag(CharacteristicProperties.Notify))
                                        {
                                            if (!rxProperties.HasFlag(GattProperty.Notify))
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                    else
                                        return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }

            /// <summary>
            /// Event handler for device ready
            /// </summary>
            public event EventHandler<EventArgs> OnDeviceDisconnectedEvent;

            /// <summary>
            /// Event handler for when the device disconnects
            /// </summary>
            protected override void OnDeviceDisconnected()
            {
                // Clear out all the loaded characteristics
                _manager._characteristics.Clear();

                // Raise an event for any interested parties
                OnDeviceDisconnectedEvent?.Invoke(this, new EventArgs());
            }

            /// <summary>
            /// Event handler for device ready
            /// </summary>
            public event EventHandler<EventArgs> DeviceReadyEvent;

            /// <summary>
            /// Event handler for when the device is ready
            /// </summary>
            protected override void OnDeviceReady()
            {
                base.OnDeviceReady();

                // Raise an event for any interested parties
                DeviceReadyEvent?.Invoke(this, new EventArgs());
            }
        }

        #endregion
    }

    /// <summary>
    /// Implementation of IBleManagerCallbacks for BLE callbacks
    /// </summary>
    public class SkfBleManagerCallbacks : Java.Lang.Object, IBleManagerCallbacks
    {
        public SkfBleManagerCallbacks()
        {
        }

        /// <summary>
        /// Event handler for device bonded
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceBondedEvent;

        public void OnBonded(BluetoothDevice p0)
        {
            DeviceBondedEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device bonding failed
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceBondingFailedEvent;

        public void OnBondingFailed(BluetoothDevice p0)
        {
            DeviceBondingFailedEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device bonding
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceBondingEvent;

        public void OnBondingRequired(BluetoothDevice p0)
        {
            DeviceBondingEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device connected
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceConnectedEvent;

        public void OnDeviceConnected(BluetoothDevice p0)
        {
            DeviceConnectedEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device connecting
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceConnectingEvent;

        public void OnDeviceConnecting(BluetoothDevice p0)
        {
            DeviceConnectingEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device disconnected
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceDisconnectedEvent;

        public void OnDeviceDisconnected(BluetoothDevice p0)
        {
            DeviceDisconnectedEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device disconnecting
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceDisconnectingEvent;

        public void OnDeviceDisconnecting(BluetoothDevice p0)
        {
            DeviceDisconnectingEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device not supported
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceNotSupportedEvent;

        public void OnDeviceNotSupported(BluetoothDevice p0)
        {
            DeviceNotSupportedEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device error
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceErrorEvent;

        public void OnError(BluetoothDevice p0, string p1, int p2)
        {
            DeviceErrorEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device link loss
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceLinkLossOcurredEvent;

        public void OnLinkLossOccurred(BluetoothDevice p0)
        {
            DeviceLinkLossOcurredEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device services discovered
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceServicesDiscoveredEvent;

        public void OnServicesDiscovered(BluetoothDevice p0, bool p1)
        {
            DeviceServicesDiscoveredEvent?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// Event handler for device ready
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceReadyEvent;

        void IBleManagerCallbacks.OnDeviceReady(BluetoothDevice p0)
        {
            DeviceReadyEvent?.Invoke(this, new DeviceEventArgs(p0));
        }
    }

    /// <summary>
    /// Event arguments for BleManagerGattCallback callbacks
    /// </summary>
    public class DeviceEventArgs : EventArgs
    {
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iDevice">Bluetooth device involved in the call</param>
        /// <param name="iStatus">GATT status (optional - Success inferred)</param>
        public DeviceEventArgs(BluetoothDevice iDevice, GattStatus iStatus = GattStatus.Success)
        {
            Device = iDevice;
            Status = iStatus;
        }

        /// <summary>
        /// Bluetooth device involved in the call
        /// </summary>
        public BluetoothDevice Device { get; set; }

        /// <summary>
        /// GATT status
        /// </summary>
        public GattStatus Status { get; }
    }

    //public class SkfTimeoutableRequest : TimeoutableRequest
    //{
    //    public SkfTimeoutableRequest() : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
    //    {
    //    }

    //    public override Request Done(ISuccessCallback callback)
    //    {
    //        return base.Done(callback);
    //    }

    //    public override Request Fail(IFailCallback callback)
    //    {
    //        return base.Fail(callback);
    //    }

    //    public override TimeoutableRequest Timeout(long timeout)
    //    {
    //        return base.Timeout(timeout);
    //    }
    //}
}