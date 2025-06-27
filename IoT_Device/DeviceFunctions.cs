using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace IoT_Device
{
    public class DeviceFunctions
    {
        private readonly DeviceClient deviceClient;

        string OpcUaString = File.ReadAllText("OpcUaString.txt");
        string DeviceID = "Device 1";

        public DeviceFunctions(DeviceClient deviceClient)
        {
            this.deviceClient = deviceClient;
        }

        #region Sendmsg
        public async Task SendMessages(int nrOfMessages = 1, int delay = 500)
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("|---------------------------------------|");
            Console.WriteLine("[Agent] Getting data from Opc Ua ...");
            var client = new OpcClient(OpcUaString);
            client.Connect();

            var PodStats = new OpcReadNode($"ns=2;s={DeviceID}/ProductionStatus");

            var data = new
            {
                ProductionStatus = client.ReadNode($"ns=2;s=Device 1/ProductionStatus").Value,
                WorkorderId = client.ReadNode($"ns=2;s={DeviceID}/WorkorderId").Value,
                Temperature = client.ReadNode($"ns=2;s={DeviceID}/Temperature").Value,
                GoodCount = client.ReadNode($"ns=2;s={DeviceID}/GoodCount").Value,
                BadCount = client.ReadNode($"ns=2;s={DeviceID}/BadCount").Value,
                ProductionRate = client.ReadNode($"ns=2;s={DeviceID}/ProductionRate").Value,
            };

            var GoodCountCalculateNode = new OpcReadNode($"ns=2;s={DeviceID}/GoodCount");
            var BadCountCalculateNode = new OpcReadNode($"ns=2;s={DeviceID}/BadCount");
            int GoodCountCalculate = client.ReadNode(GoodCountCalculateNode).As<int>();
            int BadCountCalculate = client.ReadNode(BadCountCalculateNode).As<int>();
            var ProductionRate = new OpcReadNode($"ns=2;s={DeviceID}/ProductionRate");
            var DeviceError = new OpcReadNode($"ns=2;s={DeviceID}/DeviceError");
            int ProductionRateNode = client.ReadNode(ProductionRate).As<int>();
            int DeviceErrorNode = client.ReadNode(DeviceError).As<int>();
            if (DeviceErrorNode == 14)
            {
                client.CallMethod($"ns=2;s={DeviceID}", $"ns=2;s={DeviceID}/EmergencyStop");
                Console.WriteLine("[Agent] Device stopped by reading too many errors !!!");
            }

            await UpdateTwinData(ProductionRateNode, DeviceErrorNode);
            Console.WriteLine("[Agent] Data Collected");
            Console.WriteLine("[Agent] Device sending message to Azure IOT HUB\n");
            var DataString = JsonConvert.SerializeObject(data);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(DataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";
            Console.WriteLine($"[Agent]{DateTime.Now.ToLocalTime()} --- Message sending");
            Console.WriteLine($"[Data] [{DataString}]");


            await deviceClient.SendEventAsync(eventMessage);
            client.Disconnect();
            Console.WriteLine("[Agent] Message Send to Azure");
        }

        public async Task TimerSendingMessages()
        {
            var client = new OpcClient(OpcUaString);
            client.Connect();

            var ProductionStatus = new OpcReadNode($"ns=2;s={DeviceID}/ProductionStatus");
            int RetValues = client.ReadNode(ProductionStatus).As<int>();
            var DeviceError = new OpcReadNode($"ns=2;s={DeviceID}/DeviceError");
            int DeviceErrorNode = client.ReadNode(DeviceError).As<int>();

            client.Disconnect();
            if (RetValues == 1)
            {
                await SendMessages(1, 1);
            }
            else
            {
                if (DeviceErrorNode == 0)
                {
                    Console.WriteLine(" [Agent] Device Offline -> Not Sending Data !");
                }
                else
                {
                    string DeviceErrorString = "";
                    if (DeviceErrorNode - 8 >= 0)
                    {
                        DeviceErrorNode = DeviceErrorNode - 8;
                        DeviceErrorString = DeviceErrorString + "Unknown Error ,";
                    }
                    if (DeviceErrorNode - 4 >= 0)
                    {
                        DeviceErrorNode = DeviceErrorNode - 4;
                        DeviceErrorString = DeviceErrorString + "Sensor Failure ,";
                    }
                    if (DeviceErrorNode - 2 >= 0)
                    {
                        DeviceErrorNode = DeviceErrorNode - 2;
                        DeviceErrorString = DeviceErrorString + "Power Failure ,";
                    }
                    if (DeviceErrorNode - 1 >= 0)
                    {
                        DeviceErrorNode = DeviceErrorNode - 1;
                        DeviceErrorString = DeviceErrorString + "Emergency Stop ,";
                    }
                    Console.WriteLine(" [Agent] Device Offline -> Errors: " + DeviceErrorString + " -> Not sending data !");

                }
            }
        }

        private async Task UpdateTwinData(int ProductionRate, int DeviceError)
        {
            string DeviceErrorString = "";
            if (DeviceError - 8 >= 0)
            {
                DeviceError = DeviceError - 8;
                DeviceErrorString = DeviceErrorString + "Unknown Error ,";
            }
            if (DeviceError - 4 >= 0)
            {
                DeviceError = DeviceError - 4;
                DeviceErrorString = DeviceErrorString + "Sensor Failure ,";
            }
            if (DeviceError - 2 >= 0)
            {
                DeviceError = DeviceError - 2;
                DeviceErrorString = DeviceErrorString + "Power Failure ,";
            }
            if (DeviceError - 1 >= 0)
            {
                DeviceError = DeviceError - 1;
                DeviceErrorString = DeviceErrorString + "Emergency Stop ,";
            }

            var twin = await deviceClient.GetTwinAsync();
            var reportedProperties = new TwinCollection();

            string ReportedErrorStatus = twin.Properties.Reported["ErrorStatus"];
            int ReportedProductionRate = twin.Properties.Reported["ProductionRate"];

            if (!ReportedErrorStatus.Equals(DeviceErrorString))
            {
                reportedProperties["ErrorStatus"] = DeviceErrorString;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            if (ReportedProductionRate != ProductionRate)
            {
                reportedProperties["ProductionRate"] = ProductionRate;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }


        }

        #endregion
        private async Task On2cdMessageRecievedAsync(Message reciecedMessage, object _)
        {
            Console.WriteLine($"[Agent] \t{DateTime.Now}> C2D message callback - message recieved with id={reciecedMessage.MessageId}");
            PrintMessages(reciecedMessage);
            await deviceClient.CompleteAsync(reciecedMessage);
            Console.WriteLine($"[Agent] \t{DateTime.Now}> Completed C2D message with ID={reciecedMessage.MessageId}");
            reciecedMessage.Dispose();

        }

        private void PrintMessages(Message recievedMessage)
        {
            string messageData = Encoding.ASCII.GetString(recievedMessage.GetBytes());
            Console.WriteLine($"\t\tRecieved message: {messageData}");
            int propCount = 0;
            foreach (var prop in recievedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++}>Key={prop.Key}:Value={prop.Value}");
            }
        }

        public async Task InitializerHandlers()
        {
            await deviceClient.SetReceiveMessageHandlerAsync(On2cdMessageRecievedAsync, deviceClient);
        }

        public async Task UpdateTwinAsync()
        {
            var twin = await deviceClient.GetTwinAsync();
            Console.WriteLine($"\tStart twin value recived: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)} ");

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;
            reportedProperties["ErrorStatus"] = String.Empty;
            reportedProperties["ProductionRate"] = 0;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

    }
}

