# IHC MQTT Gateway

This is an example to show how to use the IHCSdkWR library to communicate with the IHC controller.

You can get the library from NuGet under the name **Dingus.IHCSdkWR**
(If it does not download automatically for this example then right click the solution and "Restore NuGet packages")

To read more about this goto my blog here:

http://www.dingus.dk/ihc-mqtt-gateway/

(Here you can also download the compiled version, if you do not want to compile the program yourself)

The M2MQTT library is use as client is use to connect to your mqtt broker.

To configure the gateway look at the ihcmqttgateway.conf file.

If you start the propgram with no arguments the "ihcmqttgateway.conf" file will be used. 
If you want to place it somewhere else you can specifi the location as an argument to the program.

Syntax:

IhcMqttGateway.exe [-v] pathtoconffile

This programs should also be able to run on a raspberry pi with mono.
If you do not already have a mqtt broker take a look at the "mosquitto", it also runs on raspberry pi


## IHC Resources ids

You can find ihc resource ids by looking in the IHC project file - it is a XML file.
An easier way is to use my IHC Alternative Service View application:

http://www.dingus.dk/updated-ihc-alternative-service-view/

You can see a tree view of you your installation, expand and click the resource and get the id. 


