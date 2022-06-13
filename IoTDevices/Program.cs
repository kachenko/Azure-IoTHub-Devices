using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTDevices
{
    class Program
    {
        private static DeviceClient s_deviceClient;
        private readonly static string s_myDeviceId = "<my-device-id>";
        private readonly static string s_iotHubUri = "<my-iothub-uri>.azure-devices.net";
        private readonly static string s_deviceKey = "<my-device-key>";
        private static bool readTheFile = false;

        private static async Task Main()
        {
            if (readTheFile)
            {
                ReadOneRowFromFile();
            }
            else
            {
                Console.WriteLine("Routing Tutorial: Simulated device\n");
                s_deviceClient = DeviceClient.Create(s_iotHubUri,
                  new DeviceAuthenticationWithRegistrySymmetricKey(s_myDeviceId, s_deviceKey), TransportType.Mqtt);

                using var cts = new CancellationTokenSource();
                var messages = SendDeviceToCloudMessagesAsync(cts.Token);
                Console.WriteLine("Press the Enter key to stop.");
                Console.ReadLine();
                await s_deviceClient.CloseAsync(cts.Token);
                cts.Cancel();
                await messages;
            }
        }

        private static void ReadOneRowFromFile()
        {
            string filePathAndName = "C:\\Users\\username\\Desktop\\testfiles\\47_utf32.txt";

            string outputFilePathAndName = filePathAndName.Replace(".txt", "_new.txt");
            string[] fileLines = System.IO.File.ReadAllLines(filePathAndName);

            var messageObject = JObject.Parse(fileLines[0]);
            var body = messageObject.Value<string>("Body");

            string outputResult = System.Text.Encoding.UTF32.GetString(System.Convert.FromBase64String(body));

            System.IO.File.WriteAllText(outputFilePathAndName, outputResult);
        }

        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken token)
        {
            double minWaterPressure = 0;
            Random random = new Random();

            while (!token.IsCancellationRequested)
            {
                double currentWaterPressure = minWaterPressure + random.NextDouble() * 15;
                bool currentIrrigationStatus = (random.Next() > (Int32.MaxValue / 2));

                string infoString;
                string levelValue;
                if (currentWaterPressure == 0 || currentIrrigationStatus == false)
                {
                    infoString = "ERROR: No water or electricity";
                    levelValue = "critical";
                }
                else if (currentWaterPressure > 6)
                {
                    infoString = "WARNING: High water pressure in the pump";
                    levelValue = "warning";
                }
                else
                {
                    infoString = "This is OK";
                    levelValue = "ok";
                }

                var telemetryDataPoint = new
                {
                    deviceId = s_myDeviceId,
                    pressure = currentWaterPressure,
                    status = currentIrrigationStatus,
                    pointInfo = infoString
                };

                var telemetryDataString = JsonConvert.SerializeObject(telemetryDataPoint);

                using var message = new Message(Encoding.UTF8.GetBytes(telemetryDataString))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json",
                };
                message.Properties.Add("level", levelValue);

                await s_deviceClient.SendEventAsync(message);

                Console.WriteLine("{0}) Sent message: {1}", DateTime.UtcNow, telemetryDataString);

                await Task.Delay(1000);
            }
        }
    }
}
