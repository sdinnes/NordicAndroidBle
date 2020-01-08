using Android.Bluetooth;
using NO.Nordicsemi.Android.Ble.Callback;
using System;

namespace NordicAndroidBle.Droid
{
    class SuccessCallBack : Java.Lang.Object, ISuccessCallback, IFailCallback
    {
        private EventHandler<DeviceEventArgs> _eventHandler;

        public SuccessCallBack(EventHandler<DeviceEventArgs> iEventHandler)
        {
            _eventHandler = iEventHandler;
        }

        /// <summary>
        /// A callback invoked when the request has succeeded
        /// </summary>
        /// <param name="p0">Bluetooth device involved in the call</param>
        public void OnRequestCompleted(BluetoothDevice p0)
        {
            _eventHandler?.Invoke(this, new DeviceEventArgs(p0));
        }

        /// <summary>
        /// A callback invoked when the request has failed with status other than BluetoothGatt.GATT_SUCCESS
        /// </summary>
        /// <param name="p0">Bluetooth device involved in the call</param>
        /// <param name="p1">GATT status code</param>
        public void OnRequestFailed(BluetoothDevice p0, int p1)
        {
            _eventHandler?.Invoke(this, new DeviceEventArgs(p0, (GattStatus)p1));
        }
    }
}