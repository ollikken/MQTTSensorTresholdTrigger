using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace MQTTSensorTresholdTrigger.Models
{
    public class SensorValuePost
    {
        public string Message;
        public DateTime LastSensorTimeStamp;
        public double LastSensorValue;

        public SensorValuePost(SensorValue sensorValue, string message)
        {
            this.LastSensorTimeStamp = sensorValue.TimeStamp;
            this.LastSensorValue = sensorValue.Value;
            this.Message = message;
        }
    }
}
