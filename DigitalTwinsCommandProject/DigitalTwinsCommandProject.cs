using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DigitalTwinsCommandProject
{
    public static class DigitalTwinToIoTHubFunction
    {
        private static readonly string IOT_HUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOT_HUB_CONNECTION_STRING");
        private static readonly string APP_SERVICE_ENDPOINT = Environment.GetEnvironmentVariable("APP_SERVICE_ENDPOINT");

        [FunctionName("DigitalTwinToIoTHubFunction")]
        public static async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            if (IOT_HUB_CONNECTION_STRING == null || APP_SERVICE_ENDPOINT == null)
            {
                log.LogError("Application settings 'IOT_HUB_CONNECTION_STRING' or 'APP_SERVICE_ENDPOINT' not set");
                return;
            }

            try
            {
                // Fetch isThresholdExceeded value from the App Service
                bool isThresholdExceeded = await FetchThresholdValueFromAppService();

                // Send the message to IoT Hub only if the threshold is exceeded
                if (isThresholdExceeded)
                {
                    // Create a message for IoT Hub
                    var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { isThresholdExceeded })));

                    // Send the message to IoT Hub
                    var serviceClient = ServiceClient.CreateFromConnectionString(IOT_HUB_CONNECTION_STRING);
                    await serviceClient.SendAsync("iPhone", message); // Replace "your-device-id" with the ID of your IoT device

                    log.LogInformation($"Message sent to IoT Hub with isThresholdExceeded value: {isThresholdExceeded}");
                }
                else
                {
                    log.LogInformation("Threshold not exceeded. No message sent to IoT Hub.");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in DigitalTwinToIoTHubFunction: {ex.Message}");
            }
        }

        private static async Task<bool> FetchThresholdValueFromAppService()
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(APP_SERVICE_ENDPOINT);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                dynamic accelerometerData = JsonConvert.DeserializeObject(content);

                // Check if x, y, or z value is above 2
                bool isThresholdExceeded = accelerometerData.x > 2 || accelerometerData.y > 2 || accelerometerData.z > 2;
                return isThresholdExceeded;
            }
        }
    }
}
