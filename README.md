# NordicAndroidBle
Xamarin Forms project for Android binding Nordic Semiconductor BLE libraries from Java

This repo is based on work done for a specific inhouse requirement in which probably not all methods in the original 
Java libraries taken from the Nordic Semiconductor libaries have been implemented.  I believe that the bindings from
Java contained in the two assemblies NordicScanner and NordicBle are complete, however.

The Nordic Semiconductor Java libraries are also hosted on GitHub here - there is one for scanning only and one for connections (GATT):

Code based on version 2.1.1, uploaded 14 May 2019.

    https://github.com/NordicSemiconductor/Android-BLE-Library
    
    https://github.com/NordicSemiconductor/Android-Scanner-Compat-Library
    
In both cases, I cloned the Java code and used Android Studio to create two jar files (using release configuration).  These were
then used to create the Xamarin Forms projects binding the code into .Net.

The PCL part of the solution hosts a simple, single view containing controls to start scanning and list all found devices; selecting a device stops the scan and attempts a connection.  The services and characteristics employed are matched to a simple, off the shelf iBeacon which has a small number of services and characteristics including a small random memory "Eddystone" area.  The code in the view model demonstrations the call-back based model used in this library: all calls, such as reading a characteristic, return a bool which only indicates that the call was made, not the success of the call.  This is very much an Android thing - the call includes an event handler reference, which is called when the BLE action completes and contains a success indicator and data (where applicable).  

Following this model avoids race conditions, makes best use of the Nordic Semiconductor's enqueuing model and greatly simplifies the calling applications flow.

The solution utilises dependency injection to make the BLE functionality available to the PCL layer, with all of the actual BLE code being in the Android layer.  Note that this means the obtained library is a singleton - this is why you will find in class `NordicBleAdaptor` that there is a used class `MyBleManager` (which descends from the abstract `BleManager` imported from Java) which does all the actual BLE stuff.  This allows you to reset the class at run time, if required.

Note that when you connect to a device, one of the parameters is a list of services and characteristics that are to be used.  This invokes a test on the device connection to verify that all are supported.  If not, the connection will fail.  Only those services and characteristics included in this list will be available for calling once connected.

Here is how it is done in the example view model:

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
                
Connecting to a device is done this way:

        private void NordicConnectDirect(DeviceInfo peripheral)
        {
            _ble.CancelScan();

            _ble.ConnectToDevice(peripheral, 0, RequestedServicesAndCharacteristics, false, ConnectionEvent);
        }

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

`ConnectionEvent` is called when the connection completes (both for failure and success); `SuccessEventArgs.Success` indicates the result.  In this example, the first step with the iBeacon is to write a password `0x666666` to be able to access it.

In the event handler `PassWordWrittenEvent` you can see how we check the result and proceed to read something from the device:

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
