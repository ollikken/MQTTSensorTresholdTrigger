using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTSensorTresholdTrigger.Events.EventArgs;
using MQTTSensorTresholdTrigger.Services;

namespace MQTTSensorTresholdTrigger
{
    class Program
    {
        private static SensorValuePoster poster;
        static void Main(string[] args)
        {
            double treshold = 0;
            string postUrl = "";

            if (args.Length < 2)
                return;

            if (args[0].Length <= 0 || !double.TryParse(args[0], out treshold))
                return;

            if (args[1].Length <= 0)
                return;

            postUrl = args[1];

            poster = new SensorValuePoster(postUrl);

            var MQTTClientFactory = new MqttFactory();
            var sensor = new MQTTSensor(MQTTClientFactory.CreateMqttClient() ,"b82399dacdbc4dc6bec4c2bda55d38cc", "test.mosquitto.org","sensors/lyse-test-01");
            sensor.SensorValueThreshold = treshold;
            sensor.SensorThresholdExceeded += sensor_SensorThresholdExceeded;
            while (true)
            {
                Console.ReadKey();
                if (sensor.CurrentValue != null)
                {
                    Console.WriteLine($"CurrentValue: {sensor.CurrentValue.Value}");
                    Console.WriteLine($"TimeStamp: {sensor.CurrentValue.TimeStamp}");
                }
            }
        }

        private static void sensor_SensorThresholdExceeded(object sender, SensorValueTresholdExceededEventArgs sensorValueTresholdExceededEventArgs)
        {
            Console.WriteLine($"Sensor value exceeded treshold. Sensor value is {sensorValueTresholdExceededEventArgs.Value.Value}");
            poster.PostTresholdValueExceeded(sensorValueTresholdExceededEventArgs.Value);
        }
    }
}
