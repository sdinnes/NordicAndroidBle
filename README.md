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
