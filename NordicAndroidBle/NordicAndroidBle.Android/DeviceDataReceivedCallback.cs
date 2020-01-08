using System;
using Android.Bluetooth;
using NO.Nordicsemi.Android.Ble.Callback;
using NO.Nordicsemi.Android.Ble.Data;

namespace NordicAndroidBle.Droid
{
    /// <summary>
    /// Implementation of IDataReceivedCallback for receiving characteristic data on request
    /// </summary>
    public class DeviceDataReceivedCallback : Java.Lang.Object, IDataReceivedCallback, IFailCallback
    {
        private Guid _characteristic;
        private EventHandler<CharacteristicReadEventArgs> _dataReadEvent;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iCharacteristic">Characteristic being read</param>
        /// <param name="iDataReadEvent">Event handler on which to return the read results</param>
        public DeviceDataReceivedCallback(Guid iCharacteristic, EventHandler<CharacteristicReadEventArgs> iDataReadEvent) : base()
        {
            _characteristic = iCharacteristic;
            _dataReadEvent = iDataReadEvent;
        }

        /// <summary>
        /// Data receiving handler
        /// </summary>
        /// <param name="p0">Device sending the data</param>
        /// <param name="p1">Data values received from the device</param>
        public void OnDataReceived(BluetoothDevice p0, Data p1)
        {
            // Will wrapping this prevent these JNI errors like "use of deleted global reference"?
            try
            {
                _dataReadEvent?.Invoke(this, new CharacteristicReadEventArgs(MacAddressUtils.GuidFromMacAddress(p0.Address), _characteristic, p1.GetValue()));
            }
            catch
            { }
        }

        /// <summary>
        /// A callback invoked when the request has failed with status other than BluetoothGatt.GATT_SUCCESS
        /// </summary>
        /// <param name="p0">Device sending the data</param>
        /// <param name="p1">GATT status code</param>
        /// <param name="message">Optional message</param>
        public void OnRequestFailed(BluetoothDevice p0, int p1)
        {
            // Will wrapping this prevent these JNI errors like "use of deleted global reference"?
            try
            {
                _dataReadEvent?.Invoke(this, new CharacteristicReadEventArgs(MacAddressUtils.GuidFromMacAddress(p0.Address), _characteristic, new byte[0], p1));
            }
            catch
            { }
        }
    }
}