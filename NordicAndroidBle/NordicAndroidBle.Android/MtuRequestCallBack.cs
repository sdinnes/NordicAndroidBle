using Android.Bluetooth;
using NO.Nordicsemi.Android.Ble.Callback;
using System;

namespace NordicAndroidBle.Droid
{

    /// <summary>
    /// Event arguments for new MTU value
    /// </summary>
    public class DeviceMtuEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="iDevice">Bluetooth device</param>
        /// <param name="mtu">Returned MTU value</param>
        /// <param name="iSuccess">Optional parameter indicating success (defaul true)</param>
        public DeviceMtuEventArgs(BluetoothDevice iDevice, int mtu, bool iSuccess = true)
        {
            Device = iDevice;
            MTU = mtu;
            Success = iSuccess;
        }

        /// <summary>
        /// Bluetooth device
        /// </summary>
        public BluetoothDevice Device { get; }

        /// <summary>
        /// Returned MTU value
        /// </summary>
        public int MTU { get; set; }

        /// <summary>
        /// Indicates success
        /// </summary>
        public bool Success { get; private set; }
    }

    /// <summary>
    /// Call back handler class for MTU
    /// </summary>
    public class MtuRequestCallBack : Java.Lang.Object, IMtuCallback, IFailCallback
    {
        public void OnMtuChanged(BluetoothDevice p0, int p1)
        {
            DeviceMtuEvent?.Invoke(this, new DeviceMtuEventArgs(p0, p1));
        }

        public void OnRequestFailed(BluetoothDevice p0, int p1)
        {
            DeviceMtuEvent?.Invoke(this, new DeviceMtuEventArgs(p0, p1, false));
        }

        /// <summary>
        /// Event handler for MTU value received
        /// </summary>
        public event EventHandler<DeviceMtuEventArgs> DeviceMtuEvent;
    }
}