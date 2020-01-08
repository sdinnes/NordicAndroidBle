using Android.Bluetooth;
using NO.Nordicsemi.Android.Ble.Callback;
using NO.Nordicsemi.Android.Ble.Data;
using System;

namespace NordicAndroidBle.Droid
{
    /// <summary>
    /// Implementation of IDataSentCallback for handling callback after writing characteristic data
    /// </summary>
    public class DeviceDataSentCallBack : Java.Lang.Object, IDataSentCallback, IFailCallback
    {
        private Guid _characteristic;
        private byte[] _dataSent;
        private EventHandler<CharacteristicWriteEventArgs> _dataWriteEvent;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="iCharacteristic">Characteristic being written</param>
        /// <param name="iDataSent">Data that was sent, for comparison in the callback</param>
        /// <param name="iDataWriteEvent">Event handler on which to return the write results</param>
        public DeviceDataSentCallBack(Guid iCharacteristic, byte[] iDataSent, EventHandler<CharacteristicWriteEventArgs> iDataWriteEvent) : base()
        {
            _characteristic = iCharacteristic;
            _dataSent = iDataSent;
            _dataWriteEvent = iDataWriteEvent;
        }

        /// <summary>
        /// Data send handler
        /// </summary>
        /// <param name="p0">Device receiving the data</param>
        /// <param name="p1">Data values sent to the device</param>
        public void OnDataSent(BluetoothDevice p0, Data p1)
        {
            // Compare what we sent with what the adaptor thinks we sent
            bool success = _dataSent != null & p1.GetValue() != null ? ArrayValuesSame(_dataSent, p1.GetValue()): true;

            _dataWriteEvent?.Invoke(this, new CharacteristicWriteEventArgs(
                MacAddressUtils.GuidFromMacAddress(p0.Address), 
                _characteristic, success,
                p1.GetValue() != null ? p1.GetValue().Length : 0));
        }

        /// <summary>
        /// A callback invoked when the request has failed with status other than BluetoothGatt.GATT_SUCCESS
        /// </summary>
        /// <param name="p0">Device receiving the data</param>
        /// <param name="p1">GATT status code</param>
        /// <param name="message">Optional message</param>
        public void OnRequestFailed(BluetoothDevice p0, int p1)
        {
            _dataWriteEvent?.Invoke(this, new CharacteristicWriteEventArgs(MacAddressUtils.GuidFromMacAddress(p0.Address), _characteristic, false, 0, p1));
        }

        /// <summary>
        /// Verify arrays are the same
        /// </summary>
        /// <param name="x">One array</param>
        /// <param name="y">Another array</param>
        /// <returns>True if both are same length and have the same values</returns>
        internal bool ArrayValuesSame(byte[] x, byte[] y)
        {
            if (x.Length == y.Length)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i]) return false;
                }
            }
            return true;
        }
    }
}