using System;
using System.Collections.Generic;
using System.Text;
using MQTTSensorTresholdTrigger.Models;

namespace MQTTSensorTresholdTrigger.Events.EventArgs
{
    public class SensorValueTresholdExceededEventArgs : System.EventArgs
    {
        public SensorValue Value;

        public SensorValueTresholdExceededEventArgs() { }

        public SensorValueTresholdExceededEventArgs(SensorValue sensorValue)
        {
            this.Value = sensorValue;
        }
    }
}
