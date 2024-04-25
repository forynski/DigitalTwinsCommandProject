using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.DigitalTwins.Core;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace DigitalTwinsCommandProject
{
    public static class DigitalTwinsCommandProject
    {
        private static readonly string ADT_SERVICE_URL = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly string IOT_HUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOT_HUB_CONNECTION_STRING");

        [FunctionName("DigitalTwinToIoTHubFunction")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            if (ADT_SERVICE_URL == null || IOT_HUB_CONNECTION_STRING == null)
            {
                log.LogError("Application settings 'ADT_SERVICE_URL' or 'IOT_HUB_CONNECTION_STRING' not set");
                return;
            }

            try
            {
                DefaultAzureCredential defaultAzureCredential = new DefaultAzureCredential();
                DigitalTwinsClient digitalTwinsClient = new DigitalTwinsClient(new Uri(ADT_SERVICE_URL), defaultAzureCredential);
                log.LogInformation($"ADT service client connection created.");

                // Query your digital twin instance to get the value of 'isThresholdExceeded' property
                string digitalTwinId = "iPhone509"; // Replace with the ID of your digital twin
                var isThresholdExceeded = (bool)(await digitalTwinsClient.GetDigitalTwinAsync<bool>(digitalTwinId));

                // Create a message for IoT Hub
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { isThresholdExceeded })));

                // Send the message to IoT Hub
                var serviceClient = ServiceClient.CreateFromConnectionString(IOT_HUB_CONNECTION_STRING);
                await serviceClient.SendAsync("iPhone", message); // Replace "your-device-id" with the ID of your IoT device

                log.LogInformation($"Message sent to IoT Hub with isThresholdExceeded value: {isThresholdExceeded}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error in DigitalTwinToIoTHubFunction: {ex.Message}");
            }
        }
    }
}
