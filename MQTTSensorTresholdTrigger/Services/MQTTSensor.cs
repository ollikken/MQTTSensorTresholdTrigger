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
        /// Event called once for each time CurrentValue exceeds SensorValueTreshold for 5 minutes or more.
        /// If triggered once it will only retrigger if sensor value also has been below treshold for 5 minutes or more.
        /// </summary>
        public EventHandler<SensorValueTresholdExceededEventArgs> SensorThresholdExceeded;

        /// <summary>
        /// Used to trigger event when never triggered before and value has not been below treshold for 5 minutes
        /// </summary>
        private bool sensorThresholdExceededEventNeverTriggered = true;

        /// <summary>
        /// Used to only trigger once when triggerable expression is true
        /// </summary>
        private bool sensorTresholdExceededEventTriggered = true;

        /// <summary>
        /// Used to restart and stop timers
        /// </summary>
        private bool previousSensorValueExceededTreshold;

        /// <summary>
        /// Detremines if the exceeded treshold event can be triggered
        /// </summary>
        private bool sensorTresholdExceededEventTriggerable
        {
            get { return sensorValueExceededTresholdStopwatch.ElapsedMilliseconds > (5 * 60 * 1000) && (sensorValueBelowTresholdStopWatch.ElapsedMilliseconds > 5 * 60 * 1000 || sensorThresholdExceededEventNeverTriggered) && !sensorTresholdExceededEventTriggered; }
        }

        /// <summary>
        /// Determines if value has been above the treshold for 5 minutes
        /// </summary>
        private Stopwatch sensorValueExceededTresholdStopwatch = new Stopwatch();

        /// <summary>
        /// Determines if value has been below the treshold for 5 minutes
        /// </summary>
        private Stopwatch sensorValueBelowTresholdStopWatch = new Stopwatch();

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
        /// Run logic for executing sensor treshold exceeded event
        /// </summary>
        private void processThresholdExceededLogic()
        {
            if (CurrentValue.Value > sensorValueThreshold)
            {
                if (!previousSensorValueExceededTreshold)
                {
                    //previous value was below threshold restart exceeded threshold timer and stop below timer
                    sensorValueExceededTresholdStopwatch.Restart();
                    sensorValueBelowTresholdStopWatch.Stop();
                    sensorTresholdExceededEventTriggered = false;
                }
                //set the previous value exceeded threshold flag
                previousSensorValueExceededTreshold = true;

                if (sensorTresholdExceededEventTriggerable)
                {
                    SensorThresholdExceeded(this, new SensorValueTresholdExceededEventArgs(CurrentValue));
                    sensorTresholdExceededEventTriggered = true;
                    sensorThresholdExceededEventNeverTriggered = false;
                }
            }
            else
            {
                if (previousSensorValueExceededTreshold)
                {
                    //previous value exceeded treshold, restart below threshold timer and stop exceeded timer
                    sensorValueBelowTresholdStopWatch.Restart();
                    sensorValueExceededTresholdStopwatch.Stop();
                    sensorTresholdExceededEventTriggered = false;
                }
                //set the previous value exceeded threshold flag
                previousSensorValueExceededTreshold = false;
            }
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
            processThresholdExceededLogic();
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
