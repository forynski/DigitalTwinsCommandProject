using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Text;
using Newtonsoft.Json;

namespace DigitalTwinsCommandProject
{
    public static class DigitalTwinToIoTHubFunction
    {
        private static readonly string IOT_HUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOT_HUB_CONNECTION_STRING");
        private static readonly string ADT_SERVICE_URL = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("DigitalTwinToIoTHubFunction")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            try
            {
                // Make an HTTP request to your App Service endpoint
                HttpResponseMessage response = await httpClient.GetAsync(ADT_SERVICE_URL);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response
                    dynamic accelerometerData = JsonConvert.DeserializeObject(responseBody);

                    // Check if x, y, or z value is above 2
                    bool isThresholdExceeded =
                        Math.Abs(accelerometerData.x) > 2 ||
                        Math.Abs(accelerometerData.y) > 2 ||
                        Math.Abs(accelerometerData.z) > 2;

                    // Create a message for IoT Hub
                    var message = new Message(Encoding.UTF8.GetBytes($"{{ \"isThresholdExceeded\": {isThresholdExceeded.ToString().ToLower()} }}"));

                    // Send the message to IoT Hub
                    var serviceClient = ServiceClient.CreateFromConnectionString(IOT_HUB_CONNECTION_STRING);
                    await serviceClient.SendAsync("iPhone", message); // Replace "your-device-id" with the ID of your IoT device

                    log.LogInformation($"Message sent to IoT Hub with isThresholdExceeded value: {isThresholdExceeded}");
                }
                else
                {
                    log.LogError($"Failed to fetch data from App Service. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in DigitalTwinToIoTHubFunction: {ex.Message}");
            }
        }
    }
}
