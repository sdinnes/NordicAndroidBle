using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace NordicAndroidBle
{
    class MainPageViewModel : INotifyPropertyChanged
    {
        #region Constants

        // iBeacon services and characteristics

        private string SERVICE_FOR_BEACON = "0000fff0-0000-1000-8000-00805f9b34fb";
        private string CHARACTERISTIC_FOR_PASSWORD = "0000fff1-0000-1000-8000-00805f9b34fb";
        private string CHARACTERISTIC_FOR_UUIDD = "0000fff2-0000-1000-8000-00805f9b34fb";

        private string SERVICE_FOR_DEVICE_NAME = "0000ff80-0000-1000-8000-00805f9b34fb";
        private string CHARACTERISTIC_FOR_DEVICE_NAME = "00002a90-0000-1000-8000-00805f9b34fb";

        private string SERVICE_FOR_EDDYSTONE = "0000ffd0-0000-1000-8000-00805f9b34fb";
        private string CHARACTERISTIC_FOR_URI_BEACON1 = "0000ffd1-0000-1000-8000-00805f9b34fb";
        private string CHARACTERISTIC_FOR_URI_BEACON2 = "0000ffd2-0000-1000-8000-00805f9b34fb";
        private string CHARACTERISTIC_FOR_URI_BEACON3 = "0000ffd3-0000-1000-8000-00805f9b34fb";

        #endregion

        #region Private members

        private readonly INordicBleAdaptor _ble;
        private string _adaptorState;

        private Command _scanBLE;
        private Command _disconnect;

        private bool _deviceIsConnected;
        private bool _isBusy;

        #endregion

        #region View stuff 

        /// <summary>
        /// Device selected on list view
        /// </summary>
        public DeviceInfo SelectedDevice { get; set; }

        /// <summary>
        /// List of services and characteristics required for the device
        /// </summary>
        /// <remarks>
        /// Note that on connection, the adaptor validates all of the required characteristics, including their
        /// given <see cref="CharacteristicProperties"/> requirements.  If any fail this test, the connection 
        /// will fail. Note that this requires that the services and characteristics to be used need to be
        /// known in advance.
        /// </remarks>
        public List<ServiceAndCharacteristicsParcel> RequestedServicesAndCharacteristics { get; private set; }

        /// <summary>
        /// Gets or sets an observable list of endpoints
        /// </summary>
        public ObservableCollection<DeviceInfo> DeviceList { get; set; }

        /// <summary>
        /// Class constructor
        /// </summary>
        public MainPageViewModel()
        {
            // Use dependency service to get the BLE adaptor
            _ble = DependencyService.Get<INordicBleAdaptor>();

            _ble.LogEvent += Ble_LogEvent;

            // Create devices list
            DeviceList = new ObservableCollection<DeviceInfo>();

            // Create the list of services and characteristics that we need - first, the generic properties
            ServiceAndCharacteristicsParcel genericService = new ServiceAndCharacteristicsParcel(new Guid(SERVICE_FOR_BEACON));

            genericService.AddCharacteristic(new Guid(CHARACTERISTIC_FOR_PASSWORD), CharacteristicProperties.Read);
            genericService.AddCharacteristic(new Guid(CHARACTERISTIC_FOR_UUIDD), CharacteristicProperties.Read);

            // Service for device information
            ServiceAndCharacteristicsParcel deviceService = new ServiceAndCharacteristicsParcel(new Guid(SERVICE_FOR_DEVICE_NAME));

            deviceService.AddCharacteristic(new Guid(CHARACTERISTIC_FOR_DEVICE_NAME), CharacteristicProperties.Read);

            // Service for the beacon
            ServiceAndCharacteristicsParcel beaconService = new ServiceAndCharacteristicsParcel(new Guid(SERVICE_FOR_EDDYSTONE));

            beaconService.AddCharacteristic(new Guid(CHARACTERISTIC_FOR_URI_BEACON1), CharacteristicProperties.Read | CharacteristicProperties.WriteNoResponse);
            beaconService.AddCharacteristic(new Guid(CHARACTERISTIC_FOR_URI_BEACON2), CharacteristicProperties.Read | CharacteristicProperties.WriteNoResponse);
            beaconService.AddCharacteristic(new Guid(CHARACTERISTIC_FOR_URI_BEACON3), CharacteristicProperties.Read | CharacteristicProperties.WriteNoResponse);

            RequestedServicesAndCharacteristics = new List<ServiceAndCharacteristicsParcel>
                {
                    genericService, deviceService, beaconService
                };
        }

        /// <summary>
        /// Event handler to receive messages from the adaptor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ble_LogEvent(object sender, LogEventArgs e)
        {
            AdaptorState = e.LogMessage;
        }

        /// <summary>
        /// <see cref="Command"/> binding for the view's scan button 
        /// </summary>
        public Command ScanBLE
        {
            get
            {
                _scanBLE = _scanBLE ?? new Command(async () => await ScanForDevices(), () => !IsBusy);
                return _scanBLE;
            }
        }

        /// <summary>
        /// Binding for adaptor state label
        /// </summary>
        public string AdaptorState { get { return _adaptorState; } set { _adaptorState = value; OnPropertyChanged(); Debug.WriteLine(_adaptorState); } }

        /// <summary>
        /// Busy property
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                SetControlIsBusy();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indicates if a device is connected
        /// </summary>
        public bool DeviceIsConnected { get { return _deviceIsConnected; } private set { _deviceIsConnected = value; SetControlIsBusy(); OnPropertyChanged(); } }

        #endregion

        #region Scanning

        /// <summary>
        /// Scan for devices
        /// </summary>
        /// <returns>Task</returns>
        private async Task ScanForDevices()
        {
            if (!IsBusy)
            {
                IsBusy = true;
                SetControlIsBusy();
                try
                {
                    // Add device discovery and disconnection event handlers
                    _ble.DeviceDiscoveredEvent += Ble_DeviceDiscoveredEvent;
                    _ble.DeviceDisconnectEvent += Ble_DeviceDisconnectEvent;

                    // Start scanning for any device
                    bool scanStarted = _ble.Scan(ScanFinished, 30);

                    if (!scanStarted)
                    {
                        ScanFinished(this, new EventArgs());
                    }
                }
                catch (Exception e)
                {
                    await Application.Current.MainPage.DisplayAlert("Scan failed", e.Message, "Close");
                    AdaptorState = e.Message;
                    IsBusy = false;
                    SetControlIsBusy();
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Scan?", "Busy!", "Close");
            }
        }

        /// <summary>
        /// Scan result event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args including device information</param>
        private void Ble_DeviceDiscoveredEvent(object sender, DeviceInfoEventArgs e)
        {
            DeviceInfo device = e.Device;

            // Add if not already on the list
            if (DeviceList.FirstOrDefault(d => d.DeviceId == device.DeviceId) == null)
            {
                DeviceList.Add(device);
                AdaptorState += Environment.NewLine + string.Format("Device found: {0} [{1}]", e.Device.Name, MacAddressUtils.MacAddressAsString(e.Device.DeviceId, true));
            }
        }

        /// <summary>
        /// Scanning finished
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void ScanFinished(object sender, EventArgs e)
        {
            IsBusy = false;
        }
        
        #endregion

        #region Connection and read

        /// <summary>
        /// Connect to the selected device
        /// </summary>
        internal void DoConnect()
        {
            if (SelectedDevice != null)
            {
                NordicConnectDirect(SelectedDevice);
            }
        }

        /// <summary>
        /// Stop scanning and connect
        /// </summary>
        /// <param name="peripheral"></param>
        private void NordicConnectDirect(DeviceInfo peripheral)
        {
            _ble.CancelScan();

            _ble.ConnectToDevice(peripheral, 0, RequestedServicesAndCharacteristics, false, ConnectionEvent);
        }

        /// <summary>
        /// Event called when the device is connected
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success</param>
        private void ConnectionEvent(object sender, SuccessEventArgs e)
        {
            DeviceIsConnected = e.Success;

            if (DeviceIsConnected)
            {
                // Devide password "0x666666" (sent as three bytes 0x66)
                byte[] pwb = new byte[] { 0x66, 0x66, 0x66 };

                bool result = _ble.WriteCharacteristic(SERVICE_FOR_BEACON, CHARACTERISTIC_FOR_PASSWORD, pwb, PassWordWrittenEvent);

                if (!result)
                {
                    Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Could not write characteristic {0}", CHARACTERISTIC_FOR_PASSWORD), "Close");
                }
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error connecting to device: {0}", e.Reason), "Close");
            }
        }

        /// <summary>
        /// Event called when password data written
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success</param>
        private void PassWordWrittenEvent(object sender, CharacteristicWriteEventArgs e)
        {
            if (e.Success)
            {
                AdaptorState = string.Format("Password write: {0} bytes written", e.NumberOfBytesWritten);

                // Read UUID
                var readResult = _ble.ReadCharacteristic(SERVICE_FOR_BEACON, CHARACTERISTIC_FOR_UUIDD, UUIDReadEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error writing password: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when UUID read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success and containing data</param>
        private void UUIDReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            if (e.Success)
            {
                string returnedUUID = BitConverter.ToString(e.Data);

                Debug.WriteLine(String.Format("UUID: [{0}]", returnedUUID));

                // Read Eddystone URI data - a randon data area we can use
                var readResult = _ble.ReadCharacteristic(SERVICE_FOR_EDDYSTONE, CHARACTERISTIC_FOR_URI_BEACON1, Beacon1ReadEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error reading UUID: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when beacon 1 data read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success and containing data</param>
        private void Beacon1ReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            if (e.Success)
            {
                string result = string.Format("{0} [{1}]", BitConverter.ToString(e.Data), Encoding.ASCII.GetString(e.Data));

                Debug.WriteLine(String.Format("Uri Beacon Data (first 20 bytes): {0}", result));

                bool readResult = _ble.ReadCharacteristic(SERVICE_FOR_EDDYSTONE, CHARACTERISTIC_FOR_URI_BEACON2, Beacon2ReadEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error reading beacon 1: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when beacon 2 data read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success and containing data</param>
        private void Beacon2ReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            if (e.Success)
            {
                string result = string.Format("{0} [{1}]", BitConverter.ToString(e.Data), Encoding.ASCII.GetString(e.Data));

                Debug.WriteLine(String.Format("Uri Beacon Data (last 8 bytes): {0}", result));

                bool readResult = _ble.ReadCharacteristic(SERVICE_FOR_EDDYSTONE, CHARACTERISTIC_FOR_URI_BEACON3, Beacon3ReadEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error reading beacon 2: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when beacon 3 data read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success and containing data</param>
        private void Beacon3ReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            if (e.Success && e.Data.Length > 0)
            {
                Debug.WriteLine("Length of Uri Beacon Data: {0}", e.Data[0]);

                // Try writing to the Eddystone data
                //
                string testData = "Test Write";
                char[] testDataChars = testData.ToCharArray();
                byte[] testDataBytes = Encoding.ASCII.GetBytes(testDataChars);

                var writeResult = _ble.WriteCharacteristic(SERVICE_FOR_EDDYSTONE, CHARACTERISTIC_FOR_URI_BEACON1, testDataBytes, EddystoneWriteEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error reading beacon 3: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when beacon 1 data written
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success</param>
        private void EddystoneWriteEvent(object sender, CharacteristicWriteEventArgs e)
        {
            if (e.Success)
            {
                Debug.WriteLine(String.Format("Uri Beacon Data written: {0} bytes", e.NumberOfBytesWritten));

                // Read it again
                var readResult = _ble.ReadCharacteristic(SERVICE_FOR_EDDYSTONE, CHARACTERISTIC_FOR_URI_BEACON1, EddystoneReadEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error writing Eddystone data: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when beacon 1 data read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success and containing data</param>
        private void EddystoneReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            if (e.Success)
            {
                var result = Encoding.ASCII.GetString(e.Data);

                Debug.WriteLine(String.Format("Uri Beacon Data after write (first 20 bytes): {0}", result));

                var readResult = _ble.ReadCharacteristic(SERVICE_FOR_EDDYSTONE, CHARACTERISTIC_FOR_URI_BEACON3, EddystoneLengthReadEvent);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error reading Eddystone data: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        /// <summary>
        /// Event called when beacon 3 data read
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args indicating success and containing data</param>
        private void EddystoneLengthReadEvent(object sender, CharacteristicReadEventArgs e)
        {
            if (e.Success && e.Data.Length > 0)
            {
                Debug.WriteLine("Length of Uri Beacon Data: {0}", e.Data[0]);
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("ERROR", string.Format("Error reading Eddystone data length: {0}", e.Message), "Close");
                _ = DisconnectDevice();
            }
        }

        #endregion

        #region Disconnection

        /// <summary>
        /// Device disconnection event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ble_DeviceDisconnectEvent(object sender, DeviceInfoEventArgs e)
        {
            _ble.DeviceDisconnectEvent -= Ble_DeviceDisconnectEvent;
            DeviceIsConnected = false;

            Application.Current.MainPage.DisplayAlert("Notice", string.Format("Device disconnected"), "Close");
        }

        /// <summary>
        /// <see cref="Command"/> binding for the view's disconnect button 
        /// </summary>
        public Command Disconnect
        {
            get
            {
                _disconnect = _disconnect ?? new Command(async () => await DisconnectDevice(), () => DeviceIsConnected);
                return _disconnect;
            }
        }

        /// <summary>
        /// Disconnect a connected device
        /// </summary>
        /// <returns></returns>
        private async Task DisconnectDevice()
        {
            await Task.Run(() => _ble.DisconnectGatt());
        }

        #endregion

        #region Updaters

        /// <summary>
        /// Manage all property changed requests (push to the View)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// override method that is used to set the enable state of specific controls on
        /// the view, based on the current state of the IsBusy property
        /// </summary>
        protected void SetControlIsBusy()
        {
            ScanBLE.ChangeCanExecute();
            Disconnect.ChangeCanExecute();
        }

        #endregion
    }
}
