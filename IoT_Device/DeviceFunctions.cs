using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

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


        

    }
}

