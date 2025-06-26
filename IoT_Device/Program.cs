using Microsoft.Azure.Devices.Client;
using IoT_Device;

string devConnectionString = File.ReadAllText("AzureDeviceConnect.txt");
Console.WriteLine("Device connection string loaded");

var deviceClient = DeviceClient.CreateFromConnectionString(devConnectionString);
await deviceClient.OpenAsync();
var device = new DeviceFunctions(deviceClient);
Console.WriteLine("Connected to device");
Console.ReadLine();