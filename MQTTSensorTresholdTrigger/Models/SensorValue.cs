using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTSensorTresholdTrigger.Models
{
    public class SensorValue
    {
        public DateTime TimeStamp;
        public double Value;
        public SensorValue(double value, DateTime timeStamp)
        {
            this.Value = value;
            this.TimeStamp = timeStamp;
        }

        public SensorValue() { }
    }
}
