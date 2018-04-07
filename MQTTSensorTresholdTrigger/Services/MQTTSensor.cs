using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Threading;
using MQTTnet;
using MQTTnet.Client;
using MQTTSensorTresholdTrigger.Events.EventArgs;
using MQTTSensorTresholdTrigger.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MQTTSensorTresholdTrigger.Services
{
    public class MQTTSensor
    {
        private IMqttClient client;

        private string clientId;
        /// <summary>
        /// The client id used to connect to the broker
        /// </summary>
        public string ClientId
        {
            get { return clientId; }
        }

        private string broker;
        /// <summary>
        /// The broker that this MQTT sensor subscribes to the topic
        /// </summary>
        public string Broker
        {
            get { return broker; }
        }

        private string topic;
        /// <summary>
        /// The topic that this MQTT sensor subscribes to
        /// </summary>
        public string Topic
        {
            get { return topic; }
        }

        private bool isConnected;
        /// <summary>
        /// True if the MQTT client is isConnected, false if not
        /// </summary>
        public bool IsConnected
        {
            get { return isConnected; }
        }

        private SensorValue currentValue;
        /// <summary>
        /// The current valid value of the sensor, can be null
        /// </summary>
        public SensorValue CurrentValue
        {
            get { return currentValue; }
        }

        private double sensorValueThreshold;
        /// <summary>
        /// The treshold at which MQTTSensor will issue the threshold exceeded event
        /// </summary>
        public double SensorValueThreshold
        {
            get { return sensorValueThreshold; }
            set
            {
                if (value == sensorValueThreshold) return;
                sensorValueThreshold = value;
            }
        }

        /// <summary>
        /// Event called when CurrentValue surpasses SensorValueTreshold.
        /// Will only trigger if CurrentValue has been below SensorValueTreshold for the lst 5 min
        /// </summary>
        public EventHandler<SensorValueTresholdExceededEventArgs> SensorThresholdExceeded;

        private bool sensorTresholdExceededTriggerable
        {
            get { return sensorTresholdExceededStopwatch.ElapsedMilliseconds > (5 * 60 * 1000); }
        }
        private Stopwatch sensorTresholdExceededStopwatch = new Stopwatch();

        /// <summary>
        /// Create a new MQTTSensor that will subscribe to the specified topic
        /// </summary>
        /// <param name="clientId">The id that the MQTT client will connect to the broker with</param>
        /// <param name="broker">The broker that the MQTT client will connect to</param>
        /// <param name="topic">The topic that the MQTT client will subscribe to</param>
        public MQTTSensor(IMqttClient client, string clientId, string broker, string topic)
        {
            this.clientId = clientId;
            this.broker = broker;
            this.topic = topic;
            this.client = client;
            //Options are from MQTTNet examples
            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId(ClientId)
                .WithTcpServer(Broker)
                .Build();
            client.Connected += Client_Connected;
            client.Disconnected += Client_Disconnected;
            client.ApplicationMessageReceived += Client_ApplicationMessageReceived;
            client.ConnectAsync(clientOptions);
            sensorTresholdExceededStopwatch.Restart();
        }

        /// <summary>
        /// Will cleanup MQTT client
        /// </summary>
        public void Cleanup()
        {
            Console.WriteLine("MQTTSensor cleaning up");
            client.UnsubscribeAsync(Topic);
            client.DisconnectAsync();
            client.Connected -= Client_Connected;
            client.Disconnected -= Client_Disconnected;
            client.ApplicationMessageReceived -= Client_ApplicationMessageReceived;
            client = null;
        }

        /// <summary>
        /// MQTT Client message received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            //Output is from MQTTNet examples
            Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
            Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
            Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
            Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
            Console.WriteLine();

            currentValue = JsonConvert.DeserializeObject<SensorValue>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload),new JsonSerializerSettings{DateTimeZoneHandling = DateTimeZoneHandling.Utc});

            if (currentValue.Value > sensorValueThreshold)
            {
                //if cool down period is done trigger event
                if(sensorTresholdExceededTriggerable)
                    SensorThresholdExceeded(this, new SensorValueTresholdExceededEventArgs(CurrentValue));

                //restart cool down period
                sensorTresholdExceededStopwatch.Restart();
            }
        }

        /// <summary>
        /// MQTT Client connection event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            isConnected = client.IsConnected;
            Console.WriteLine($"MQTT Client connected to {Broker}");
            Console.WriteLine($"MQTT Client subscribing to {Topic}");
            client.SubscribeAsync(new TopicFilterBuilder().WithTopic(Topic).Build());
        }

        /// <summary>
        /// MQTT Client disconnected event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            isConnected = client.IsConnected;
            if (e.ClientWasConnected)
            {
                Console.WriteLine(
                    $"MQTT Client was disconnected"); //Version 2.7.x provides an excecption as reason for disconnection, but has a bug making it unstable
                //Since connection was dropped try to reconnect
                //Options are from MQTTNet examples
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId(ClientId)
                    .WithTcpServer(Broker)
                    .Build();
                client.ConnectAsync(clientOptions);
            }
            else
                Console.WriteLine(
                    $"MQTT Client could not connect to {Broker}"); //Version 2.7.x provides an excecption as reason for disconnection, but has a bug making it unstable
        }
    }
}
