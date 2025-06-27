using Microsoft.Azure.Devices.Client;
using IoT_Device;

string devConnectionString = File.ReadAllText("AzureDeviceConnect.txt");
Console.WriteLine("Device connection string loaded");

var deviceClient = DeviceClient.CreateFromConnectionString(devConnectionString);

await deviceClient.OpenAsync();

var device = new DeviceFunctions(deviceClient);
Console.WriteLine("Connected to device");
await device.InitializerHandlers();
await device.UpdateTwinAsync();
Console.WriteLine("Staring up..");
Console.WriteLine("");
Console.WriteLine("");
await device.SendMessages(2, 500);
var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
while (await periodicTimer.WaitForNextTickAsync())
{
    await device.TimerSendingMessages();
}